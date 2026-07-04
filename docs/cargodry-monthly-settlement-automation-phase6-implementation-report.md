# CargoDry Monthly Settlement Automation — Phase 6 Implementation Report

**Date:** 2026-07-04  
**Author:** Inktavia Marine OS Engineering  
**Phase:** 6 — Settlement Automation Job Layer

---

## A. Scope Summary

Phase 6 implements a safe, auditable, opt-in automation layer over the CargoDry monthly sell-through settlement workflow. It adds a scheduled job + manual trigger API that can preview and optionally execute settlement operations in bulk, while enforcing all business-critical hard constraints.

---

## B. Hard Constraints Enforced

| Constraint | Enforcement Point |
|---|---|
| `AutoCompletePayout = false` always | Entity domain invariant in `Create()` factory |
| `DryRun` default everywhere | Command/BFF/Job all default `Mode = DryRun` |
| Job `IsActive = false` | `CargoDryMonthlySettlementAutomationJob.IsActive` returns `false` |
| Never marks settlement `Settled` | Automation service only calls `Resolve` / `PreparePayment` / `PrepareInvoice` |
| Never calls payout lifecycle commands | `ProcessSettlementAsync` contains no approve/processing/complete/fail calls |
| Does not bypass CQRS handlers | Uses `ISender` to dispatch existing command handlers, never EF directly |
| `AutoPreparePayment = false` default | Job + Command defaults |
| `AutoPrepareInvoice = false` default | Job + Command defaults |

---

## C. New Files Created

### Enums (Abstraction layer)
- `CargoDrySettlementAutomationRunStatus` — Pending, Running, Completed, PartiallyCompleted, Failed, DryRunCompleted
- `CargoDrySettlementAutomationMode` — DryRun, Live

### Domain Entities
- `CargoDrySettlementAutomationRunEntity` — extends `AizenEntityWithAudit`
  - Fields: RunCode (SAR-{YYYYMM}-{seq:D4}), TargetYearMonth, Mode, Status, 3 safety flags, Trigger fields, 5 aggregate counters, Note, ErrorSummary, CreatedAtUtc
  - Navigation: `IReadOnlyList<CargoDrySettlementAutomationRunItemEntity> RunItems`
  - Factory: `Create(...)`, domain methods: `MarkRunning`, `Complete`, `Fail`, `AddItem`
- `CargoDrySettlementAutomationRunItemEntity` — extends `AizenEntity`
  - Fields: RunId (FK), SettlementId (denormalized), SettlementCode, ProviderProfileId, ProductCode, CurrencyCode, PeriodYearMonth, StatusBefore, Action, Success, ErrorMessage, 3 attribution counters, ProcessedAtUtc

### Abstraction DTOs
- `CargoDrySettlementAutomationRunDto`
- `CargoDrySettlementAutomationRunItemDto`
- `CargoDrySettlementAutomationPreviewItemDto`
- `CargoDrySettlementAutomationPreviewDto`

### Repository Interface
- `ICargoDrySettlementAutomationRunRepository` — GetByIdAsync, GetByCodeAsync, GetPagedAsync, GetNextRunSequenceAsync, AddAsync, SaveChangesAsync

### Service Interface
- `ICargoDryMonthlySettlementAutomationService` — GetPreviewAsync, RunAsync

### EF Configuration
- `CargoDrySettlementAutomationRunEntityConfiguration` — table `settlement_automation_runs`, schema `cargodry`
- `CargoDrySettlementAutomationRunItemEntityConfiguration` — table `settlement_automation_run_items`, CASCADE delete on RunId FK

### Repository Implementation
- `CargoDrySettlementAutomationRunRepository`

### Application Service
- `CargoDryMonthlySettlementAutomationService` — `GetPreviewAsync`, `RunAsync`, `ExecuteDryRunAsync`, `ExecuteLiveRunAsync`, `ProcessSettlementAsync`

### CQRS — Module Layer
- `RunCargoDryMonthlySettlementAutomationCommand` + Validator + Handler
- `GetCargoDryMonthlySettlementAutomationPreviewQuery` + Handler
- `GetCargoDrySettlementAutomationRunsPagedQuery` + Handler
- `GetCargoDrySettlementAutomationRunDetailQuery` + Handler

### Module Controller Endpoints (4)
```
GET  /api/v1/cargodry/admin/commercial/settlement-automation/preview
POST /api/v1/cargodry/admin/commercial/settlement-automation/run
GET  /api/v1/cargodry/admin/commercial/settlement-automation/runs
GET  /api/v1/cargodry/admin/commercial/settlement-automation/runs/{id}
```

### BFF Layer

**Remote Call Methods** (added to `IAdminCargoDryBffRemoteCall`):
- `GetSettlementAutomationPreviewAsync`
- `RunSettlementAutomationAsync`
- `GetSettlementAutomationRunsAsync`
- `GetSettlementAutomationRunDetailAsync`

**BFF DTOs** (added to `CargoDryCommercialBffDtos.cs`):
- `CargoDrySettlementAutomationRunBffDto`
- `CargoDrySettlementAutomationRunItemBffDto`
- `CargoDrySettlementAutomationRunsPagedBffDto`
- `CargoDrySettlementAutomationPreviewItemBffDto`
- `CargoDrySettlementAutomationPreviewBffDto`
- `RunSettlementAutomationBffRequest`

**BFF Handlers:**
- `GetCargoDrySettlementAutomationPreviewBffQuery` + Handler
- `RunCargoDryMonthlySettlementAutomationBffCommand` + Handler
- `GetCargoDrySettlementAutomationRunsPagedBffQuery` + Handler
- `GetCargoDrySettlementAutomationRunDetailBffQuery` + Handler

**BFF Controller Endpoints** (added to `AdminCargoDryController`):
```
GET  /api/v1/admin-panel/cargodry/commercial/settlement-automation/preview
POST /api/v1/admin-panel/cargodry/commercial/settlement-automation/run
GET  /api/v1/admin-panel/cargodry/commercial/settlement-automation/runs
GET  /api/v1/admin-panel/cargodry/commercial/settlement-automation/runs/{id}
```

### Job
- `CargoDryMonthlySettlementAutomationJob`
  - `IsActive = false` — disabled by default
  - `CronExpression = "0 2 1 * *"` — 02:00 UTC on the 1st of every month
  - Targets previous month (job runs on the 1st → settles prior month)
  - RunCode format for job-triggered runs: `SAR-{YYYYMM}-9000` (reserved scheduler range)
  - Always runs in `DryRun` mode — mode must be explicitly changed for live execution
  - Registered automatically via `AddAizenRecurringJob()` assembly scanning

### EF Migration
- `20260704090000_AddCargoDrySettlementAutomationRuns`
  - Creates `settlement_automation_runs` table with all fields and 6 indexes
  - Creates `settlement_automation_run_items` table with FK + 4 indexes
  - `Down()` drops both tables (items first due to FK dependency)

---

## D. Modified Files

| File | Change |
|---|---|
| `CargoDryDbContext.cs` | Added `SettlementAutomationRuns` + `SettlementAutomationRunItems` DbSets |
| `CargoDry.Repository/DependencyInjection.cs` | Registered `ICargoDrySettlementAutomationRunRepository` |
| `CargoDry.Application/DependencyInjection.cs` | Registered `ICargoDryMonthlySettlementAutomationService` |
| `ICargoDrySellThroughSettlementRepository` | Added `GetForYearMonthAsync` method |
| `CargoDrySellThroughSettlementRepository` | Implemented `GetForYearMonthAsync` with YYYYMM decomposition |
| `CargoDryCommercialController.cs` | Added Phase 6 section with 4 endpoints |
| `IAdminCargoDryBffRemoteCall.cs` | Added 4 Phase 6 remote call methods |
| `CargoDryCommercialBffDtos.cs` | Appended 6 BFF DTO classes |
| `AdminCargoDryController.cs` | Added 4 Phase 6 endpoints + `RunSettlementAutomationBodyRequest` |

---

## E. DryRun vs Live Execution

| Aspect | DryRun | Live |
|---|---|---|
| Mutations | None | `ResolveMonthlySellThroughSettlement` dispatched per eligible Pending settlement |
| Payment prep | No | Only if `AutoPreparePayment = true` |
| Invoice prep | No | Only if `AutoPrepareInvoice = true` AND `AutoPreparePayment = true` |
| Run status | `DryRunCompleted` | `Completed` / `PartiallyCompleted` / `Failed` |
| Run items | Populated with predicted actions | Populated with actual outcomes |
| Payout commands | Never called | Never called (hard constraint) |
| Settlement → Settled | Never | Never (hard constraint) |

---

## F. Settlement Eligibility Logic

The automation service only processes settlements where:
- `PeriodStartUtc.Year * 100 + PeriodStartUtc.Month == TargetYearMonth`
- `Status ∈ { Pending, ReadyForSettlement }`

For `Pending` settlements, `ResolveMonthlySellThroughSettlement` is dispatched first, which validates all attributions are resolved before marking `ReadyForSettlement`.

For `ReadyForSettlement` settlements, payment/invoice preparation is dispatched if the respective flags are enabled.

---

## G. RunCode Format

| Source | Format | Example |
|---|---|---|
| Manual / API trigger | `SAR-{YYYYMM}-{seq:D4}` | `SAR-202606-0001` |
| Scheduler / Job | `SAR-{YYYYMM}-9000` | `SAR-202606-9000` |

Sequence 9000 is reserved for scheduler-triggered runs and avoids conflicts with the 4-digit manual sequence space (0001–8999).

---

## H. Enabling the Recurring Job

The job is disabled by default (`IsActive = false`). To enable:

1. Discuss with business — confirm Live mode execution is wanted
2. Override `IsActive` via configuration injection (or subclass override)
3. If Live mode is desired, change default `Mode` in the job from `DryRun` to `Live`
4. Monitor run results via the `/runs` and `/runs/{id}` endpoints

The job should NOT be enabled in production until at least one manual Live run has been validated against the preview output.

---

## I. Idempotency Notes

- RunCode uniqueness is enforced at the DB level (unique index on `RunCode`)
- The job uses `SAR-{YYYYMM}-9000` — only one job-triggered run per month can succeed
- Manual runs use `GetNextRunSequenceAsync` which increments atomically
- Per-settlement errors are caught, recorded as error items, and processing continues — no full rollback

---

## J. Error Handling

Errors are handled at two levels:

1. **Per-settlement**: `try/catch` in `ProcessSettlementAsync`. Error recorded as an error `RunItem`. Run continues with next settlement.
2. **Run-level**: Any unhandled exception in `RunAsync` calls `run.Fail(errorSummary, nowUtc)` and persists the failed run.

The `ErrorSummary` field on the run entity aggregates per-settlement error messages (up to 4000 chars).

---

## K. DI Registration Summary

All Phase 6 services use scoped lifetime:

```csharp
// CargoDry.Repository/DependencyInjection.cs
services.AddScoped<ICargoDrySettlementAutomationRunRepository, CargoDrySettlementAutomationRunRepository>();

// CargoDry.Application/DependencyInjection.cs
services.AddScoped<ICargoDryMonthlySettlementAutomationService, CargoDryMonthlySettlementAutomationService>();

// Job: auto-discovered via AddAizenRecurringJob() assembly scanning
// CargoDryMonthlySettlementAutomationJob — IsActive = false
```

---

## L. Build Verification Checklist

Before running migrations in staging/production:

- [ ] `dotnet build` passes on `Aizen.Modules.CargoDry.Repository`
- [ ] `dotnet build` passes on `Aizen.Modules.CargoDry.Application`
- [ ] `dotnet build` passes on `Aizen.Modules.CargoDry`
- [ ] `dotnet build` passes on `Aizen.Bff.AdminPanel.Application`
- [ ] `dotnet build` passes on `Aizen.Bff.AdminPanel`
- [ ] `dotnet ef migrations list` shows `20260704090000_AddCargoDrySettlementAutomationRuns` as Pending
- [ ] `dotnet ef database update` applies migration without errors
- [ ] GET preview endpoint returns 200 for a valid `targetYearMonth`
- [ ] POST run endpoint with `mode=1` (DryRun) returns a run with status `DryRunCompleted`
- [ ] GET runs endpoint returns paginated list
- [ ] GET run detail endpoint returns run with RunItems

---

## M. Phase 7 (Post-MVP Considerations)

These are explicitly NOT part of Phase 6 and should only be considered after business validation:

- **Enable job in production** — after manual Live run validation
- **Live mode from job** — requires deliberate configuration change + sign-off
- **Admin Web UI** — not implemented; all interaction via BFF API endpoints
- **ProviderResale pivot** — out of scope
- **Automatic payout completion** — explicitly prohibited; `AutoCompletePayout` is always `false`
- **Iyzico live payout integration** — separate phase, separate spec

---

*End of Phase 6 Implementation Report.*
