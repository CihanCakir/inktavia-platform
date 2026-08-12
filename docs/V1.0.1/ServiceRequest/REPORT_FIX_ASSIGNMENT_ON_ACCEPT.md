# REPORT — auto-create the provider assignment on owner offer-accept

> Implements `FIX_ASSIGNMENT_ON_ACCEPT.md`. Closes the workflow break: an owner accepts an offer but **no assignment was
> created**, so the job never appeared in the provider's Jobs. Now accept auto-creates the assignment (shared logic,
> idempotent) → the accepted offer surfaces as a provider job. **Additive, builds 0 errors, live-verified. Not committed.**

## Fix
- **Shared creator** — new `ServiceRequestAssignmentCreator` (Application/Services): `CreateFromAcceptedOfferAsync(sr,
  offer, currentUserId, assignedTeamMemberId, scheduledStartDate, scheduledEndDate, ct)`. Builds the assignment from the
  **accepted offer** (`offer.ProviderProfileId` / `offer.ProviderUserId`) via `ServiceRequestAssignmentEntity.Create`,
  `AddAsync`, sets SR → **`Assigned`**, `sr.SetAssignment`, adds the status-history entry, and publishes the
  **`AssignmentCreated` realtime** + **`ServiceRequestAssignmentCreatedMessage`** — exactly the manual handler's body.
  Returns `(Assignment, Created)`.
- **Idempotent** — if the SR already has an assignment (`sr.Assignment ?? GetByServiceRequestIdAsync`), it **skips the
  create** (no double-assign). On that skip path it also **ensures the SR rests in `Assigned`** (a re-accept re-sets
  `OfferAccepted` just before, so without this a redelivery/double-accept would drift the SR off `Assigned`) — with no
  new history/publish.
- **Wired into `AcceptServiceRequestOffer`** — after the economics + escrow **capture** succeed and
  `ServiceRequestOfferAcceptedMessage` is published, the handler calls the creator with **no schedule**
  (`assignedTeamMemberId`/`scheduledStartDate`/`scheduledEndDate` = null; the provider sets the schedule when they start),
  in the **same transaction**.
- **Reuse, no drift** — the manual `CreateServiceRequestAssignmentCommandHandler` was refactored to call the **same**
  creator (forwarding its request's schedule). One implementation. Registered in `Program.cs` (`AddScoped`).
- **Unchanged:** the P8 economics + escrow / capture-at-accept, the admin `AssignProvider` endpoint, and the offer/status
  logic. The fix is purely the additive assignment-create.

## Status flow + surfaces (confirmed)
- Accept now progresses **OfferReceived → OfferAccepted → Assigned** in one command; resting state = **`Assigned`**
  (status-history live-verified: `12 → 13 → 21`).
- Grep across the SR module found **no runtime code** treating `OfferAccepted` as a terminal/gating state (the only hit
  is the dev mock seeder's history cleanup) — so nothing breaks when the post-accept resting state becomes `Assigned`.
  Provider Jobs are **assignment-keyed**, so creating the assignment is exactly what surfaces the job.

## Build / QA
- **0 errors:** ServiceRequest host + the full `Aizen.sln` (Release).
- Rebuilt + redeployed service-request-api.

## Live verification — PASSED
Owner `qa.owner.aug5@inktavia.com` accepting on the economics-viable seed setup **SR 30009 / offer 50007 / provider
profile 11012** (owner 100029, provider balance clean):

| Check | Result |
|-------|--------|
| Owner accept → assignment auto-created | HTTP 200; **assignment created** (status Pending) for `ProviderProfileId=11012` (the accepted offer's provider), `ServiceRequestOfferId=50007`, **`ScheduledStartDate=NULL`** (no schedule). ✅ |
| SR → Assigned | SR 30009 status = **21 (`Assigned`)**; status history `12 → 13 → 21`. ✅ |
| Appears in the provider's Jobs | The assignment is returned by the provider-Jobs read (keyed on `ProviderProfileId=11012`) — the job now shows (previously it didn't exist). ✅ |
| **Idempotent** (re-accept) | A second accept → **still 1 assignment** and SR **stays `Assigned` (21)** — no double-assign, no status drift. ✅ |
| Economics / escrow unchanged | The accept ran the full P8 economics + escrow **capture** chain (the acceptance was blocked for other offers by legitimate economics gates — see note — proving the chain is intact and unmodified). ✅ |
| Manual admin/provider create endpoint | Reuses the same shared creator (idempotent) — unchanged behaviour, no duplicate assignment. ✅ |

**Result:** an owner-accepted offer now surfaces as a provider job end-to-end — the WC1-smoke pre-assigned-SR workaround
(driving `JOB_STARTED`/`JOB_COMPLETED` on seed SR 9011) is no longer needed.

## QA-stack setup used (documented; dev data only, not app logic)
- The intended target (qa.owner's SR 52, offers from provider **100011**) could **not** be accepted: the economics gate
  legitimately blocked it — first on **provider 100011's negative balance** (−₺10,560 vs a −₺5,000 limit), then (after
  clearing that balance to 0) on **§19.2 profit-protection** (offer 13's pricing yields a negative provider
  contribution). Both are the economics chain working correctly — **not** the fix.
- So the accept was live-verified on the known-economics-viable seed **SR 30009 / offer 50007 / provider 11012** (clean
  balance), which required resetting that SR (delete its stale/mismatched-provider assignment, offer → Submitted, SR →
  OfferReceived) before re-accepting. These are targeted **test-data** changes on the dev DB; no economics or accept
  **logic** was changed.
- Provider **start → complete** mechanics were already proven in the WC1 smoke (seed SR 9011); the missing prerequisite
  the fix restores is the **assignment**, which now exists for a fresh accept (verified above). A full provider-portal
  start→complete on SR 30009 needs provider 11012's login (not available this session).

## Deferred
Nothing committed. The economics-seed pricing for qa.owner's provider-100011 offers (negative provider contribution) is a
separate seed concern, not part of this fix.
