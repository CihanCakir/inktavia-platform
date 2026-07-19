# Seed: advance the SR 9011 job to InProgress (JOB_STARTED)

Follow-up to `SEED_ACCEPTED_JOB.md`. Move the seeded assignment **91001** (provider2, SR 9011) from `Assigned` to
**InProgress** using the existing Start flow, so we can see live: the **"Devam Eden" KPI = 1**, the jobs row status
**"Devam Ediyor"** with the stepper advanced, and the **JOB_STARTED** pill in the Messages thread. Small, idempotent,
Dev/Local only.

## Verified in source
- `assignment.Start()` sets assignment status → `InProgress` (no precondition guard).
- `StartServiceRequestAssignmentCommandHandler` does: `assignment.Start()`, `sr.ChangeStatus(InProgress)`, status
  history, and (20e) creates the idempotent `JOB_STARTED` system message + publishes `ServiceRequestMessageSentMessage`
  with `assignment.ProviderProfileId` so the pill reaches the provider live.
- After JB-6 the jobs list projects `sr.Status`, so the row will read `InProgress`; JB-2 summary counts `InProgress`.

## Work (prefer the real command; seeder fallback allowed)
Advance assignment **91001** to InProgress. **Preferred:** invoke `StartServiceRequestAssignmentCommand(91001)` via its
handler (provider2 identity) so every side effect fires as in production (SR→InProgress, JOB_STARTED message, realtime).

**Fallback (if wiring the command in a dev harness is impractical):** extend `SeedAcceptedJobAsync` to reach the same
end-state, idempotently:
1. If assignment 91001 status is not `InProgress` → `assignment.Start()`.
2. If SR 9011 status is not `InProgress` → `sr.ChangeStatus(InProgress)` (+ a status-history row).
3. If no `JOB_STARTED` system message on SR 9011 (`HasSystemMessageAsync(9011, "JOB_STARTED")`) → create it
   (`SenderType.System, MessageType.StatusChange, "JOB_STARTED"`) **and** publish
   `ServiceRequestMessageSentMessage { ServiceRequestId=9011, MessageId, SenderType=System,
   ProviderProfileId = assignment.ProviderProfileId }` so the pill arrives via realtime.
- Idempotent throughout: re-running does nothing if already InProgress + message present.

## Acceptance — observed
- SR 9011 status = `InProgress`; assignment 91001 status = `InProgress`.
- `GET /provider/jobs` → the job row status = **"InProgress"**; `GET /provider/jobs/summary` → `inProgress=1,
  active=1, total=1` (assigned back to 0).
- One `JOB_STARTED` system message on SR 9011 (idempotent — not duplicated), delivered as a content-free
  `MessageAdded`.
- (SPA, no new work) Jobs KPI "Devam Eden" = 1, the table badge reads "Devam Ediyor", the stepper advances to the
  In-Progress stage, and the Messages thread for SR 9011 shows the "İş Başlatıldı" pill.

## Report
Append to `REPORT_BACKEND.md` ("SEED start job"): the jobs row + summary showing InProgress, and the JOB_STARTED
message present once. Unfinished is **not done**.
