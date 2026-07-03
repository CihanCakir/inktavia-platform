# CargoDry Phase 3 — Sales Attribution & Sell-Through Settlement Foundation
## Implementation Report

**Date:** 2026-07-03
**Phase:** 3 of N
**Status:** ✅ Complete — Pending Owner Approval for Phase 4

---

## A. Files Changed

### Created (new files)

| Layer | File | Description |
|---|---|---|
| Abstraction/Enum | `CargoDrySalesAttributionStatus.cs` | 6 statuses: Pending, Attributed, SettlementPending, Settled, Cancelled, CommercialReviewRequired |
| Abstraction/Enum | `CargoDrySellThroughSettlementStatus.cs` | 6 statuses: Pending, ReadyForSettlement, Scheduled, Settled, Cancelled, Disputed |
| Abstraction/Dto | `CargoDrySalesAttributionDto.cs` | Full DTO + ListItemDto + PagedResultDto |
| Abstraction/Dto | `CargoDrySellThroughSettlementDto.cs` | Full DTO + ListItemDto + PagedResultDto |
| Abstraction/Interface | `ICargoDryCommercialActivationService.cs` | Service interface with `ResolveAsync(long kitId, long activatedByUserId, CancellationToken ct)` |
| Domain/Entities | `CargoDrySalesAttributionEntity.cs` | 22-field entity with factory `Create()` and domain methods |
| Domain/Entities | `CargoDrySellThroughSettlementEntity.cs` | 20-field entity with factory `Create()` and domain methods |
| Domain/Interface/Repository | `ICargoDrySalesAttributionRepository.cs` | 4 methods including `GetByKitIdAsync` (idempotency) |
| Domain/Interface/Repository | `ICargoDrySellThroughSettlementRepository.cs` | 5 methods including `GetOpenForAgreementProductAsync` |
| Repository/Configurations | `CargoDrySalesAttributionEntityConfiguration.cs` | EF config: `sales_attributions` table, unique KitId index, 9 query indexes |
| Repository/Configurations | `CargoDrySellThroughSettlementEntityConfiguration.cs` | EF config: `sell_through_settlements` table, unique SettlementCode, composite + 6 query indexes |
| Repository/Repositories | `CargoDrySalesAttributionRepository.cs` | Full paged query implementation |
| Repository/Repositories | `CargoDrySellThroughSettlementRepository.cs` | Full paged query implementation |
| Application/Services | `CargoDryCommercialActivationService.cs` | Commercial path resolver — all 4 SalesChannel cases handled |
| Application/Queries | `GetCargoDrySalesAttributionsPagedQuery.cs` + Handler | Paged list |
| Application/Queries | `GetCargoDrySalesAttributionDetailQuery.cs` + Handler | Detail by Id |
| Application/Queries | `GetCargoDrySellThroughSettlementsPagedQuery.cs` + Handler | Paged list |
| Application/Queries | `GetCargoDrySellThroughSettlementDetailQuery.cs` + Handler | Detail by Id |
| Module/Controllers | `CargoDryCommercialController.cs` | 4 GET endpoints under `/api/v1/cargodry/admin/commercial` |
| Bff/Dto | `CargoDryCommercialBffDtos.cs` | 6 BFF DTO classes (2 paged, 2 list item, 2 full detail) |
| Bff/Query | `GetCargoDrySalesAttributionsPagedBffQuery.cs` + Handler | BFF paged query |
| Bff/Query | `GetCargoDrySalesAttributionDetailBffQuery.cs` + Handler | BFF detail query |
| Bff/Query | `GetCargoDrySellThroughSettlementsPagedBffQuery.cs` + Handler | BFF paged query |
| Bff/Query | `GetCargoDrySellThroughSettlementDetailBffQuery.cs` + Handler | BFF detail query |

### Modified (existing files)

| File | Change |
|---|---|
| `CargoDryDbContext.cs` | Added `DbSet<CargoDrySalesAttributionEntity>` and `DbSet<CargoDrySellThroughSettlementEntity>` |
| `Repository/DependencyInjection.cs` | Registered both repository implementations |
| `Application/DependencyInjection.cs` | Registered `ICargoDryCommercialActivationService` → `CargoDryCommercialActivationService` |
| `ActivateKitCommandHandler.cs` | Injected `ICargoDryCommercialActivationService`, called `ResolveAsync()` after `kit.Activate()`, before `SaveChangesAsync()` |
| `IAdminCargoDryBffRemoteCall.cs` | Added 4 remote call methods pointing to `/api/v1/cargodry/admin/commercial/sales-attributions[/{id}]` and `/settlements[/{id}]` |
| `AdminCargoDryController.cs` (BFF) | Added 4 GET commercial endpoints under `commercial/sales-attributions` and `commercial/settlements` |
| `CargoDryCommercialController.cs` (Module) | Renamed routes from `attributions` to `sales-attributions` per audit spec |

---

## B. Abstraction Layer

### Enums

**`CargoDrySalesAttributionStatus`**
```
Pending = 1, Attributed = 2, SettlementPending = 3,
Settled = 4, Cancelled = 5, CommercialReviewRequired = 6
```

**`CargoDrySellThroughSettlementStatus`**
```
Pending = 1, ReadyForSettlement = 2, Scheduled = 3,
Settled = 4, Cancelled = 5, Disputed = 6
```

### DTOs

`CargoDrySalesAttributionDto` — 26 properties (full detail including review fields).
`CargoDrySalesAttributionListItemDto` — 20 properties (list display without admin-only review fields).
`CargoDrySalesAttributionPagedResultDto` — Items, Total, Page, PageSize.

`CargoDrySellThroughSettlementDto` — 21 properties (full detail including dispute/note).
`CargoDrySellThroughSettlementListItemDto` — 16 properties.
`CargoDrySellThroughSettlementPagedResultDto` — Items, Total, Page, PageSize.

---

## C. Domain Entities

### `CargoDrySalesAttributionEntity` (22 fields)

Key design decisions:
- One attribution per activated kit — enforced by unique DB index on `KitId`.
- `SalePrice`, `CommissionRate`, `CommissionAmount` are nullable — Phase 3 does not have payment data; they are populated in Phase 4+ when the payment transaction is resolved.
- `CurrencyCode` sourced from `CargoDryConsignmentAgreementEntity.CurrencyCode` for consignment path.
- `ConsignmentRate` is stored as the commission rate reference from the agreement.
- `SellThroughSettlementId` set to null at creation; linked via `LinkToSettlement()` domain method once a settlement is created in the same EF Unit of Work.

Domain methods: `LinkToSettlement()`, `MarkSettled()`, `ResolveManually()`, `Cancel()`.

### `CargoDrySellThroughSettlementEntity` (20 fields)

Key design decisions:
- `SettlementCode` is human-readable: `STS-{agreementId}-{productCode}-{yyyyMM}`.
- `TotalKitCount`, `TotalSaleAmount`, `TotalCommissionAmount`, `ProviderPayoutAmount` accumulate per `AddAttribution()` call.
- In Phase 3, all financial amounts are `0m` placeholders — no real payment data resolved yet.
- `Status` starts as `Pending`. Settlement lifecycle: `Pending → ReadyForSettlement → Scheduled → Settled`.
- Dispute path: any non-Settled status can transition to `Disputed`.
- `ProviderPayoutAmount = TotalSaleAmount - TotalCommissionAmount` — computed on each `AddAttribution()`.

Domain methods: `AddAttribution()`, `MarkReadyForSettlement()`, `Schedule()`, `MarkSettled()`, `RaiseDispute()`, `Cancel()`.

---

## D. Repository Layer

### `ICargoDrySalesAttributionRepository`
- `GetByIdAsync(long id, ct)` — detail lookup
- `GetByKitIdAsync(long kitId, ct)` — **idempotency guard** — prevents duplicate attribution on retry
- `GetPagedAsync(...)` — 10 filter parameters, returns `(Items, Total)`
- `AddAsync(entity, ct)` — stage entity (no SaveChanges)
- `SaveChangesAsync(ct)` — flush

### `ICargoDrySellThroughSettlementRepository`
- `GetByIdAsync(long id, ct)` — detail lookup
- `GetByCodeAsync(string code, ct)` — by settlement code
- `GetOpenForAgreementProductAsync(long agreementId, string productCode, ct)` — finds existing `Pending` settlement for the same agreement+product (prevents duplicate settlement per period)
- `GetPagedAsync(...)` — 8 filter parameters
- `AddAsync(entity, ct)` + `SaveChangesAsync(ct)`

Both repositories use `AsNoTracking().AsQueryable()` for reads, full EF tracking for adds.

DbContext: both `DbSet<>` properties added, picked up by `ApplyConfigurationsFromAssembly`.

DI: both `AddScoped<IInterface, Impl>()` registered in `Repository/DependencyInjection.cs`.

---

## E. Commercial Activation Service

`CargoDryCommercialActivationService.ResolveAsync()` is the Phase 3 commercial core.

### Decision matrix

| `kit.SalesChannel` | Action |
|---|---|
| `null` | Attribution(`CommercialReviewRequired`) — no settlement, no financials |
| `DirectSale` | Attribution(`Attributed`) — no provider, no settlement |
| `ProviderAttributedSale` | Attribution(`Attributed`) + inventory `IncrementActivated(1)` + `InventoryMovement(KitActivated, -1)` |
| `ConsignmentSellThrough` | Attribution(`SettlementPending`) + find/create `SellThroughSettlement(Pending)` + inventory movement + settlement `AddAttribution()` |

### Important constraints respected
- Uses `agreement.ConsignmentRate` — **not** `CommissionRate` (does not exist on the agreement entity).
- Does **not** use `SalePricePerKit` (does not exist on the agreement entity).
- Phase 3: `SalePrice` and `CommissionAmount` set to `null` explicitly.
- Settlement `AddAttribution()` receives `0m` as placeholder amounts.
- **No PaymentTransaction, Invoice, or ProviderPayout created.**

### Idempotency
`GetByKitIdAsync(kitId)` is called before any work. If an attribution already exists, `ResolveAsync()` returns immediately. Safe to re-enter on retry.

### Transaction safety
`ResolveAsync()` does **not** call `SaveChangesAsync()`. All `AddAsync()` calls stage entities in EF's change tracker. The single `SaveChangesAsync()` in `ActivateKitCommandHandler` flushes all changes atomically — the kit activation and commercial attribution in one DB transaction.

---

## F. ActivateKitCommandHandler Integration

```csharp
kit.Activate(request.UserId, request.VesselId, product.ValidityDays);

// Phase 3: resolve commercial attribution (stages entities, does not SaveChanges)
await _commercialActivation.ResolveAsync(kit.Id, request.UserId, ct);

await _kits.SaveChangesAsync(ct);
```

- `_commercialActivation` injected via constructor.
- Called **after** `kit.Activate()` (so `kit.Id` is set) and **before** `SaveChangesAsync()`.
- Existing activation logic (cache invalidation, activation log, message publish) is unchanged.
- If commercial resolution fails, the entire Unit of Work rolls back — no partial state.

---

## G. Queries Implemented

### Module layer (4 CQRS pairs)

| Query | Handler | Description |
|---|---|---|
| `GetCargoDrySalesAttributionsPagedQuery` | `GetCargoDrySalesAttributionsPagedQueryHandler` | 10 filters, returns `PagedResult` |
| `GetCargoDrySalesAttributionDetailQuery` | `GetCargoDrySalesAttributionDetailQueryHandler` | Full `CargoDrySalesAttributionDto` by Id |
| `GetCargoDrySellThroughSettlementsPagedQuery` | `GetCargoDrySellThroughSettlementsPagedQueryHandler` | 8 filters, returns `PagedResult` |
| `GetCargoDrySellThroughSettlementDetailQuery` | `GetCargoDrySellThroughSettlementDetailQueryHandler` | Full `CargoDrySellThroughSettlementDto` by Id |

### BFF layer (4 query pairs)

| BFF Query | Description |
|---|---|
| `GetCargoDrySalesAttributionsPagedBffQuery` + Handler | Forwards all filters to module via `IAdminCargoDryBffRemoteCall` |
| `GetCargoDrySalesAttributionDetailBffQuery` + Handler | Forwards Id to module detail endpoint |
| `GetCargoDrySellThroughSettlementsPagedBffQuery` + Handler | Forwards all filters to module |
| `GetCargoDrySellThroughSettlementDetailBffQuery` + Handler | Forwards Id to module detail endpoint |

---

## H. Module Endpoints

Controller: `CargoDryCommercialController`  
Base route: `/api/v1/cargodry/admin/commercial`  
Auth: `[Authorize(Roles = "Admin,SuperAdmin")]`

| Method | Route | Query | Response |
|---|---|---|---|
| `GET` | `/sales-attributions` | 10 filters + page/pageSize | `CargoDrySalesAttributionPagedResultDto` |
| `GET` | `/sales-attributions/{id}` | — | `CargoDrySalesAttributionDto` or 404 |
| `GET` | `/settlements` | 8 filters + page/pageSize | `CargoDrySellThroughSettlementPagedResultDto` |
| `GET` | `/settlements/{id}` | — | `CargoDrySellThroughSettlementDto` or 404 |

---

## I. BFF Endpoints

Controller: `AdminCargoDryController`  
Base route: `/api/v1/admin-panel/cargodry`  
Auth: `[Authorize(Policy = "AdminPanelAccess")]`

| Method | Route | Description |
|---|---|---|
| `GET` | `/commercial/sales-attributions` | Paged sales attributions |
| `GET` | `/commercial/sales-attributions/{id}` | Sales attribution detail |
| `GET` | `/commercial/settlements` | Paged sell-through settlements |
| `GET` | `/commercial/settlements/{id}` | Settlement detail |

Remote calls (in `IAdminCargoDryBffRemoteCall`):
- `GetSalesAttributionsPagedAsync(...)` → `GET /api/v1/cargodry/admin/commercial/sales-attributions`
- `GetSalesAttributionDetailAsync(id, ct)` → `GET /api/v1/cargodry/admin/commercial/sales-attributions/{id}`
- `GetSellThroughSettlementsPagedAsync(...)` → `GET /api/v1/cargodry/admin/commercial/settlements`
- `GetSellThroughSettlementDetailAsync(id, ct)` → `GET /api/v1/cargodry/admin/commercial/settlements/{id}`

Auth headers forwarded automatically through `AdminPanelBffAuthDelegatingHandler`.

---

## J. Migration

**Migration name:** `AddCargoDryProviderInventoryAndMovementsRelative` (timestamp: `20260703121931`)

Note: Both Phase 2 (Provider Inventory) and Phase 3 (Sales Attribution + Sell-Through Settlement) tables were combined into this single migration because they were authored in the same session.

Tables created:
- `cargodry.sales_attributions` — all 22 entity columns + AizenEntityWithAudit base columns + `ConsignmentAgreementId` added to `kits` table
- `cargodry.sell_through_settlements` — all 20 entity columns + AizenEntityWithAudit base columns

Indexes on `sales_attributions`:
- `IX_sales_attributions_KitId` — **UNIQUE** (one attribution per kit)
- `IX_sales_attributions_ProviderProfileId`
- `IX_sales_attributions_ProductCode`
- `IX_sales_attributions_BatchCode`
- `IX_sales_attributions_SalesChannel`
- `IX_sales_attributions_CommercialModel`
- `IX_sales_attributions_Status`
- `IX_sales_attributions_SellThroughSettlementId`
- `IX_sales_attributions_ConsignmentAgreementId`
- `IX_sales_attributions_CreatedAtUtc`

Indexes on `sell_through_settlements`:
- `IX_sell_through_settlements_SettlementCode` — **UNIQUE**
- `IX_sell_through_settlements_ConsignmentAgreementId`
- `IX_sell_through_settlements_ProviderProfileId`
- `IX_sell_through_settlements_ProductCode`
- `IX_sell_through_settlements_Status`
- `IX_sell_through_settlements_PeriodStartUtc`
- `IX_sell_through_settlements_PeriodEndUtc`
- `IX_sell_through_settlements_ConsignmentAgreementId_ProductCode_Status` — **COMPOSITE** (used by `GetOpenForAgreementProductAsync`)

---

## K. Build Result

**dotnet SDK not available in the sandbox environment.** Local build required.

To verify:
```bash
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/Aizen.Modules.CargoDry.csproj
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Aizen.Modules.CargoDry.Repository.csproj
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
```

**Known compile risk:** BFF `AdminCargoDryController` commercial endpoints use `result?.PagedResult` and `result?.Detail` via `SetResponse()`. If the existing BFF pattern for nullable-detail returns differs from other endpoints, minor adjustment may be needed. All other usings, namespaces, and type references are verified by inspection.

---

## L. Idempotency and Safety Rules

| Rule | Implementation |
|---|---|
| One attribution per kit | Unique DB index on `KitId`; `GetByKitIdAsync` guard in `ResolveAsync()` |
| One open settlement per Provider+Currency+Product+Month | `GetOpenForProviderCurrencyProductPeriodAsync` finds existing `Pending` settlement; creates new only if none exists (Phase 3.2) |
| Settlement code uniqueness | `SettlementCode` unique DB index; format `STS-{providerProfileId}-{currencyCode}-{productCode}-{yyyyMM}` (Phase 3.2) |
| No financials in Phase 3 | `SalePrice`, `CommissionAmount` explicitly `null`; settlement totals `0m` |
| No PaymentTransaction | Not created anywhere in Phase 3 |
| No Invoice | Not created anywhere in Phase 3 |
| No ProviderPayout | Not created anywhere in Phase 3 |
| Existing activation unchanged | `kit.Activate()` called before `ResolveAsync()`; commercial failure rolls back entire Unit of Work |
| ConsignmentRate field only | `agreement.ConsignmentRate` used (not `CommissionRate` — which does not exist); `SalePricePerKit` not used |
| CommercialReviewRequired safety | Missing SalesChannel, missing provider, or missing agreement → attribution flagged for review, no settlement created |

---

## M. Deferred to Phase 4

The following are explicitly **out of scope** for Phase 3 and must not be implemented without owner approval:

- **Payment Transaction creation** — resolving SalePrice from Iyzico webhook events
- **Invoice generation** — linking attribution to invoice records
- **Provider payout automation** — computing `ProviderPayoutAmount` from real payment data and updating settlement financials
- **Settlement processing job** — automated job to close settlements at period end
- **Iyzico integration** — payment gateway calls
- **Credit note / refund logic** — handling refunded kits and reversing attributions
- **Admin Web pages** — frontend UI for commercial attribution and settlement management
- **Settlement manual resolution endpoints** — `ResolveManually()` domain method exists but no command/endpoint yet

---

## N. Phase 3.2 — Monthly Settlement Grouping Alignment

**Date:** 2026-07-03 (addendum, same session)

### Owner Decision

Consignment sell-through settlements must be grouped by:

```
ProviderProfileId + CurrencyCode + ProductCode + Month
```

AgreementId is still stored on the settlement entity for traceability, but it is not a grouping key — a single provider can have multiple agreements over time for the same product, and the settlement should accumulate all activations for a given calendar month regardless of which agreement was active.

### SettlementCode Format

**Before (Phase 3):**
```
STS-{consignmentAgreementId}-{productCode}-{yyyyMM}
Example: STS-45-CD-BASIC-202607
```

**After (Phase 3.2):**
```
STS-{providerProfileId}-{currencyCode}-{productCode}-{yyyyMM}
Example: STS-129-TRY-CD-BASIC-202607
```

ProductCode is normalized: `ToUpperInvariant()`, spaces replaced with `-`. CurrencyCode is `ToUpperInvariant()`.

Column `SettlementCode` widened from `varchar(50)` to `varchar(100)` to safely accommodate the new format.

### Repository Method Changed

**Removed:**
```csharp
GetOpenForAgreementProductAsync(long consignmentAgreementId, string productCode, CancellationToken ct)
```

**Added:**
```csharp
GetOpenForProviderCurrencyProductPeriodAsync(
    long providerProfileId,
    string currencyCode,
    string productCode,
    DateTime periodStartUtc,
    DateTime periodEndUtc,
    CancellationToken ct)
```

Queries by: `ProviderProfileId + CurrencyCode + ProductCode + PeriodStartUtc + PeriodEndUtc + Status == Pending`.

### CommercialActivationService Changes

`HandleConsignmentSellThroughAsync` now:

1. Calculates monthly period from activation date:
   ```csharp
   var periodStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
   var periodEndUtc   = periodStartUtc.AddMonths(1);
   ```
2. Resolves `currencyCode` from `agreement.CurrencyCode ?? "USD"` (was already done; now declared before the settlement lookup)
3. Calls `GetOpenForProviderCurrencyProductPeriodAsync(...)` instead of `GetOpenForAgreementProductAsync(...)`
4. Creates settlement with `periodStartUtc`/`periodEndUtc` (fixed monthly bounds, not agreement dates)
5. Generates settlement code via new `GenerateSettlementCode(providerProfileId, currencyCode, productCode, nowUtc)`

### Migration / Index Changes

**Migration file:** `20260703140000_AddMonthlySettlementGroupingIndexes.cs`

**Up() applies:**
1. `AlterColumn` — `SettlementCode` varchar(50) → varchar(100)
2. `CreateIndex IX_sell_through_settlements_CurrencyCode`
3. `CreateIndex IX_sell_through_settlements_ProviderProfileId_CurrencyCode_ProductCode_PeriodStartUtc_PeriodEndUtc_Status`

**Down() reverts:**
- Drops both new indexes, reverts column to varchar(50)

**EF Configuration updated** (`CargoDrySellThroughSettlementEntityConfiguration`):
- `HasMaxLength(100)` on `SettlementCode`
- Added `HasIndex(x => x.CurrencyCode)`
- Added 6-column composite index for the approved grouping

### Build Result

dotnet SDK not available in sandbox — local build required:

```bash
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/Aizen.Modules.CargoDry.csproj
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Aizen.Modules.CargoDry.Repository.csproj
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Aizen.Bff.AdminPanel.Application.csproj
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
```

**Known compile risks:** None expected — interface and implementation both renamed identically, service updated consistently, no removed types (only method renamed).

---

*Report generated: 2026-07-03 | Author: CargoDry Phase 3 Implementation*
