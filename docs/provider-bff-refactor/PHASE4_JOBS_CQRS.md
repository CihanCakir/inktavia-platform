# Phase 4 — Jobs controller → gold standard (CQRS + typed + PRT)

`JobsController` has 9 endpoints. Only `GET` (GetJobs) is already CQRS/typed/PRT; the other 8 use the **service-locator
anti-pattern** (`HttpContext.RequestServices.GetRequiredService<…>()`) to grab the resolver/holder/remote-call inline,
return `IActionResult`, and some hand-roll `Ok(new { header = new { isSuccess = true } })`. Move all of that into
proper handlers. **Apply the Phase-2-fix conventions** (one class per file, validators, DTOs from module Abstraction).

## Target (per `ProviderMeController` + `PHASE2_FIX_FILE_SPLIT_AND_ABSTRACTION.md`)
- Controller injects **only** `IAizenCQRSProcessor`. Every endpoint: `[ProducesResponseType(typeof(T), 200)]`,
  returns `Task<AizenApiResponse<T>>`, body `var result = await _cqrs.ProcessAsync(new …Bff{Query|Command}{…}, ct); return SetResponse(result);`.
- Handlers (constructor-injected deps — **no service locator**) own: `_resolver.ResolveAsync(ct)` + ProfileId guard,
  the remote-call, the Refit-error → `AizenBusinessException` mapping, and (for detail) the vessel enrichment.
- Feature folder `Jobs/`, namespace `…Application.Jobs`. `GetProviderJobs` query already exists there — leave it,
  just confirm it's one-class-per-file.
- Response/request DTOs from the module Abstractions (`Aizen.Modules.ServiceRequest.Abstraction`,
  `Aizen.Modules.Vessel.Abstraction`) — nothing inline. Use each remote-call's `.Body` type.

## The 9 operations
| Endpoint | Op (folder) | Kind | Remote call(s) | Response |
|---|---|---|---|---|
| `GET` | `Jobs/Query/GetProviderJobs/` (exists) | Query | `GetProviderJobs` (already handler) | `GetProviderJobsResponse` |
| `GET {assignmentId}` | `Jobs/Query/GetJobDetailBff/` | Query | `GetProviderJobDetail(id)` **+ vessel enrichment** | `.Body` type |
| `GET {assignmentId}/work-logs` | `Jobs/Query/GetJobWorkLogsBff/` | Query | `GetWorkLogs(id)` | `.Body` type |
| `POST {assignmentId}/work-logs` | `Jobs/Command/AddJobWorkLogBff/` | Command | `AddWorkLog(id, body)` | `.Body` type |
| `POST {assignmentId}/start` | `Jobs/Command/StartJobBff/` | Command | `StartJob(id)` | `bool` (true) |
| `POST {assignmentId}/complete` | `Jobs/Command/CompleteJobBff/` | Command | `CompleteJob(id, body)` | `bool` (true) |
| `GET summary` | `Jobs/Query/GetJobsSummaryBff/` | Query | `GetProviderJobsSummary()` | `.Body` type |
| `GET workload` | `Jobs/Query/GetJobsWorkloadBff/` | Query (`Weeks=6`) | `GetProviderJobsWorkload(Weeks)` | `.Body` type |
| `GET action-required` | `Jobs/Query/GetJobsActionRequiredBff/` | Query | `GetProviderJobsActionRequired()` | `.Body` type |

Routes/verbs/params unchanged. Bodies bind to the module Abstraction requests:
`AddServiceRequestWorkLogRequest` (Request/WorkLog), `SubmitServiceRequestCompletionRequest` (Request/Completion).

## Preserve exactly (do not lose)
1. **GetJobDetail vessel enrichment.** Move the whole block into `GetJobDetailBffQueryHandler` (inject
   `IServiceRequestRemoteCall`, `IVesselRemoteCall`, `IAizenDistributedCache`, `IProviderProfileResolver`,
   `IProviderIdentityHolder`): after `GetProviderJobDetail`, if `Body.Request.VesselId > 0`, read cache key
   `vessel:summary:v3:{VesselId}` (`GetNoHash`), else call `IVesselRemoteCall.GetSummaries(vesselId)` and cache the
   summary for 10 min, then map the same 14 fields onto `Request` (`VesselName ??=`, VesselTypeCode, Brand, Model,
   LengthValue/UnitCode, Year, MaterialCode, RegistrationNumber, BeamValue/UnitCode, DraftValue/UnitCode). Keep the
   enrichment **non-fatal** (swallow its exceptions) exactly as today. Wrap the primary call so a Refit error →
   `AizenBusinessException("Job not found.")`.
2. **Refit-error mapping.** Move the `ExtractError(Refit.ApiException)` helper into the handlers (or a shared
   `Common` helper) and keep the same friendly messages: detail → "Job not found."; work-logs GET → "Job not found.";
   AddWorkLog → "Failed to add work log."; start → "Failed to start job."; complete → "Failed to complete job." —
   using the module `header.errorMessage` when present.
3. Replace the hand-rolled `Ok(new { header … })` on start/complete with `bool` (true) command results returned via
   `SetResponse`.

## Validators
`AddJobWorkLogBffCommand`, `CompleteJobBffCommand`: `AssignmentId > 0` + body not null. `StartJobBffCommand`,
`GetJobDetailBffQuery`, `GetJobWorkLogsBff`: `AssignmentId > 0`. `GetJobsWorkloadBff`: `Weeks` in 1..52.

## Acceptance
- `JobsController` injects only `IAizenCQRSProcessor`; no `GetRequiredService`, no `RemoteCall`, no `IActionResult`
  in the controller. Every endpoint typed + `[ProducesResponseType]`.
- 9 operations under `Jobs/{Query|Command}/{Op}/`, one class per file, validators present; enrichment + error
  mapping intact in handlers.
- Builds. After rebuild: jobs list + KPI summary + workload + action-required, job detail (**with vessel specs still
  populated**), work-logs list/add, start, complete — all behave exactly as before (verify on the Jobs + Job detail
  screens; the before/after + vessel spec panel must still fill).

## Report
Append to `REPORT_BACKEND.md` ("Phase 4"): Jobs converted to CQRS (6 queries + 3 commands under `Jobs/Query|Command`),
service-locator removed, typed + PRT, vessel enrichment + Refit-error mapping moved into handlers, hand-rolled
envelopes removed.
