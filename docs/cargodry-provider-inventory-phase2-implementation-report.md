# CargoDry Phase 2 — Provider Inventory & Batch Allocation
## Implementation Report

**Date:** 2026-07-03  
**Phase:** 2 of N (Phase 1 = Consignment Agreement CQRS + BFF Foundation — completed and build-verified)  
**Scope:** Provider Inventory tracking, Batch Allocation flow, Inventory Movements ledger, BFF + Controller layer  
**Status:** ✅ All files written — awaiting `dotnet build` verification on local machine

---

## A. What Was Implemented

### A1. Domain Layer (Phase 2 foundation — completed prior session)

| Entity / Enum | Location | Notes |
|---|---|---|
| `InventoryMovementType` enum | `CargoDry.Abstraction/Enum/` | BatchAllocated, KitActivated, KitRevoked, KitReturned, ManualAdjustment |
| `CargoDryProviderInventoryEntity` | `CargoDry.Domain/Entities/` | Stock counters + computed `AvailableStock` (not DB-mapped) |
| `CargoDryInventoryMovementEntity` | `CargoDry.Domain/Entities/` | Immutable ledger rows; never updated |
| `CargoDryProviderInventoryEntityConfiguration` | `CargoDry.Repository/Persistence/Configurations/` | Table: `cargodry.provider_inventories` |
| `CargoDryInventoryMovementEntityConfiguration` | `CargoDry.Repository/Persistence/Configurations/` | Table: `cargodry.inventory_movements` |

### A2. Abstraction Layer (Phase 2 foundation — completed prior session)

New DTOs in `CargoDry.Abstraction/Dto/`:
- `CargoDryProviderInventoryDto` — full inventory row DTO
- `CargoDryProviderInventoryListItemDto` — paginated list row
- `CargoDryProviderInventoryPagedResultDto` — paged wrapper
- `CargoDryProviderInventoryDetailDto` — aggregate detail for one provider
- `CargoDryInventoryMovementDto` — movement ledger row
- `CargoDryInventoryMovementPagedResultDto` — paged wrapper
- `AllocateBatchToProviderResult` — allocation result
- `BatchAllocationPreviewDto` — pre-flight eligibility check (includes `CanAllocate`, `BlockingReason`)
- `AdjustInventoryResult` — adjustment result

New repository interfaces in `CargoDry.Domain/Interface/Repository/`:
- `ICargoDryProviderInventoryRepository`
- `ICargoDryInventoryMovementRepository`

### A3. Repository Layer (Phase 2 foundation — completed prior session)

- `CargoDryProviderInventoryRepository` — `GetPagedAsync`, `GetByProviderAsync`, `GetByBatchAndProviderAsync`, `UpsertAsync`
- `CargoDryInventoryMovementRepository` — `GetPagedAsync`, `AddAsync`
- `CargoDryDbContext` extended with `DbSet<CargoDryProviderInventoryEntity>` and `DbSet<CargoDryInventoryMovementEntity>`
- DI registrations added

### A4. Application Layer (Task #430 — completed this session)

#### Commands

**`AllocateBatchToProviderCommand`** — `CargoDry.Application/Commands/AllocateBatchToProvider/`  
- Validates batch exists + not revoked + assigned to requested provider  
- Idempotency: returns existing row if batch already allocated  
- For `ConsignmentSellThrough`: resolves active consignment agreement, calls `agreement.CanAllocate(kitCount, nowUtc)`  
- Creates `CargoDryProviderInventoryEntity` + `CargoDryInventoryMovementEntity` (type: `BatchAllocated`)  
- Updates `agreement.AllocatedKitCount` for ConsignmentSellThrough model  

**`AdjustProviderInventoryCommand`** — `CargoDry.Application/Commands/AdjustProviderInventory/`  
- Positive quantity = add stock, negative = remove stock  
- Calls `inventory.Adjust(quantity, nowUtc)` (entity guards against negative AvailableStock)  
- Appends `CargoDryInventoryMovementEntity` (type: `ManualAdjustment`)  
- `MapToDto()` is declared `internal static` and reused by `GetProviderInventoryDetailQueryHandler`  

#### Queries

| Query | Handler | Notes |
|---|---|---|
| `GetProviderInventoryListQuery` | `GetProviderInventoryListQueryHandler` | Paged + filtered by provider, product, commercialModel, salesChannel, hasAvailableStock, search |
| `GetProviderInventoryDetailQuery` | `GetProviderInventoryDetailQueryHandler` | Aggregate summary per provider; reuses `AdjustProviderInventoryCommandHandler.MapToDto()` |
| `GetProviderInventoryMovementsQuery` | `GetProviderInventoryMovementsQueryHandler` | Paged movement ledger with date range + type filters |
| `GetBatchAllocationPreviewQuery` | `GetBatchAllocationPreviewBffQueryHandler` | Pre-flight eligibility: returns `CanAllocate` + `BlockingReason` (never throws) |

#### Module Controller

**`CargoDryProviderInventoryController`** — `Aizen.Modules.CargoDry/Controllers/`  
Route: `api/v1/cargodry/admin/inventory`  
Authorization: `[Authorize(Roles = "Admin,SuperAdmin")]`

| Method | Route | Handler |
|---|---|---|
| GET | `/` | `GetProviderInventoryListQuery` |
| GET | `/provider/{providerProfileId}` | `GetProviderInventoryDetailQuery` |
| GET | `/movements` | `GetProviderInventoryMovementsQuery` |
| GET | `/preview` | `GetBatchAllocationPreviewQuery` |
| POST | `/allocate` | `AllocateBatchToProviderCommand` |
| POST | `/adjust` | `AdjustProviderInventoryCommand` |

### A5. BFF Layer (Task #431 — completed this session)

#### DTOs
File: `Aizen.Bff.AdminPanel.Application/AdminCargoDry/Dto/CargoDryProviderInventoryBffDtos.cs`  
8 DTO classes: `CargoDryProviderInventoryBffDto`, `CargoDryProviderInventoryListItemBffDto`, `CargoDryProviderInventoryPagedBffDto`, `CargoDryProviderInventoryDetailBffDto`, `CargoDryInventoryMovementBffDto`, `CargoDryInventoryMovementPagedBffDto`, `AllocateBatchToProviderBffResultDto`, `BatchAllocationPreviewBffDto`

#### Remote Call Extension
File: `IAdminCargoDryBffRemoteCall.cs` — 6 new methods added under `// ── Provider Inventory ──` section  
File: `CargoDryRemoteRequests.cs` — 2 new request DTOs added: `AllocateBatchToProviderBffRequest`, `AdjustProviderInventoryBffRequest`

#### BFF Queries (4 pairs)

| Query | Handler | Response Wrapper |
|---|---|---|
| `GetCargoDryInventoryListBffQuery` | `GetCargoDryInventoryListBffQueryHandler` | `GetCargoDryInventoryListBffResponse.PagedResult` |
| `GetCargoDryInventoryDetailBffQuery` | `GetCargoDryInventoryDetailBffQueryHandler` | `GetCargoDryInventoryDetailBffResponse.Detail` |
| `GetCargoDryInventoryMovementsBffQuery` | `GetCargoDryInventoryMovementsBffQueryHandler` | `GetCargoDryInventoryMovementsBffResponse.PagedResult` |
| `GetCargoDryAllocationPreviewBffQuery` | `GetCargoDryAllocationPreviewBffQueryHandler` | `GetCargoDryAllocationPreviewBffResponse.Preview` |

#### BFF Commands (2 pairs)

| Command | Handler | Response Wrapper |
|---|---|---|
| `AllocateBatchToProviderBffCommand` | `AllocateBatchToProviderBffCommandHandler` | `AllocateBatchToProviderBffCommandResponse.Result` |
| `AdjustProviderInventoryBffCommand` | `AdjustProviderInventoryBffCommandHandler` | `AdjustProviderInventoryBffCommandResponse.UpdatedInventory` |

#### BFF Controller Extension
File: `AdminCargoDryController.cs` — 6 new endpoints added under `// ── Provider Inventory ──` section  
Route base: `api/v1/admin-panel/cargodry/inventory`

| Method | Route | BFF Handler |
|---|---|---|
| GET | `inventory` | `GetCargoDryInventoryListBffQuery` |
| GET | `inventory/provider/{providerProfileId}` | `GetCargoDryInventoryDetailBffQuery` |
| GET | `inventory/movements` | `GetCargoDryInventoryMovementsBffQuery` |
| GET | `inventory/preview` | `GetCargoDryAllocationPreviewBffQuery` |
| POST | `inventory/allocate` | `AllocateBatchToProviderBffCommand` |
| POST | `inventory/adjust` | `AdjustProviderInventoryBffCommand` |

New inline body records: `AllocateBatchToProviderBodyRequest`, `AdjustInventoryBodyRequest`

### A6. EF Migration (Task #432)

File: `20260703120000_AddCargoDryProviderInventoryAndMovements.cs`

**Tables created:**

`cargodry.provider_inventories`
- PK: `Id` (bigint identity)
- Columns: ProviderProfileId, ProductCode, BatchCode, CommercialModel, SalesChannel, StockLocationType, TotalAllocated, TotalActivated, TotalRevoked, TotalReturned, TotalAdjusted (all int, defaults 0), LastMovementAtUtc, CreatedAtUtc, UpdatedAtUtc + AizenEntityWithAudit base columns
- Unique index: `(ProviderProfileId, ProductCode, BatchCode) WHERE BatchCode IS NOT NULL`
- Additional indexes: ProviderProfileId, ProductCode, BatchCode, CommercialModel, SalesChannel

`cargodry.inventory_movements`
- PK: `Id` (bigint identity)
- Columns: ProviderProfileId, ProductCode, BatchCode, KitId, MovementType, Quantity, BalanceAfter, CommercialModel (nullable), SalesChannel (nullable), ReferenceType, ReferenceId, Note, CreatedAtUtc, CreatedByUserId + AizenEntityWithAudit base columns
- Indexes: ProviderProfileId, ProductCode, BatchCode, KitId, MovementType, CreatedAtUtc, (ProviderProfileId, ProductCode) composite

---

## B. Key Design Decisions

### B1. AvailableStock is computed, not stored
`AvailableStock = TotalAllocated + TotalAdjusted - TotalActivated - TotalRevoked - TotalReturned`  
EF Config: `builder.Ignore(x => x.AvailableStock)`  
Rationale: avoids stale read / double-update race conditions. Counter columns are the source of truth.

### B2. Allocation Preview returns CanAllocate, never throws
`GetBatchAllocationPreviewQueryHandler` returns structured `BatchAllocationPreviewDto` with `CanAllocate=false` and `BlockingReason` string for all ineligibility cases. Admin UI can show blocking reason inline without error handling.

### B3. Idempotency on AllocateBatchToProvider
If a record for `(ProviderProfileId, BatchCode)` already exists, the command returns the existing row with `AlreadyExists=true`. Safe to retry.

### B4. Consignment agreement cap enforcement
For `ConsignmentSellThrough` commercial model, `CanAllocate(kitCount, nowUtc)` on the agreement entity enforces:
- Status must be `Active`
- Current date must be within `[StartDateUtc, EndDateUtc)`
- `AllocatedKitCount + kitCount <= MaxKitCount`

### B5. Inventory Movement as immutable ledger
`CargoDryInventoryMovementEntity` rows are append-only. No Update or Delete operations. This provides a full audit trail for all stock changes.

### B6. MapToDto reuse (internal static)
`AdjustProviderInventoryCommandHandler.MapToDto()` is `internal static` and reused by `GetProviderInventoryDetailQueryHandler` to avoid duplicate mapping logic. Both are in the same assembly.

---

## C. Out-of-Scope (Not Implemented — Phase 3+)

The following items were explicitly **not** implemented and must not be added without owner approval:

- `CargoDrySalesAttributionEntity`
- `CargoDrySellThroughSettlementEntity`
- Kit activation settlement logic
- Payment integration / invoice generation from inventory events
- Provider payout generation from consignment sell-through
- Consignment settlement cron job
- Admin Web pages (React/TypeScript frontend)

---

## D. Build Verification Steps (run locally)

```bash
# CargoDry module
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/Aizen.Modules.CargoDry.csproj

# CargoDry repository (migration check)
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Aizen.Modules.CargoDry.Repository.csproj

# BFF AdminPanel
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
```

---

## E. File Index

### Module (Aizen.Modules.CargoDry.*)

| File | Status |
|---|---|
| `Application/Commands/AllocateBatchToProvider/AllocateBatchToProviderCommand.cs` | ✅ Written |
| `Application/Commands/AllocateBatchToProvider/AllocateBatchToProviderCommandHandler.cs` | ✅ Written |
| `Application/Commands/AllocateBatchToProvider/AllocateBatchToProviderCommandValidator.cs` | ✅ Written |
| `Application/Commands/AdjustProviderInventory/AdjustProviderInventoryCommand.cs` | ✅ Written |
| `Application/Commands/AdjustProviderInventory/AdjustProviderInventoryCommandHandler.cs` | ✅ Written |
| `Application/Commands/AdjustProviderInventory/AdjustProviderInventoryCommandValidator.cs` | ✅ Written |
| `Application/Queries/GetProviderInventoryList/GetProviderInventoryListQuery.cs` | ✅ Written |
| `Application/Queries/GetProviderInventoryList/GetProviderInventoryListQueryHandler.cs` | ✅ Written |
| `Application/Queries/GetProviderInventoryDetail/GetProviderInventoryDetailQuery.cs` | ✅ Written |
| `Application/Queries/GetProviderInventoryDetail/GetProviderInventoryDetailQueryHandler.cs` | ✅ Written |
| `Application/Queries/GetProviderInventoryMovements/GetProviderInventoryMovementsQuery.cs` | ✅ Written |
| `Application/Queries/GetProviderInventoryMovements/GetProviderInventoryMovementsQueryHandler.cs` | ✅ Written |
| `Application/Queries/GetBatchAllocationPreview/GetBatchAllocationPreviewQuery.cs` | ✅ Written |
| `Application/Queries/GetBatchAllocationPreview/GetBatchAllocationPreviewQueryHandler.cs` | ✅ Written |
| `Controllers/CargoDryProviderInventoryController.cs` | ✅ Written |
| `Repository/Migrations/20260703120000_AddCargoDryProviderInventoryAndMovements.cs` | ✅ Written |

### BFF (Aizen.Bff.AdminPanel.*)

| File | Status |
|---|---|
| `Application/AdminCargoDry/Dto/CargoDryProviderInventoryBffDtos.cs` | ✅ Written |
| `Application/Common/RemoteClients/CargoDry/CargoDryRemoteRequests.cs` | ✅ Extended |
| `Application/Common/RemoteClients/IAdminCargoDryBffRemoteCall.cs` | ✅ Extended |
| `Application/AdminCargoDry/Query/GetCargoDryInventoryList/GetCargoDryInventoryListBffQuery.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryInventoryList/GetCargoDryInventoryListBffQueryHandler.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryInventoryDetail/GetCargoDryInventoryDetailBffQuery.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryInventoryDetail/GetCargoDryInventoryDetailBffQueryHandler.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryInventoryMovements/GetCargoDryInventoryMovementsBffQuery.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryInventoryMovements/GetCargoDryInventoryMovementsBffQueryHandler.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryAllocationPreview/GetCargoDryAllocationPreviewBffQuery.cs` | ✅ Written |
| `Application/AdminCargoDry/Query/GetCargoDryAllocationPreview/GetCargoDryAllocationPreviewBffQueryHandler.cs` | ✅ Written |
| `Application/AdminCargoDry/Command/AllocateBatchToProvider/AllocateBatchToProviderBffCommand.cs` | ✅ Written |
| `Application/AdminCargoDry/Command/AllocateBatchToProvider/AllocateBatchToProviderBffCommandHandler.cs` | ✅ Written |
| `Application/AdminCargoDry/Command/AdjustProviderInventory/AdjustProviderInventoryBffCommand.cs` | ✅ Written |
| `Application/AdminCargoDry/Command/AdjustProviderInventory/AdjustProviderInventoryBffCommandHandler.cs` | ✅ Written |
| `Controllers/V1/AdminCargoDryController.cs` | ✅ Extended (+6 endpoints) |
