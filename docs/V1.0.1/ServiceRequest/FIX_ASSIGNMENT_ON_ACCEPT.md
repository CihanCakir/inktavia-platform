# FIX — auto-create the provider assignment on owner offer-accept

> **Repo:** `addesso-project` (ServiceRequest module). Close the workflow break flagged in the WC1 economics smoke: an
> owner accepts a provider's offer, but **no assignment is created**, so the job **never appears in the provider's
> Jobs** — the provider can't start/complete it. In the marketplace direct-accept flow the accepted offer's provider
> **is** the assignee, so the assignment must be automatic. Additive. **Do not commit.**

## Investigated baseline
- **`AcceptServiceRequestOffer`** (owner, MO3 capture-at-accept): runs P8 economics + escrow **capture**, marks the
  offer Accepted, sets SR → **`OfferAccepted`** — but **does not create a `ServiceRequestAssignment`**.
- **Provider Jobs are assignment-keyed:** `ProviderJobsController` — summary / `jobs/{assignmentId}` detail / start /
  complete / reject all operate on an **assignment**. **No assignment ⇒ no job ⇒ the provider can't start or
  complete.** (This is why the WC1 smoke had to drive `JOB_STARTED`/`JOB_COMPLETED` on the **pre-assigned seed job**
  SR 9011 instead of the freshly-accepted SR 55/56.)
- **Assignment creation exists but only via MANUAL endpoints:** `CreateServiceRequestAssignmentCommand`
  (`ServiceRequestAssignmentController`) + admin `AssignProvider` (`AdminServiceRequestController`). **Neither runs on
  owner accept.**
- **`CreateServiceRequestAssignmentCommandHandler` is exactly the runtime path** — its own doc says *"Creates
  assignment from accepted offer, sets SR to Assigned, publishes AssignmentCreated"*: it
  `ServiceRequestAssignmentEntity.Create(sr, offer.Id, offer.ProviderProfileId, offer.ProviderUserId, …)`, sets SR →
  **`Assigned`**, `sr.SetAssignment(...)`, publishes the `AssignmentCreated` realtime + `ServiceRequestAssignmentCreatedMessage`.
  It just isn't called by the accept flow.

## Fix — auto-create the assignment inside `AcceptServiceRequestOffer`
After the accept's economics + escrow **capture** succeed (and the offer is Accepted), **create the assignment for the
accepted offer's provider** in the **same transaction**:
- Build the assignment from the **accepted offer** (`offer.ProviderProfileId`, `offer.ProviderUserId`) via the shared
  `ServiceRequestAssignmentEntity.Create(...)` — **no schedule** (`ScheduledStartDate`/`EndDate`/`AssignedTeamMemberId`
  = null; the provider sets the schedule when they **start** the job).
- Set SR → **`Assigned`**, `sr.SetAssignment(assignment)`, add status history, and publish the **`AssignmentCreated`
  realtime** + **`ServiceRequestAssignmentCreatedMessage`** — identical to `CreateServiceRequestAssignmentCommandHandler`.
- **Reuse, don't duplicate:** extract the create-assignment body into a **shared domain helper / application service**
  that both `AcceptServiceRequestOffer` and the manual `CreateServiceRequestAssignment` endpoint call (or have accept
  dispatch the command in-transaction) — one implementation, no drift.
- **Idempotent:** if the SR **already has an assignment** (re-run / retry), **skip** the create (don't double-assign);
  reuse the accept's existing capture-idempotency so a redelivery never creates a second assignment.

## Status flow + surfaces (confirm — don't break)
Accept now progresses **OfferAccepted → Assigned** (assignment created) in one command; the resting state after accept
is **`Assigned`**. Confirm nothing keys on `OfferAccepted` as the **terminal** post-accept state:
- **Owner MO-series** (MO3 accept, MO4 completion, MO5 dispute, MO6 change-order): the owner sees an accepted/assigned
  SR + payment status — verify the owner surfaces render `Assigned` correctly (it's "accepted, provider assigned").
- **Admin** SR detail + the status timeline: `Assigned` history entry appears.
- The **auto-approve completion countdown**, disputes, change-orders operate later in the lifecycle — unaffected.
If any surface treated `OfferAccepted` as the end state, update it to treat `Assigned` as the post-accept resting state.

## Don't-break / QA
- Additive: accept now also creates the assignment (shared logic). Economics / escrow / capture-at-accept / the manual
  admin assignment endpoints are **unchanged**. No double-assign (idempotent).
- Tests: (1) SR module + solution build 0 errors; (2) **owner accept → an assignment is created** for the offer's
  provider, SR → `Assigned`, `sr.SetAssignment` set, `AssignmentCreated` + `ServiceRequestAssignmentCreatedMessage`
  published; (3) the accepted SR **appears in the provider's Jobs** (summary + detail) and the provider can
  **start → complete** it; (4) **idempotent** — a re-run/redelivery does not create a second assignment; (5) escrow
  capture + economics unchanged; (6) the manual admin assign endpoints still work. Live smoke: owner accepts a fresh
  offer → the job shows in the provider portal Jobs → provider starts + completes (no seed workaround) — needs the
  stack.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FIX_ASSIGNMENT_ON_ACCEPT.md`: the auto-create wired into `AcceptServiceRequestOffer`
(shared create logic, no-schedule, idempotent), the `OfferAccepted → Assigned` flow + the surfaces confirmed, and the
live proof that an owner-accepted offer now surfaces as a provider job end-to-end (closing the WC1-smoke
pre-assigned-SR workaround).
