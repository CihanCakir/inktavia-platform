# JD-3 — Backend: Provider work logs (list + add) on the Job Workspace

Expose the existing work-log command/query as provider-scoped BFF endpoints so the Job Workspace can show a chronological
work-log feed and let the provider add entries (arrived, inspection, note, paused, resumed, completed…). The command,
query, entity, DTO and repository all exist and are keyed by **AssignmentId** — this phase adds the missing **ownership
guards** and the provider endpoints (same shape as JD-1/JD-2).

## Verified in source (2026-07-17)
- `AddServiceRequestWorkLogCommand(long assignmentId, AddServiceRequestWorkLogRequest req)` → handler loads assignment+SR,
  creates `ServiceRequestWorkLogEntity`, `assignment.AddWorkLog(...)`, publishes `WorkLogAdded` realtime. Actor from
  `_info.UserInfoAccessor.UserInfo.UserId`. **No SR status change** (pure log).
- `AddServiceRequestWorkLogRequest`: `{ LogType, Title, Description?, LocationLatitude?, LocationLongitude?,
  AttachmentFileId? }`.
- `ServiceRequestWorkLogType`: GeneralNote, ArrivedAtVessel, InspectionStarted, InspectionCompleted, WorkStarted,
  MaterialRequired, AdditionalIssueFound, WaitingForOwnerApproval, WorkPaused, WorkResumed, WorkCompleted (codes stay
  codes; the SPA localizes).
- `GetServiceRequestWorkLogsQuery(long assignmentId)` → `GetByAssignmentIdAsync` → `ServiceRequestWorkLogDto`
  `{ Id, ServiceRequestAssignmentId, ProviderUserId, LogType, Title, Description?, LocationLatitude?, LocationLongitude?,
  AttachmentFileId?, LoggedAt }`.
- **Gap — no ownership guard** in either handler (the add handler doesn't verify the caller owns the assignment; the get
  handler fetches by assignment with no assignment load). Mirror the JD-2 fix.
- Existing `ServiceRequestWorkLogController` (`/service-requests/{serviceRequestId}/work-logs/...`) is the generic module
  controller — **not** the provider-scoped surface. JD-3 exposes on `ProviderJobsController` instead.
- `AttachmentFileId` is optional — real evidence upload/authorization is **JD-5**. Accept null now.

## Work

### 1. Module — ownership guards (mirror JD-2)
In **`AddServiceRequestWorkLogCommandHandler`** and **`GetServiceRequestWorkLogsQueryHandler`**, after loading the
assignment (the get handler must load it first): resolve `providerProfileId` from
`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`; if `0` →
`AizenBusinessException("Provider identity could not be resolved.")`; if
`assignment.ProviderProfileId != providerProfileId` → `AizenBusinessException("Job not found.")` (fail closed). Keep the
`ProviderUserId`/actor resolution as-is; keep the `WorkLogAdded` realtime.

### 2. Module — expose on `ProviderJobsController` (provider-scoped, `Controller/V1/Jobs`)
- `GET jobs/{assignmentId:long}/work-logs` → `GetServiceRequestWorkLogsQuery(assignmentId)`.
- `POST jobs/{assignmentId:long}/work-logs` (body `AddServiceRequestWorkLogRequest`) →
  `AddServiceRequestWorkLogCommand(assignmentId, body)`.
- Same controller/route base as JD-1/JD-2 (`api/v1/service-requests/provider`), provider identity via the assertion.

### 3. BFF — `ProviderJobsController` + remote-call
- `[AizenRemoteCallGet("/api/v1/service-requests/provider/jobs/{assignmentId}/work-logs")]` and the matching
  `[AizenRemoteCallPost(".../work-logs")]` on `IProviderServiceRequestRemoteCall`.
- `GET /api/v1/provider/jobs/{assignmentId:long}/work-logs` and `POST .../work-logs` (body =
  `AddServiceRequestWorkLogRequest`) on the BFF controller, provider-scoped (resolver + holder, assertion), passthrough +
  `Refit.ApiException` → business error (via the existing `ExtractError`).
- No vessel enrichment needed here. Return the module responses as-is.

## Constraints
- **Ownership-checked** before read/write — caller must own the assignment, else "Job not found". No client-sent profile.
- Work log adds **no SR status change** (independent of JD-2 lifecycle). `AttachmentFileId` optional (evidence is JD-5).
- Codes stay codes (`LogType`), money none, dates UTC/Postgres-safe. No customer PII.

## Acceptance — observed
- `GET /provider/jobs/91001/work-logs` → the assignment's logs (chronological), `LoggedAt` set, `LogType` codes.
- `POST /provider/jobs/91001/work-logs` `{ logType: "GeneralNote", title: "...", description: "..." }` as **provider2** →
  201/200; the new log appears in the subsequent GET; `WorkLogAdded` realtime fired.
- `GET`/`POST` on an assignment owned by **another** provider → "Job not found".

## Report
Append to `REPORT_BACKEND.md` ("JD-3"): a work log added + listed for job 91001, and the cross-provider "not found"
rejection. Unfinished is **not done**.

## Frontend (I build in parallel — FE-D)
Job Workspace "İş Kayıtları" card: chronological entries (LogType icon + Title + Description + LoggedAt), localized log
types, and a "Çalışma Kaydı Ekle" modal (LogType select + Title + optional note). Success → invalidate the work-logs
query. Evidence/photo on a log is deferred to JD-5.
