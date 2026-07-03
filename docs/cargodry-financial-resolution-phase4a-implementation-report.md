# CargoDry Phase 4A — Financial Resolution & Monthly Settlement Finalization
## Implementation Report

**Date:** 2026-07-03  
**Phase:** 4A  
**Status:** ✅ Complete — pending local `dotnet build` verification

---

## A. Scope

Phase 4A introduces financial resolution to the CargoDry sell-through commercial flow:

1. **Per-attribution financial resolution** — Admin resolves `SalePrice`, `CommissionRate`, `ProviderShareAmount`, and `PlatformShareAmount` on each `CargoDrySalesAttributionEntity` record.
2. **Monthly settlement finalization** — Admin finalizes a `CargoDrySellThroughSettlementEntity` by verifying all attributions are resolved, recalculating totals, and marking the settlement `ReadyForSettlement`.
3. **CurrencyCode guard** — Replaced the silent `?? "USD"` fallback with a hard business error.

---

## B. Files Changed / Created

### B.1 Application Service — CurrencyCode Guard (Task #447)

**`Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/Services/CargoDryCommercialActivationService.cs`**

- Added `using Aizen.Core.Infrastructure.Exception;`
- Replaced `agreement.CurrencyCode ?? "USD"` with:
```csharp
if (string.IsNullOrWhiteSpace(agreement.CurrencyCode))
    throw new AizenBusinessException(
        $"Consignment agreement {agreement.Id} has no CurrencyCode configured. ...");
var currencyCode = agreement.CurrencyCode!;
```

### B.2 Domain Entities (Task #448)

**`Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/Entities/CargoDrySalesAttributionEntity.cs`**

New fields added:
| Field | Type | Notes |
|---|---|---|
| `ProviderShareAmount` | `decimal?` | = `SalePrice × CommissionRate` |
| `PlatformShareAmount` | `decimal?` | = `SalePrice − ProviderShareAmount` |
| `FinancialResolvedAtUtc` | `DateTime?` | Set on resolution |
| `FinancialResolvedByUserId` | `long?` | Admin actor |
| `ResolutionNote` | `string?` | MaxLength 1000 |
| `IsFinanciallyResolved` | `bool` (computed) | `=> FinancialResolvedAtUtc.HasValue` |

New domain method `ResolveFinancials(salePrice, commissionRate, currencyCode, resolvedAtUtc, resolvedByUserId, resolutionNote?)`:
- Guards against re-resolving Settled/Cancelled attributions
- Validates `salePrice >= 0` and `commissionRate ∈ [0, 1]`
- Calculates `providerShare = round(salePrice × commissionRate, 4)`
- Sets all financial fields atomically

**`Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/Entities/CargoDrySellThroughSettlementEntity.cs`**

New field: `ReadyForSettlementAtUtc DateTime?`

New method `RecalculateTotals(totalKitCount, totalSaleAmount, totalProviderShareAmount)` — updates totals from resolved attribution data.

Updated `MarkReadyForSettlement(readyAtUtc, note?)` — now sets `ReadyForSettlementAtUtc` and accepts optional note.

### B.3 EF Configuration + Migration (Task #449)

**`CargoDrySalesAttributionEntityConfiguration.cs`**
```csharp
builder.Property(x => x.ProviderShareAmount).HasColumnType("numeric(18,4)");
builder.Property(x => x.PlatformShareAmount).HasColumnType("numeric(18,4)");
builder.Property(x => x.FinancialResolvedAtUtc);
builder.Property(x => x.FinancialResolvedByUserId);
builder.Property(x => x.ResolutionNote).HasMaxLength(1000);
builder.Ignore(x => x.IsFinanciallyResolved);  // computed — no DB column
```

**`CargoDrySellThroughSettlementEntityConfiguration.cs`**
```csharp
builder.Property(x => x.ReadyForSettlementAtUtc);
```

**Migration:** `20260703150000_AddCargoDryFinancialResolutionFields.cs`

`Up()`:
- Adds 5 columns to `cargodry.sales_attributions`
- Adds `ReadyForSettlementAtUtc` to `cargodry.sell_through_settlements`
- Creates index `IX_sales_attributions_FinancialResolvedAtUtc` (for unresolved filter queries)

`Down()`: Drops all added columns and the index.

### B.4 Abstraction — DTOs + Repository Interface (Task #450)

**`CargoDrySalesAttributionDto.cs`**
- Detail DTO: `+ProviderShareAmount`, `+PlatformShareAmount`, `+IsFinanciallyResolved`, `+FinancialResolvedAtUtc`, `+FinancialResolvedByUserId`, `+ResolutionNote`
- List item DTO: `+ProviderShareAmount`, `+IsFinanciallyResolved`

**`CargoDrySellThroughSettlementDto.cs`**
- Both `CargoDrySellThroughSettlementDto` and `CargoDrySellThroughSettlementListItemDto`: `+ReadyForSettlementAtUtc`

**`ICargoDrySalesAttributionRepository.cs`**
```csharp
Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetBySettlementIdAsync(long settlementId, CancellationToken ct);
Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetUnresolvedBySettlementIdAsync(long settlementId, CancellationToken ct);
```

### B.5 Repository Implementations (Task #451)

**`CargoDrySalesAttributionRepository.cs`**
- `GetBySettlementIdAsync` — filters `SellThroughSettlementId == settlementId`
- `GetUnresolvedBySettlementIdAsync` — same + `FinancialResolvedAtUtc == null`

### B.6 Query Handler Mapping Updates (Task #451)

All 4 affected query handlers updated to map new fields:
- `GetCargoDrySalesAttributionDetailQueryHandler` — maps 6 new attribution fields
- `GetCargoDrySalesAttributionsPagedQueryHandler` — maps `ProviderShareAmount`, `IsFinanciallyResolved`
- `GetCargoDrySellThroughSettlementDetailQueryHandler` — maps `ReadyForSettlementAtUtc`
- `GetCargoDrySellThroughSettlementsPagedQueryHandler` — maps `ReadyForSettlementAtUtc`

### B.7 Command 1 — ResolveCargoDrySalesAttributionFinancials (Task #452)

**Directory:** `Application/Commands/ResolveCargoDrySalesAttributionFinancials/`

**Command:**
```csharp
SalesAttributionId, SalePrice, CurrencyCode, CommissionRateOverride?, ResolvedByUserId, ResolutionNote?
```

**Validator:** SalesAttributionId > 0 | SalePrice > 0 | CurrencyCode required len=3 | CommissionRateOverride 0–1 if present | ResolvedByUserId > 0 | ResolutionNote maxLen 1000

**Handler logic:**
1. Load attribution by Id — throw if not found
2. Reject if status is Settled or Cancelled
3. Rate cascade: `CommissionRateOverride → agreement.ConsignmentRate → product.ProviderCommissionRate → 0`
4. Call `attribution.ResolveFinancials(...)`
5. If `SellThroughSettlementId` exists: load all settlement attributions → call `settlement.RecalculateTotals(...)` 
6. `SaveChangesAsync`
7. Map to `CargoDrySalesAttributionDto` and return

### B.8 Command 2 — ResolveMonthlySellThroughSettlement (Task #453)

**Directory:** `Application/Commands/ResolveMonthlySellThroughSettlement/`

**Command:** `SettlementId, ResolvedByUserId, ResolutionNote?`

**Validator:** SettlementId > 0 | ResolvedByUserId > 0 | ResolutionNote maxLen 1000

**Handler logic:**
1. Load settlement — throw if not found
2. Reject if status is not `Pending`
3. `GetUnresolvedBySettlementIdAsync` — throw business error if count > 0 (lists unresolved Ids)
4. `GetBySettlementIdAsync` — load all attributions
5. Sum `SalePrice` and `ProviderShareAmount` from resolved attributions
6. `settlement.RecalculateTotals(count, totalSale, totalProviderShare)`
7. `settlement.MarkReadyForSettlement(nowUtc, resolutionNote)`
8. `SaveChangesAsync`
9. Map to `CargoDrySellThroughSettlementDto` and return

### B.9 Module Controller (Task #454)

**`CargoDryCommercialController.cs`** — 2 new POST endpoints:

```
POST /api/v1/cargodry/admin/commercial/sales-attributions/{id}/resolve-financials
POST /api/v1/cargodry/admin/commercial/settlements/{id}/resolve-monthly
```

Inline request models: `ResolveAttributionFinancialsRequest`, `ResolveMonthlySettlementRequest`

### B.10 BFF Layer (Task #455)

**`CargoDryRemoteRequests.cs`** — 2 new request DTOs added:
- `ResolveAttributionFinancialsBffRequest` (SalePrice, CurrencyCode, CommissionRateOverride?, ResolvedByUserId, ResolutionNote?)
- `ResolveMonthlySettlementBffRequest` (ResolvedByUserId, ResolutionNote?)

**`CargoDryCommercialBffDtos.cs`** — Phase 4A fields added to:
- `CargoDrySalesAttributionBffDto`: 6 new fields
- `CargoDrySalesAttributionListItemBffDto`: `ProviderShareAmount`, `IsFinanciallyResolved`
- `CargoDrySellThroughSettlementBffDto`: `ReadyForSettlementAtUtc`
- `CargoDrySellThroughSettlementListItemBffDto`: `ReadyForSettlementAtUtc`

**`IAdminCargoDryBffRemoteCall.cs`** — 2 new remote call methods:
```csharp
Task<CargoDrySalesAttributionBffDto> ResolveAttributionFinancialsAsync(long id, [AizenRemoteCallBody] ..., CancellationToken ct = default);
Task<CargoDrySellThroughSettlementBffDto> ResolveMonthlySettlementAsync(long id, [AizenRemoteCallBody] ..., CancellationToken ct = default);
```

**New BFF command folders:**
- `Command/ResolveCargoDrySalesAttributionFinancials/` — Command + Handler
- `Command/ResolveMonthlySellThroughSettlement/` — Command + Handler

**`AdminCargoDryController.cs`** (BFF host) — 2 new POST endpoints:
```
POST /api/v1/admin-panel/cargodry/commercial/sales-attributions/{id}/resolve-financials
POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/resolve-monthly
```

---

## C. Financial Model

```
CommissionRate   = provider's share rate (from agreement or product, 0.00–1.00)
ProviderShare    = round(SalePrice × CommissionRate, 4, AwayFromZero)
PlatformShare    = round(SalePrice − ProviderShare, 4, AwayFromZero)
CommissionAmount = ProviderShare   (stored for historical compatibility)
```

Rate cascade (per attribution):
1. `CommissionRateOverride` (command input, if present)
2. `ConsignmentAgreement.ConsignmentRate` (if attribution has ConsignmentAgreementId)
3. `CargoDryProduct.ProviderCommissionRate` (product-level fallback)
4. `0` (DirectSale — platform takes nothing)

---

## D. Business Rules Enforced

| Rule | Enforcement |
|---|---|
| CurrencyCode must not be null on agreement | `AizenBusinessException` in `CargoDryCommercialActivationService` |
| Cannot re-resolve Settled/Cancelled attribution | `InvalidOperationException` in `ResolveFinancials()` domain method |
| CommissionRate must be 0–1 | Validator + domain method guard |
| SalePrice must be positive | Validator + domain method guard |
| All attributions must be resolved before settlement finalization | `GetUnresolvedBySettlementIdAsync` check in `ResolveMonthlySellThroughSettlement` handler |
| Settlement must be in Pending status to finalize | Status check before `MarkReadyForSettlement` |

---

## E. API Endpoints Summary

### Module Layer (`Aizen.Modules.CargoDry`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/cargodry/admin/commercial/sales-attributions/{id}/resolve-financials` | Admin/SuperAdmin | Resolve financial amounts on an attribution |
| POST | `/api/v1/cargodry/admin/commercial/settlements/{id}/resolve-monthly` | Admin/SuperAdmin | Finalize monthly settlement |

### BFF Layer (`Aizen.Bff.AdminPanel`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/admin-panel/cargodry/commercial/sales-attributions/{id}/resolve-financials` | AdminPanelAccess | BFF proxy for attribution resolution |
| POST | `/api/v1/admin-panel/cargodry/commercial/settlements/{id}/resolve-monthly` | AdminPanelAccess | BFF proxy for settlement finalization |

---

## F. Migration

**Name:** `20260703150000_AddCargoDryFinancialResolutionFields`

Columns added to `cargodry.sales_attributions`:
- `ProviderShareAmount numeric(18,4)`
- `PlatformShareAmount numeric(18,4)`
- `FinancialResolvedAtUtc timestamp with time zone`
- `FinancialResolvedByUserId bigint`
- `ResolutionNote varchar(1000)`

Column added to `cargodry.sell_through_settlements`:
- `ReadyForSettlementAtUtc timestamp with time zone`

Index:
- `IX_sales_attributions_FinancialResolvedAtUtc` on `cargodry.sales_attributions(FinancialResolvedAtUtc)`

---

## G. Build Commands (Run Locally)

```bash
# Module projects
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/

# BFF projects
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/

# Migration (apply after build)
dotnet ef database update --project Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/
```

---

## H. Hard Restrictions (Enforced — Not Implemented)

The following items are **explicitly outside Phase 4A scope** and must not be implemented until owner approval:

- ❌ `PaymentTransaction` creation
- ❌ `Invoice` creation
- ❌ `ProviderPayout` record creation
- ❌ Iyzico gateway integration
- ❌ Settlement processing scheduled job
- ❌ Credit note / refund logic
- ❌ Admin Web frontend pages for Phase 4A
- ❌ Phase 4B (payment execution, payout disbursement)

---

## I. What Comes Next (Phase 4B — Owner Approval Required)

Phase 4B would implement:
- Trigger actual payment initiation from `ReadyForSettlement` → `Scheduled`
- Create `ProviderPayout` records against Iyzico Marketplace sub-merchant
- Manage payout status lifecycle: `Scheduled → Settled / Failed`
- Generate PDF settlement invoices for provider records

**Phase 4B must not be started without explicit owner approval.**
