# CargoDry Phase 4C Implementation Report
## Settlement Invoice Preparation (ProviderSettlementStatement)

**Date:** 2026-07-03  
**Phase:** 4C — Settlement Invoice / Statement Preparation  
**Status:** Complete (Steps 1–7)

---

## A. Scope and Constraints

### What was implemented
- Draft `ProviderSettlementStatement` invoice creation in the Payment module for Scheduled sell-through settlements
- Idempotent invoice preparation tracked via 4 new entity fields on `CargoDrySellThroughSettlementEntity`
- Preview (eligibility check) endpoint before triggering preparation
- Full stack: module layer, BFF layer, EF migration, snapshot

### Hard constraints respected
- ❌ No `PaymentTransaction` created
- ❌ No `CommissionInvoice` or wrong invoice type
- ❌ Settlement status NOT changed to `Settled` (stays `Scheduled`)
- ❌ No payout execution, Iyzico calls, settlement job, refund/credit note, Admin Web pages
- ✅ Document type: `InvoiceType.ProviderSettlementStatement = 8` (Inktavia owes provider)
- ✅ Tax rate = 0 on settlement statements (inter-party)
- ✅ Invoice state tracked via new entity fields (Option B lifecycle)
- ✅ Idempotency by `InvoiceSourceType.CargoDrySettlement + SettlementId`
- ✅ Both module endpoints and BFF endpoints implemented
- ✅ Additive EF migration only

---

## B. New Enum Values

All enum additions are code-only (no migration required for enums stored as integers in EF).

| Enum | Value | Name | Notes |
|------|-------|------|-------|
| `InvoiceType` | 8 | `ProviderSettlementStatement` | New type for settlement payouts |
| `CommercialModel` | 5 | `ConsignmentSettlement` | Settlement commercial model |
| `InvoiceLineType` | 10 | `ProviderSettlementLine` | Settlement line, TaxRate=0 |
| `InvoiceSourceType` | 8 | `CargoDrySettlement` | Already existed from Phase 4B |

**Invoice number prefix:** `PST` (e.g. `PST-20260703-00001`)  
Added to `InvoiceNumberService.PrefixMap`.

---

## C. Payment Module Changes

### `InvoiceNumberService.cs` (MODIFIED)
Added `PST` prefix for `ProviderSettlementStatement` to the prefix dictionary.

### `IInvoiceRepository.cs` (MODIFIED)
Added:
```csharp
Task<InvoiceHeaderEntity?> GetBySourceAsync(
    InvoiceSourceType sourceType, long sourceId, CancellationToken ct = default);
```

### `InvoiceRepository.cs` (MODIFIED)
Implemented `GetBySourceAsync` via `FirstOrDefaultAsync`.

### `CreateCargoDrySettlementStatementCommand.cs` (CREATED)
`Payment.Application/Commands/CreateCargoDrySettlementStatement/`

Fields: `SettlementId`, `SettlementCode`, `ProviderProfileId`, `ProviderPayoutAmount`, `TotalSaleAmount`, `TotalCommissionAmount`, `TotalKitCount`, `CurrencyCode`, `ProductCode`, `PeriodStartUtc`, `PeriodEndUtc`, `PayoutRecordId?`, `PreparedByUserId`, `Notes?`

### `CreateCargoDrySettlementStatementCommandHandler.cs` (CREATED)
- Idempotency: `ExistsForSourceAsync` → `GetBySourceAsync`
- `InvoiceHeaderEntity.CreateDraft()` with:
  - `InvoiceType.ProviderSettlementStatement`
  - `CommercialModel.ConsignmentSettlement`
  - SellerUserId = null (Inktavia is seller), BuyerUserId = providerProfileId
  - SubTotal = TaxableAmount = Total = `ProviderPayoutAmount`
  - TaxAmount = 0m
- `InvoiceLineEntity.Create()` with `ProviderSettlementLine`, Qty=TotalKitCount, UnitCode="KIT", TaxRate=0m

### `CargoDrySettlementInvoiceService.cs` (CREATED)
`Payment.Application/Services/`  
Implements `ICargoDrySettlementInvoiceService` (in `Payment.Abstraction`).  
Dispatches `CreateCargoDrySettlementStatementCommand` via `ISender`.

### `DependencyInjection.cs` (MODIFIED)
```csharp
services.AddScoped<ICargoDrySettlementInvoiceService, CargoDrySettlementInvoiceService>();
```

---

## D. CargoDry Domain Changes

### `CargoDrySellThroughSettlementEntity.cs` (MODIFIED)
4 new Phase 4C fields:
```csharp
public long?     InvoiceId                  { get; private set; }
public DateTime? InvoicePreparedAtUtc        { get; private set; }
public long?     InvoicePreparedByUserId     { get; private set; }
public string?   InvoicePreparationNote      { get; private set; }
```

New domain method:
```csharp
public void MarkInvoicePrepared(long invoiceId, long preparedByUserId, DateTime preparedAtUtc, string? note = null)
```
- Guards: `Status != Scheduled` → throws; `InvoiceId.HasValue` → no-op (idempotency)
- Status remains `Scheduled` after call

---

## E. CargoDry Repository Changes

### `CargoDrySellThroughSettlementEntityConfiguration.cs` (MODIFIED)
- Added EF property mappings for all 4 Phase 4C fields
- Added filtered index `IX_sell_through_settlements_InvoiceId` (`WHERE "InvoiceId" IS NOT NULL`)

---

## F. CargoDry Abstraction Changes

### `CargoDrySellThroughSettlementDto.cs` (MODIFIED)
Added to `CargoDrySellThroughSettlementDto`:
```csharp
public long?     InvoiceId                  { get; init; }
public DateTime? InvoicePreparedAtUtc       { get; init; }
public long?     InvoicePreparedByUserId    { get; init; }
public string?   InvoicePreparationNote     { get; init; }
```

Added to `CargoDrySellThroughSettlementListItemDto`:
```csharp
public long?     InvoiceId               { get; init; }
public DateTime? InvoicePreparedAtUtc    { get; init; }
```

---

## G. CargoDry Application Layer Changes

### `GetCargoDrySellThroughSettlementDetailQueryHandler.cs` (MODIFIED)
Added Phase 4C field mappings to DTO projection.

### `PrepareCargoDrySettlementPaymentCommandHandler.cs` (MODIFIED)
Added null Phase 4C field mappings in `BuildResponse()` (required since DTO now includes them).

### `PrepareCargoDrySettlementInvoiceCommand.cs` (CREATED)
`CargoDry.Application/Commands/PrepareCargoDrySettlementInvoice/`

### `PrepareCargoDrySettlementInvoiceCommandHandler.cs` (CREATED)
Flow:
1. Load settlement; idempotency guard (`InvoiceId.HasValue`)
2. Status guard (`Scheduled` required)
3. Amount guard (`ProviderPayoutAmount > 0`)
4. `ICargoDrySettlementInvoiceService.PrepareSettlementStatementAsync()`
5. `settlement.MarkInvoicePrepared()`
6. `SaveChangesAsync()`
7. Return DTO with Phase 4B + 4C fields mapped

### `GetCargoDrySettlementInvoicePreparationPreviewQuery.cs` (CREATED)
`CargoDry.Application/Queries/GetCargoDrySettlementInvoicePreparationPreview/`

### `GetCargoDrySettlementInvoicePreparationPreviewQueryHandler.cs` (CREATED)
Checks (non-throwing, returns `CanPrepare=false + BlockingReasons`):
- Settlement found
- Status = `Scheduled` (or already prepared)
- `PayoutRecordId` exists (Phase 4B complete)
- `ProviderPayoutAmount > 0`
- Invoice not yet prepared (`InvoiceId.HasValue` → `InvoiceExists=true, CanPrepare=true` for idempotency info)

---

## H. Module Controller Changes

### `CargoDryCommercialController.cs` (FIXED + EXTENDED)
Fixed structural bug from previous session (Phase 4C methods were outside the class).
Added 2 new Phase 4C endpoints inside the controller class:

```
GET  /api/v1/cargodry/admin/commercial/settlements/{id}/invoice-preparation-preview
POST /api/v1/cargodry/admin/commercial/settlements/{id}/prepare-invoice
```

Added `PrepareSettlementInvoiceRequest` inline request model class outside the controller.

---

## I. BFF Layer Changes

### `CargoDryCommercialBffDtos.cs` (MODIFIED)
Added Phase 4C fields to:
- `CargoDrySellThroughSettlementBffDto` (4 fields: `InvoiceId`, `InvoicePreparedAtUtc`, `InvoicePreparedByUserId`, `InvoicePreparationNote`)
- `CargoDrySellThroughSettlementListItemBffDto` (2 fields: `InvoiceId`, `InvoicePreparedAtUtc`)

Added new DTOs:
- `CargoDrySettlementInvoicePreparationPreviewBffDto`
- `PrepareCargoDrySettlementInvoiceBffResponseDto`

### `CargoDryRemoteRequests.cs` (MODIFIED)
Added `PrepareCargoDrySettlementInvoiceBffRequest`.

### `IAdminCargoDryBffRemoteCall.cs` (MODIFIED)
Added 2 Phase 4C methods:
```csharp
[AizenRemoteCallGet(".../invoice-preparation-preview")]
Task<CargoDrySettlementInvoicePreparationPreviewBffDto> GetSettlementInvoicePreparationPreviewAsync(
    long id, CancellationToken ct = default);

[AizenRemoteCallPost(".../prepare-invoice")]
Task<PrepareCargoDrySettlementInvoiceBffResponseDto> PrepareSettlementInvoiceAsync(
    long id, [AizenRemoteCallBody] PrepareCargoDrySettlementInvoiceBffRequest request,
    CancellationToken ct = default);
```

### `GetCargoDrySettlementInvoicePreparationPreviewBffQuery.cs` + Handler (CREATED)
`AdminCargoDry/Query/GetCargoDrySettlementInvoicePreparationPreview/`  
Proxies to `_remote.GetSettlementInvoicePreparationPreviewAsync()`.

### `PrepareCargoDrySettlementInvoiceBffCommand.cs` + Handler (CREATED)
`AdminCargoDry/Command/PrepareCargoDrySettlementInvoice/`  
Proxies to `_remote.PrepareSettlementInvoiceAsync()`.

### `AdminCargoDryController.cs` (MODIFIED)
Added 2 Phase 4C endpoints:
```
GET  /api/v1/admin-panel/cargodry/commercial/settlements/{id}/invoice-preparation-preview
POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/prepare-invoice
```
Added `PrepareSettlementInvoiceBodyRequest` inline class.

---

## J. EF Migration

### Migration: `20260703150000_AddCargoDrySettlementInvoicePreparationFields.cs`
Schema: `cargodry`, table: `sell_through_settlements`

**Up:**
- `AddColumn` × 4: `InvoiceId` (bigint), `InvoicePreparedAtUtc` (timestamptz), `InvoicePreparedByUserId` (bigint), `InvoicePreparationNote` (varchar 1000)
- `CreateIndex`: `IX_sell_through_settlements_InvoiceId` (filtered: `WHERE "InvoiceId" IS NOT NULL`)

**Down:** reverses all 4 columns + index.

### `CargoDryDbContextModelSnapshot.cs` (MODIFIED)
- Added 4 Phase 4C property entries in the `CargoDrySellThroughSettlementEntity` block
- Added `HasIndex("InvoiceId").HasDatabaseName(...).HasFilter(...)` entry

---

## K. Settlement Lifecycle (Option B)

```
Pending → ReadyForSettlement → Scheduled → [Settled] (future Phase 4D)
                                   ↑
              Phase 4B: create PayoutRecord (status: Scheduled)
              Phase 4C: create Draft Invoice (status: still Scheduled)
                        InvoiceId, InvoicePreparedAtUtc, InvoicePreparedByUserId set
```

Status `Scheduled` at all times during Phase 4C. No new status enum value was added.

---

## L. Idempotency Design

### Invoice-level (Payment module)
`IInvoiceRepository.ExistsForSourceAsync(CargoDrySettlement, settlementId)` — if exists, load via `GetBySourceAsync()` and return early.

### Settlement-level (CargoDry module)
`settlement.InvoiceId.HasValue` guard in `PrepareCargoDrySettlementInvoiceCommandHandler`.  
`MarkInvoicePrepared()` also no-ops if `InvoiceId` is already set.

Both layers guard independently → safe to retry from any failure point.

---

## M. Files Modified / Created Summary

| File | Action |
|------|--------|
| `Payment.Application/Services/InvoiceNumberService.cs` | Modified — PST prefix |
| `Payment.Domain/Interface/Repository/IInvoiceRepository.cs` | Modified — `GetBySourceAsync` |
| `Payment.Repository/Repositories/InvoiceRepository.cs` | Modified — `GetBySourceAsync` impl |
| `Payment.Application/Commands/CreateCargoDrySettlementStatement/...Command.cs` | Created |
| `Payment.Application/Commands/CreateCargoDrySettlementStatement/...CommandHandler.cs` | Created |
| `Payment.Application/Services/CargoDrySettlementInvoiceService.cs` | Created |
| `Payment.Application/DependencyInjection.cs` | Modified — DI registration |
| `CargoDry.Domain/Entities/CargoDrySellThroughSettlementEntity.cs` | Modified — 4 fields + method |
| `CargoDry.Repository/.../CargoDrySellThroughSettlementEntityConfiguration.cs` | Modified — EF config |
| `CargoDry.Abstraction/Dto/CargoDrySellThroughSettlementDto.cs` | Modified — Phase 4C fields |
| `CargoDry.Application/Queries/GetCargoDrySellThroughSettlementDetail/...Handler.cs` | Modified — mapping |
| `CargoDry.Application/Commands/PrepareCargoDrySettlementPayment/...Handler.cs` | Modified — null fields |
| `CargoDry.Application/Commands/PrepareCargoDrySettlementInvoice/...Command.cs` | Created |
| `CargoDry.Application/Commands/PrepareCargoDrySettlementInvoice/...CommandHandler.cs` | Created |
| `CargoDry.Application/Queries/GetCargoDrySettlementInvoicePreparationPreview/...Query.cs` | Created |
| `CargoDry.Application/Queries/GetCargoDrySettlementInvoicePreparationPreview/...Handler.cs` | Created |
| `CargoDry/Controllers/CargoDryCommercialController.cs` | Fixed + Modified |
| `Bff.AdminPanel.Application/AdminCargoDry/Dto/CargoDryCommercialBffDtos.cs` | Modified |
| `Bff.AdminPanel.Application/Common/RemoteClients/CargoDry/CargoDryRemoteRequests.cs` | Modified |
| `Bff.AdminPanel.Application/Common/RemoteClients/IAdminCargoDryBffRemoteCall.cs` | Modified |
| `Bff.AdminPanel.Application/AdminCargoDry/Query/GetCargoDrySettlementInvoicePreparationPreview/...Query.cs` | Created |
| `Bff.AdminPanel.Application/AdminCargoDry/Query/GetCargoDrySettlementInvoicePreparationPreview/...Handler.cs` | Created |
| `Bff.AdminPanel.Application/AdminCargoDry/Command/PrepareCargoDrySettlementInvoice/...Command.cs` | Created |
| `Bff.AdminPanel.Application/AdminCargoDry/Command/PrepareCargoDrySettlementInvoice/...Handler.cs` | Created |
| `Bff.AdminPanel/Controllers/V1/AdminCargoDryController.cs` | Modified |
| `CargoDry.Repository/Migrations/20260703150000_AddCargoDrySettlementInvoicePreparationFields.cs` | Created |
| `CargoDry.Repository/Migrations/CargoDryDbContextModelSnapshot.cs` | Modified |

Total: **27 files** touched across Payment, CargoDry, and BFF layers.
