# Seed: one accepted + assigned job for provider2 on SR 9011 (+ JB-6 status alignment)

Goal: produce **one real accepted job** for `provider2@inktavia.com` on **ServiceRequest 9011**, using the existing
domain flow (offer → accept by the owner → assignment), so the Jobs page shows real data (KPI, enriched row,
lifecycle). Also fix a status-vocabulary mismatch (JB-6) so the list, the summary, and the SPA agree.

## Verified in source (2026-07-17)
- `AcceptServiceRequestOfferCommandHandler` does **not** create an assignment. It sets `offer.Accept()`, SR →
  `OfferAccepted`, escrow (log-and-continue on failure), and the `OFFER_ACCEPTED` system message (idempotent).
- `CreateServiceRequestAssignmentCommandHandler` creates the `ServiceRequestAssignment` (Status `Pending`), sets SR →
  `Assigned`, takes `ScheduledStartDate/EndDate`. **No auto-assignment consumer** listens to `OfferAccepted` — the
  assignment must be created explicitly.
- The provider Jobs list projects **`Status = assignment.Status`** (`ServiceRequestAssignmentStatus`:
  Pending/Accepted/…), while JB-2 summary groups by **`sr.Status`** (`ServiceRequestStatus`: Assigned/Scheduled/…).
  These are **different enums** → the list rows and the KPI/donut speak different vocabularies, and the SPA maps the
  SR-status one. **Fix in step 4.**

## Steps

### 1. Ensure provider2 has a **Submitted** offer on SR 9011
Resolve provider2's `ProviderProfileId` and their offer on SR 9011.
- If an offer exists but is `Draft` (a leftover from builder testing), **submit it** via the existing `SubmitOffer`
  path (as provider2) → status `Submitted` (+ the offer-as-message from #18).
- If no offer exists, create a minimal one (a line item or two, e.g. 5000 + KDV) and submit it.
- (An offer must be `Submitted` before it can be accepted.)

### 2. Accept it **as the SR 9011 owner**
Run `AcceptServiceRequestOfferCommand` with the **owner** identity (resolve `sr.OwnerUserId` for SR 9011; execute the
command in that owner's context — a Dev/Local action). Result: offer → `Accepted`, SR → `OfferAccepted`, one
`OFFER_ACCEPTED` system message (idempotent — 20d may already have seeded it), escrow attempt (non-fatal in MVP).

### 3. Create the assignment (the job) for provider2
Run `CreateServiceRequestAssignmentCommand` for SR 9011 → provider2's offer, with
`ScheduledStartDate` = ~next Monday, `ScheduledEndDate` = +2 days. Result: a `ServiceRequestAssignment`
(the **job**) for provider2, SR → `Assigned`. (Optionally also run `AcceptServiceRequestAssignment` → `StartAssignment`
to demo `InProgress` + the `JOB_STARTED` pill — but a single `Assigned` job is enough for now.)

> Identity note: these commands are owner/provider-scoped. Run each with the correct identity in a Dev/Local context.
> If wiring command identities in a dev harness is impractical, an **idempotent guarded seeder** that reaches the same
> end-state via the domain entities (`offer.Accept()`, `sr.ChangeStatus(Assigned)`, `ServiceRequestAssignmentEntity.
> Create(...)`) is acceptable — but prefer the real command flow so escrow/messages/realtime fire as in production.
> Idempotent (guard on an existing accepted offer / assignment for SR 9011).

### 4. JB-6 — align the jobs list status with the SR status
In the module `GetProviderJobsQueryHandler` projection, change
`Status = x.a.Status.ToString()` → **`Status = x.sr.Status.ToString()`** so the list uses the same
`ServiceRequestStatus` vocabulary as the JB-2 summary and the SPA (Assigned/Scheduled/InProgress/WaitingForMaterial/
Completed…). The assignment's own sub-status stays internal (used for the assignment lifecycle, not the job badge).
This makes the KPI counts, the table badges, and the lifecycle stepper consistent.

## Acceptance — observed
- `GET /provider/jobs` → **1 job**: SR 9011, `title` "Acil: Dümen sistemi arızası — Çeşme", `requestCode`
  "SR-SEED-EMERGENCY-1", `vesselName` "Aegean Wind", `status` **"Assigned"**, scheduled dates set.
- `GET /provider/jobs/summary` → `assigned=1, active=1, total=1` (others 0).
- SR 9011 status = `Assigned`; provider2's offer = `Accepted`; the `OFFER_ACCEPTED` message present (not duplicated).
- Idempotent: re-running does not create a second offer/assignment.

## Report
Append to `REPORT_BACKEND.md` ("SEED accepted job / JB-6"): the jobs list row + summary counts for provider2, and
confirmation the list status now reads `Assigned` (SR status), matching the summary. Unfinished is **not done**.

## Frontend (already built — will light up)
Once this + a BFF restart land: the Jobs KPI shows Aktif=1/Planlanan or Assigned, the enriched table row renders
(title/vessel/stepper/status/dates), and the status donut + (if scheduled this week) the workload bar show data. No
new frontend work needed.
