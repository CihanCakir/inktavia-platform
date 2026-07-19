# JD-2 — Backend: Provider job actions (Start / Submit-completion)

Expose the two existing provider commands as provider-scoped BFF endpoints so the Job Workspace primary action works:
**İşi Başlat** (Assigned/Scheduled → InProgress) and **Tamamlandı Bildir** (InProgress → CompletionSubmitted). The commands
already exist and fire the right side effects — this phase **adds the missing access + state guards** and the endpoints.

## Verified in source (2026-07-17)
- `StartServiceRequestAssignmentCommand(long assignmentId)` → handler: `assignment.Start()`, `sr.ChangeStatus(InProgress)`,
  status-history row, `WorkStarted` realtime, idempotent `JOB_STARTED` system message + publish. Actor from
  `_info.UserInfoAccessor.UserInfo.UserId`.
- `SubmitServiceRequestCompletionCommand(long assignmentId, SubmitServiceRequestCompletionRequest req)` → handler: creates
  `ServiceRequestCompletionEntity(sr, assignment, userId, req.CompletionNotes, req.EvidenceFileId)`, `assignment.Complete()`,
  `sr.ChangeStatus(CompletionSubmitted)`, `sr.SetCompletion(...)`, status-history, `CompletionSubmitted` realtime +
  `ServiceRequestCompletionSubmittedMessage`.
- **Gap 1 — no ownership guard:** neither handler checks `assignment.ProviderProfileId == caller`. A provider could
  start/complete another provider's job. **Must fix.**
- **Gap 2 — no state guard:** `assignment.Start()` / `assignment.Complete()` just set the status (no precondition). Start
  on a completed job, or double completion, is not blocked. **Must fix.**
- The correct guard pattern already exists in `SubmitOfferCommandHandler`:
  `var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0; if (providerProfileId
  == 0) throw new AizenBusinessException("Provider identity could not be resolved."); if (x.ProviderProfileId !=
  providerProfileId) throw new AizenBusinessException("...not found.");` — mirror it.
- BFF POST pattern: `[AizenRemoteCallPost("...")]` on `IProviderServiceRequestRemoteCall` + a provider POST action
  (`ProviderOffersController` `offers/{offerId:long}/withdraw|submit`), provider-scoped via the resolver/holder + assertion.

## Work

### 1. Module — add guards to both handlers (do NOT change side effects)
In **`StartServiceRequestAssignmentCommandHandler`** and **`SubmitServiceRequestCompletionCommandHandler`**, right after
loading the assignment (and SR):
- **Ownership:** resolve `providerProfileId` from `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`;
  if `0` → `AizenBusinessException("Provider identity could not be resolved.")`; if
  `assignment.ProviderProfileId != providerProfileId` → `AizenBusinessException("Job not found.")` (fail closed — same as
  JD-1, never reveal another provider's job).
- **State (use `sr.Status`, the stepper vocabulary):**
  - Start: allow only when `sr.Status ∈ { Assigned, Scheduled }`. Otherwise `AizenBusinessException("SR_JOB_NOT_STARTABLE")`.
    (Already-InProgress → this code; keeps the action idempotent-safe by rejecting, since `JOB_STARTED` is once-only.)
  - Complete: allow only when `sr.Status == InProgress`. Otherwise `AizenBusinessException("SR_JOB_NOT_COMPLETABLE")`
    (blocks double-submit and out-of-order completion).
- Keep the actor resolution as-is (`UserInfoAccessor.UserInfo.UserId`). **Verify** `UserInfo.UserId` is populated under the
  BFF assertion path (the seed Start flow used it); if it resolves to `0`, fall back to a provider-derived id and note it in
  the report — do not silently write actor `0`.
- Evidence is optional now (`EvidenceFileId` nullable — real upload authorization is JD-5). `CompletionNotes` optional.

### 2. Module — expose on `ProviderJobsController` (provider-scoped, in `Controller/V1/Jobs`)
- `POST jobs/{assignmentId:long}/start` → `ProcessAsync(new StartServiceRequestAssignmentCommand(assignmentId))`.
- `POST jobs/{assignmentId:long}/complete` (body `SubmitServiceRequestCompletionRequest` — notes optional, evidence
  nullable) → `ProcessAsync(new SubmitServiceRequestCompletionCommand(assignmentId, body))`.
- Same controller/route as JD-1 (`api/v1/service-requests/provider`), provider identity via the assertion.

### 3. BFF — `ProviderJobsController` + remote-call
- `[AizenRemoteCallPost("/api/v1/service-requests/provider/jobs/{assignmentId}/start")]` and `.../complete` on
  `IProviderServiceRequestRemoteCall`.
- `POST /api/v1/provider/jobs/{assignmentId:long}/start` and `.../complete` (body `{ notes? }`) on the BFF controller,
  provider-scoped (resolver + holder, assertion), passthrough. Map `Refit.ApiException` → the module's business error
  (so `SR_JOB_NOT_STARTABLE` / `..._NOT_COMPLETABLE` / "Job not found" reach the SPA as the enveloped error).
- **Return value:** the SPA refetches the job detail after the action, so returning the existing command responses is
  enough — no new aggregate needed. (Optionally include the new `sr.Status` for an optimistic patch.)

## Constraints
- **Ownership-checked before mutating** — caller must own the assignment, else "Job not found". No client-sent profile id.
- **State-guarded** — Start only from Assigned/Scheduled; Complete only from InProgress. Idempotent side effects kept
  (`JOB_STARTED` once-only). No customer PII. Dates UTC/Postgres-safe. Money untouched.
- **Do NOT** add Pause/Resume/Schedule/Material actions — no domain commands exist. Owner Approve/Reject stays a
  customer-app action, not the provider BFF.

## Acceptance — observed
- `POST /provider/jobs/91001/start` as **provider2** on the seeded Assigned job → 200; SR 9011 → `InProgress`,
  assignment → `InProgress`, one `JOB_STARTED` message (not duplicated), a status-history row; the Jobs list/summary shift
  to Devam Eden.
- Re-`POST .../start` → rejected `SR_JOB_NOT_STARTABLE` (already InProgress).
- `POST /provider/jobs/91001/complete` (from InProgress, notes optional) → 200; SR → `CompletionSubmitted`, a completion
  row + `ServiceRequestCompletionSubmittedMessage`, status-history; re-complete → `SR_JOB_NOT_COMPLETABLE`.
- `POST` on a job owned by **another** provider → "Job not found".
- Actor id on the new status-history / system message is a real user id (not `0`).

## Report
Append to `REPORT_BACKEND.md` ("JD-2"): the start then complete transitions on job 91001 with statuses + the idempotency/
ownership rejections, and confirmation the actor id is populated. Unfinished is **not done**.

## Frontend (I build in parallel — FE-G)
Wire the state-aware primary action (İşi Başlat / Tamamlandı Bildir) with a single-confirm dialog (irreversible), a
completion modal (optional notes), success → invalidate job-detail + jobs list/summary, and read-only state banners
(CompletionSubmitted "Onay Bekleniyor", Completed "İş Tamamlandı"). Errors surface the coded messages.
