# Phase 4D — Provider Payout Lifecycle & Settlement Closure
## Implementation Report

**Date:** 2026-07-03  
**Phase:** 4D (follows Phase 4B: Payment Preparation, Phase 4C: Invoice Preparation)  
**Status:** ✅ Complete — pending `dotnet build` verification on local machine

---

## A. Scope & Objectives

Phase 4D implements the full payout execution lifecycle for CargoDry sell-through settlements. After a settlement has a PayoutRecord (Phase 4B) and an Invoice (Phase 4C), admin operators can drive the payout through the following state machine:

```
ReadyForSettlement
  → Scheduled / PaymentPrepared (Phase 4B)
  → InvoicePrepared (Phase 4C)
  → PayoutApproved          [approve-payout]
  → PayoutProcessing         [mark-payout-processing — optional]
  → PayoutCompleted          [complete-payout — THE ONLY path to Settled]
  → Settled
```

Additionally:
- `fail-payout` can be called from any non-Settled state; settlement remains `Scheduled` (allows retry)
- `payout-execution-preview` provides a live eligibility snapshot before any action

**Hard constraints honoured:**
1. No live Iyzico gateway call
2. No automatic bank transfer
3. No scheduled settlement job
4. No refund/credit note automation
5. No Admin Web frontend pages
6. `CompleteCargoDrySettlementPayoutCommand` is the ONLY command that marks settlement `Settled`
7. Settlement status remains `Scheduled` after Approve and Processing transitions
8. `MarkPayoutFailed` does NOT set settlement `Settled`
9. Completed payout requires `ManualPaymentReference` (stored as `GatewayPayoutId`)
10. Failed payout requires `FailureReason`

---

## B. Domain Changes

### B1. `PayoutStatus` enum — `Aizen.Modules.Payment.Abstraction/Enum/PayoutStatus.cs`

Added:
```csharp
Approved = 7
```

### B2. `PayoutRecordEntity` — `Aizen.Modules.Payment.Domain/Entities/Payout/PayoutRecordEntity.cs`

**7 new audit fields:**
- `ApprovedAtUtc` — UTC timestamp when admin approved payout
- `ApprovedByUserId` — admin user who approved
- `ProcessingAtUtc` — UTC timestamp when payout entered Processing
- `ProcessingByUserId` — admin user who initiated processing
- `CompletedByUserId` — admin user who confirmed manual completion
- `FailedAtUtc` — UTC timestamp of failure recording
- `FailedByUserId` — admin user who recorded failure

**4 new domain methods:**
- `Approve(long approvedByUserId, DateTime approvedAtUtc, string? note)` → `Pending → Approved`
- `MarkProcessingByAdmin(long processedByUserId, DateTime processingAtUtc, string? externalReference, string? note)` → `Approved/Pending → Processing`
- `MarkCompletedManual(long completedByUserId, DateTime completedAtUtc, string manualPaymentReference, string? note)` → `any non-terminal → Completed` (idempotent if already Completed)
- `MarkFailedByAdmin(long failedByUserId, DateTime failedAtUtc, string reason, string? externalReference, string? note)` → `any non-Completed → Failed`

### B3. `CargoDrySellThroughSettlementEntity` — `Aizen.Modules.CargoDry.Domain/Entities/CargoDrySellThroughSettlementEntity.cs`

**5 new Phase 4D fields:**
- `PayoutCompletedAtUtc` — UTC timestamp of payout confirmation
- `PayoutCompletedByUserId` — admin user who confirmed
- `PayoutCompletionReference` — external bank/EFT reference (max 500)
- `PayoutFailureReason` — failure context (max 1000)
- `PayoutLifecycleNote` — general admin note (max 1000)

**2 new domain methods:**
- `MarkPayoutCompleted(long userId, DateTime nowUtc, string reference, string? note)` — sets `Status = Settled` + closure fields
- `MarkPayoutFailed(string reason, string? note)` — records failure fields; does NOT change `Status`

---

## C. EF Core / PostgreSQL Changes

### C1. Payment Migration — `AddPayoutRecordLifecycleFields`

**File:** `Modules/Payment/src/Aizen.Modules.Payment.Repository/Migrations/20260703160000_AddPayoutRecordLifecycleFields.cs`

Adds 7 nullable columns to `payment.payout_records`:

| Column | Type |
|---|---|
| `ApprovedAtUtc` | `timestamp with time zone` |
| `ApprovedByUserId` | `bigint` |
| `CompletedByUserId` | `bigint` |
| `FailedAtUtc` | `timestamp with time zone` |
| `FailedByUserId` | `bigint` |
| `ProcessingAtUtc` | `timestamp with time zone` |
| `ProcessingByUserId` | `bigint` |

**EF Configuration** (`PayoutRecordConfiguration.cs`) — 7 `b.Property(x => x.XxxField)` entries added, all nullable, no custom column type needed (EF infers `bigint` and `timestamp with time zone` from C# types).

### C2. CargoDry Migration — `AddCargoDrySettlementPayoutCompletionFields`

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Migrations/20260703160001_AddCargoDrySettlementPayoutCompletionFields.cs`

Adds 5 nullable columns to `cargodry.sell_through_settlements`:

| Column | Type | Max Length |
|---|---|---|
| `PayoutCompletedAtUtc` | `timestamp with time zone` | — |
| `PayoutCompletedByUserId` | `bigint` | — |
| `PayoutCompletionReference` | `character varying(500)` | 500 |
| `PayoutFailureReason` | `character varying(1000)` | 1000 |
| `PayoutLifecycleNote` | `character varying(1000)` | 1000 |

### C3. Model Snapshots

Both `PaymentDbContextModelSnapshot.cs` and `CargoDryDbContextModelSnapshot.cs` updated in-place with the new columns. Both Designer.cs files generated from the updated snapshots.

---

## D. Payment Module — Lifecycle Service

### D1. Interface — `ICargoDrySettlementPayoutLifecycleService`

**File:** `Modules/Payment/src/Aizen.Modules.Payment.Abstraction/Interface/ICargoDrySettlementPayoutLifecycleService.cs`

```csharp
Task<CargoDryPayoutLifecycleResultDto> ApproveAsync(long payoutRecordId, long approvedByUserId, string? note, CancellationToken ct);
Task<CargoDryPayoutLifecycleResultDto> MarkProcessingAsync(long payoutRecordId, long processedByUserId, string? externalReference, string? note, CancellationToken ct);
Task<CargoDryPayoutLifecycleResultDto> CompleteManualAsync(long payoutRecordId, long completedByUserId, string manualPaymentReference, string? note, CancellationToken ct);
Task<CargoDryPayoutLifecycleResultDto> FailAsync(long payoutRecordId, long failedByUserId, string reason, string? externalReference, string? note, CancellationToken ct);
Task<CargoDryPayoutLifecycleResultDto?> GetPayoutStateAsync(long payoutRecordId, CancellationToken ct);
```

### D2. Result DTO — `CargoDryPayoutLifecycleResultDto`

**File:** `Modules/Payment/src/Aizen.Modules.Payment.Abstraction/Model/Result/CargoDryPayoutLifecycleResultDto.cs`

Carries full payout state including: `PayoutRecordId`, `PayoutStatus`, `ProviderProfileId`, `Amount`, `CurrencyCode`, `ExternalReference`, all audit timestamps and user IDs, `FailureReason`, `AdminNote`, `AlreadyCompleted`, `Message`.

### D3. Implementation — `CargoDrySettlementPayoutLifecycleService`

**File:** `Modules/Payment/src/Aizen.Modules.Payment.Application/Services/CargoDrySettlementPayoutLifecycleService.cs`

Injects `IPayoutRecordRepository`. Each method:
1. Loads payout by ID via repository
2. Calls domain method on entity
3. Returns `CargoDryPayoutLifecycleResultDto` mapped from updated entity

`ApproveAsync` loads payout directly by ID (not via source validation) — CargoDry command handlers validate settlement ownership before calling the service.

---

## E. CargoDry Application Layer — Commands & Queries

### E1. Preview Query

**Files:** `GetCargoDrySettlementPayoutExecutionPreviewQuery.cs` + Handler

Returns full eligibility snapshot:
- Settlement identity (`SettlementId`, `SettlementCode`, `Status`)
- Financials (`ProviderPayoutAmount`, `CurrencyCode`, `ProductCode`, period dates)
- Phase prerequisites: `PaymentPrepared` (Phase 4B), `InvoicePrepared` (Phase 4C)
- Live payout state from Payment module (fetched via `ICargoDrySettlementPayoutLifecycleService.GetPayoutStateAsync`)
- Eligibility flags: `CanApprovePayout`, `CanMarkProcessing`, `CanCompletePayout`, `CanFailPayout`
- `BlockingReasons` list + `RecommendedActions` list

`CanCompletePayout` is true when payout status is `Approved`, `Processing`, OR `Pending` (allows direct shortcut from Pending → Completed without going through Approve).

### E2. Commands

| Command | Business Rule | Settlement Status After |
|---|---|---|
| `ApproveCargoDrySettlementPayoutCommand` | Requires `Scheduled` + `PayoutRecordId.HasValue` | `Scheduled` (unchanged) |
| `MarkCargoDrySettlementPayoutProcessingCommand` | Requires `Scheduled` + PayoutRecord in `Approved` state | `Scheduled` (unchanged) |
| `CompleteCargoDrySettlementPayoutCommand` | Requires `Scheduled` + `PayoutRecordId.HasValue` + `InvoiceId.HasValue` | **`Settled`** |
| `FailCargoDrySettlementPayoutCommand` | Cannot fail an already-`Settled` settlement | `Scheduled` (unchanged) |

All commands use `AizenBusinessException` (not `InvalidOperationException`) for business rule violations.

**Idempotency:** `CompleteCargoDrySettlementPayoutCommand` detects `AlreadyCompleted = true` when settlement is already `Settled` and returns current payout state without re-running completion.

---

## F. Module Controller — `CargoDryCommercialController`

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry/Controllers/CargoDryCommercialController.cs`

5 Phase 4D endpoints added after Phase 4C section:

```
GET  /api/v1/cargodry/admin/commercial/settlements/{id}/payout-execution-preview
POST /api/v1/cargodry/admin/commercial/settlements/{id}/approve-payout
POST /api/v1/cargodry/admin/commercial/settlements/{id}/mark-payout-processing
POST /api/v1/cargodry/admin/commercial/settlements/{id}/complete-payout
POST /api/v1/cargodry/admin/commercial/settlements/{id}/fail-payout
```

4 inline request models added:
- `ApproveSettlementPayoutRequest` — `ApprovedByUserId`, `Note?`
- `MarkSettlementPayoutProcessingRequest` — `ProcessedByUserId`, `ExternalReference?`, `Note?`
- `CompleteSettlementPayoutRequest` — `CompletedByUserId`, `ManualPaymentReference` (required), `Note?`
- `FailSettlementPayoutRequest` — `FailedByUserId`, `FailureReason` (required), `ExternalReference?`, `Note?`

Response shape: `Ok(new { result.Settlement, result.PayoutResult })` for all except Complete which also returns `AlreadyCompleted`.

---

## G. BFF Layer — DTOs

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminCargoDry/Dto/CargoDryCommercialBffDtos.cs`

**Phase 4D fields added to existing DTOs:**
- `CargoDrySellThroughSettlementBffDto` (detail): +6 fields (`PayoutCompletedAtUtc`, `PayoutCompletedByUserId`, `PayoutCompletionReference`, `PayoutFailureReason`, `PayoutLifecycleNote`, `CreatedAtUtc`)
- `CargoDrySellThroughSettlementListItemBffDto` (list): +4 fields

**3 new BFF DTOs:**
- `CargoDrySettlementPayoutExecutionPreviewBffDto` — full preview including prerequisites, live payout state, eligibility flags
- `CargoDryPayoutLifecycleResultBffDto` — mirrors `CargoDryPayoutLifecycleResultDto` with `int` for status (BFF convention)
- `CargoDrySettlementPayoutLifecycleResponseBffDto` — wrapper: `Settlement?`, `PayoutResult?`, `AlreadyCompleted`

---

## H. BFF Layer — Remote Calls & Request DTOs

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/CargoDry/CargoDryRemoteRequests.cs`

4 new request DTOs:
- `ApproveCargoDrySettlementPayoutBffRequest`
- `MarkCargoDrySettlementPayoutProcessingBffRequest`
- `CompleteCargoDrySettlementPayoutBffRequest`
- `FailCargoDrySettlementPayoutBffRequest`

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IAdminCargoDryBffRemoteCall.cs`

5 new methods using `[AizenRemoteCallGet]` / `[AizenRemoteCallPost]` + `[AizenRemoteCallBody]` attributes:
- `GetSettlementPayoutExecutionPreviewAsync`
- `ApproveSettlementPayoutAsync`
- `MarkSettlementPayoutProcessingAsync`
- `CompleteSettlementPayoutAsync`
- `FailSettlementPayoutAsync`

---

## I. BFF Application Layer — Handlers

10 new files (5 command/query + 5 handler files):

| Namespace | Handler | Remote Call |
|---|---|---|
| `GetCargoDrySettlementPayoutExecutionPreview` | `BffQueryHandler` | `GetSettlementPayoutExecutionPreviewAsync` |
| `ApproveCargoDrySettlementPayout` | `BffCommandHandler` | `ApproveSettlementPayoutAsync` |
| `MarkCargoDrySettlementPayoutProcessing` | `BffCommandHandler` | `MarkSettlementPayoutProcessingAsync` |
| `CompleteCargoDrySettlementPayout` | `BffCommandHandler` | `CompleteSettlementPayoutAsync` → returns `AlreadyCompleted` |
| `FailCargoDrySettlementPayout` | `BffCommandHandler` | `FailSettlementPayoutAsync` |

All handlers use `[DocumentationInfo]` attribute and inject `IAdminCargoDryBffRemoteCall`.

---

## J. BFF Controller — `AdminCargoDryController`

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminCargoDryController.cs`

5 using statements added, Phase 4D section added with 5 endpoints:

```
GET  /api/v1/admin-panel/cargodry/commercial/settlements/{id}/payout-execution-preview
POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/approve-payout
POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/mark-payout-processing
POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/complete-payout
POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/fail-payout
```

4 Phase 4D body request classes added at the bottom of the file:
`ApproveSettlementPayoutBodyRequest`, `MarkSettlementPayoutProcessingBodyRequest`, `CompleteSettlementPayoutBodyRequest`, `FailSettlementPayoutBodyRequest`

BFF responses wrap `CargoDrySettlementPayoutLifecycleResponseBffDto` via `SetResponse()` consistent with the existing BFF pattern.

---

## K. Cross-Module Boundary Design

```
AdminCargoDryController (BFF)
  └── IAizenCQRSProcessor
        └── XxxBffCommandHandler
              └── IAdminCargoDryBffRemoteCall (Refit)
                    └── CargoDryCommercialController (Module)
                          └── ISender (MediatR)
                                └── XxxCargoDryCommandHandler
                                      ├── ICargoDrySellThroughSettlementRepository   [load settlement]
                                      └── ICargoDrySettlementPayoutLifecycleService   [cross-module to Payment]
                                            └── IPayoutRecordRepository                [load/save PayoutRecord]
```

The `ICargoDrySettlementPayoutLifecycleService` is registered in `Payment.Application` DI and resolved by CargoDry command handlers — the only cross-module in-process bridge in Phase 4D. No MediatR cross-dispatch.

---

## L. Files Changed / Created

**Payment module (6 files):**
- `PayoutStatus.cs` — added `Approved = 7`
- `PayoutRecordEntity.cs` — 7 fields + 4 domain methods
- `PayoutRecordConfiguration.cs` — 7 property mappings
- `ICargoDrySettlementPayoutLifecycleService.cs` — new interface
- `CargoDrySettlementPayoutLifecycleService.cs` — new implementation
- `CargoDryPayoutLifecycleResultDto.cs` — new DTO

**Payment migrations (3 files):**
- `20260703160000_AddPayoutRecordLifecycleFields.cs`
- `20260703160000_AddPayoutRecordLifecycleFields.Designer.cs`
- `PaymentDbContextModelSnapshot.cs` — 7 new properties on `payout_records`

**CargoDry module (5 files):**
- `CargoDrySellThroughSettlementEntity.cs` — 5 fields + 2 domain methods
- `CargoDrySellThroughSettlementEntityConfiguration.cs` — 5 property mappings
- All 4 command handlers + `GetCargoDrySettlementPayoutExecutionPreviewQueryHandler`
- `CargoDryCommercialController.cs` — 5 new endpoints

**CargoDry migrations (3 files):**
- `20260703160001_AddCargoDrySettlementPayoutCompletionFields.cs`
- `20260703160001_AddCargoDrySettlementPayoutCompletionFields.Designer.cs`
- `CargoDryDbContextModelSnapshot.cs` — 5 new properties on `sell_through_settlements`

**BFF (16 files):**
- `CargoDryCommercialBffDtos.cs` — 3 new DTOs + Phase 4D fields on 2 existing DTOs
- `CargoDryRemoteRequests.cs` — 4 new request DTOs
- `IAdminCargoDryBffRemoteCall.cs` — 5 new methods
- 10 BFF Application handler files (5 command/query + 5 handler)
- `AdminCargoDryController.cs` — 5 endpoints + 5 usings + 4 body request classes

**Docs (1 file):**
- `docs/cargodry-payout-lifecycle-phase4d-implementation-report.md` (this file)

---

## M. Local Build Checklist

Run `dotnet build` on all 12 affected projects before applying migrations:

```bash
# Payment
dotnet build Modules/Payment/src/Aizen.Modules.Payment.Domain/
dotnet build Modules/Payment/src/Aizen.Modules.Payment.Abstraction/
dotnet build Modules/Payment/src/Aizen.Modules.Payment.Application/
dotnet build Modules/Payment/src/Aizen.Modules.Payment.Repository/
dotnet build Modules/Payment/src/Aizen.Modules.Payment/

# CargoDry
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/

# BFF
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/
```

Apply migrations:
```bash
# In Aizen.Modules.Payment.Repository project directory
dotnet ef database update --connection "..."

# In Aizen.Modules.CargoDry.Repository project directory
dotnet ef database update --connection "..."
```

---

## N. Settlement Lifecycle — Final State Machine (All Phases)

```
[Created]
  ↓ ResolveMonthlySellThroughSettlement
[ReadyForSettlement]
  ↓ PrepareCargoDrySettlementPayment (Phase 4B)
[Scheduled] + PayoutRecordId set
  ↓ PrepareCargoDrySettlementInvoice (Phase 4C)
[Scheduled] + InvoiceId set
  ↓ ApproveCargoDrySettlementPayout
[Scheduled] + PayoutRecord.Status = Approved
  ↓ MarkCargoDrySettlementPayoutProcessing (optional)
[Scheduled] + PayoutRecord.Status = Processing
  ↓ CompleteCargoDrySettlementPayout ← THE ONLY PATH TO SETTLED
[Settled] + PayoutCompletedAtUtc + PayoutCompletionReference set

  ⚡ fail-payout (from any non-Settled state)
  → Settlement remains [Scheduled]; PayoutRecord.Status = Failed
  → Admin can restart from approve-payout
```
