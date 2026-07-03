# CargoDry Consignment Agreement — Phase 1 Implementation Report

**Date:** 2026-07-03  
**Author:** Inktavia Marine OS — AI Architect  
**Scope:** Phase 1 — Consignment Agreement CQRS + BFF Foundation  
**Status:** ✅ COMPLETE

---

## A. Overview

Phase 1 establishes the full backend + BFF stack for CargoDry Consignment Agreements. This is the commercial-trust layer that defines the terms under which Inktavia ships CargoDry kits to providers without treating the transfer as an immediate sale (consignment sell-through model, Decision N3).

The implementation follows the existing Inktavia modular monolith conventions end-to-end:

- Domain entity with DDD lifecycle methods
- Repository interface + EF Core implementation
- CQRS commands and queries (module Application layer)
- Module REST controller (admin-only, `[Authorize(Roles = "Admin,SuperAdmin")]`)
- BFF Refit remote call interface
- BFF CQRS commands and queries
- BFF controller with 9 endpoints
- Hand-written EF migration
- DbSet registration + DI registration

Phase 2 (inventory allocation, sell-through settlement, kit linking) is **explicitly deferred** and must not be implemented until owner approval.

---

## B. Domain Layer

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/Entities/CargoDryConsignmentAgreementEntity.cs`

Extends `AizenEntityWithAudit` (provides `PublicId`, `CreateDate`, `ModifyDate`, `IsDeleted`, `IsActive`, etc.).

### Fields

| Field | Type | Purpose |
|---|---|---|
| `AgreementCode` | `string(60)` | Human-readable unique code, e.g. `CSG-2026-PROVIDER-001`. Admin-assigned. |
| `ProviderProfileId` | `long` | Cross-module ID — no EF FK to Identity. Structural validation only. |
| `ProductCode` | `string(50)` | Which CargoDry product this agreement covers. |
| `ConsignmentRate` | `decimal(6,4)` | Provider's share of retail price on each sell-through (0.0–1.0). |
| `MinimumSettlementAmount` | `decimal(18,2)` | Minimum cumulative sell-through before settlement triggers in a billing cycle. |
| `CurrencyCode` | `string(5)` | Default: `TRY`. |
| `MaxKitCount` | `int` | Maximum kits that can be allocated to the provider under this agreement. |
| `AllocatedKitCount` | `int` | Running count of kits already allocated. Default: 0. |
| `Status` | `ConsignmentAgreementStatus` | Lifecycle state: Draft → Active → Suspended → Terminated / Expired. |
| `StartDateUtc` | `DateTime` | Agreement becomes valid at this UTC date. |
| `EndDateUtc` | `DateTime?` | Optional expiry. Null = open-ended. |
| `TermsDocumentRef` | `string?(500)` | Object-storage ref to a terms PDF or similar. |
| `Notes` | `string?(1000)` | Internal admin notes. |
| `ActivatedAtUtc` | `DateTime?` | Set when `Activate()` is called. |
| `SuspendedAtUtc` | `DateTime?` | Set when `Suspend()` is called. |
| `TerminatedAtUtc` | `DateTime?` | Set when `Terminate()` is called. |
| `SuspendReason` | `string?(500)` | Required when suspending. |
| `TerminationReason` | `string?(500)` | Required when terminating. |

### Domain Methods

| Method | Guard |
|---|---|
| `Create(...)` | Factory; sets `Status = Draft`, `AllocatedKitCount = 0`, forces UTC on dates. |
| `Activate()` | Only `Draft → Active`. Throws `InvalidOperationException` otherwise. |
| `Suspend(reason)` | Only `Active → Suspended`. |
| `Terminate(reason)` | Only `Active` or `Suspended → Terminated`. Terminal — no further transitions. |
| `MarkExpired()` | Sets `Active → Expired`. Called by background job (Phase 2+). |
| `UpdateTerms(...)` | Only `Draft` or `Suspended`. Blocked on `Active`, `Terminated`, `Expired`. |
| `IncreaseAllocatedKitCount(count)` | Validates `count > 0` and `AllocatedKitCount + count <= MaxKitCount`. |
| `CanAllocate(count, nowUtc)` | Returns true if `Active`, within date range, and under cap. |
| `IsActive(nowUtc)` | Computed: `Active` status + date range check. |
| `RemainingKitCount` | Computed property: `MaxKitCount - AllocatedKitCount`. |

---

## C. Abstraction Layer

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Enum/ConsignmentAgreementStatus.cs`

```
Draft = 1, Active = 2, Suspended = 3, Terminated = 4, Expired = 5
```

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryConsignmentAgreementDto.cs`

Full response DTO with all fields including computed `RemainingKitCount` and `StatusName`.

---

## D. Application Layer (Module CQRS)

### Commands — 5 total

| Command | Handler action |
|---|---|
| `CreateConsignmentAgreementCommand` | Validates uniqueness (`AgreementCode`, active-per-provider-product), calls `CargoDryConsignmentAgreementEntity.Create(...)`, persists via repository. |
| `UpdateConsignmentAgreementCommand` | Loads by ID (404 if missing), calls `entity.UpdateTerms(...)`. |
| `ActivateConsignmentAgreementCommand` | Loads by ID, calls `entity.Activate()`. |
| `SuspendConsignmentAgreementCommand` | Loads by ID, calls `entity.Suspend(reason)`. |
| `TerminateConsignmentAgreementCommand` | Loads by ID, calls `entity.Terminate(reason)`. |

Each command has a corresponding **FluentValidation validator**:
- `CreateConsignmentAgreementCommandValidator` — validates code format, rate bounds (0 < rate ≤ 1), positive max kit count, start date not in the past, etc.
- Other validators validate ID existence and reason non-empty where required.

### Queries — 4 total

| Query | Returns |
|---|---|
| `GetConsignmentAgreementsPagedQuery` | `(List, Total)` with optional filters: `providerProfileId`, `productCode`, `status`, `dateFrom`, `dateTo`, `search`. |
| `GetConsignmentAgreementByIdQuery` | `CargoDryConsignmentAgreementDto?` |
| `GetConsignmentAgreementByCodeQuery` | `CargoDryConsignmentAgreementDto?` |
| `GetActiveConsignmentAgreementForProviderQuery` | Single active agreement for a provider+product pair at `DateTime.UtcNow`. |

---

## E. Repository Layer

### Interface

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/Interface/Repository/ICargoDryConsignmentAgreementRepository.cs`

```
GetByIdAsync(id)
GetByAgreementCodeAsync(code)
GetActiveForProviderProductAsync(providerProfileId, productCode, nowUtc)
ExistsActiveForProviderProductAsync(providerProfileId, productCode, excludeId?)
GetPagedAsync(filters..., skip, take)
AddAsync(entity)
SaveChangesAsync()
```

### Implementation

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Repositories/CargoDryConsignmentAgreementRepository.cs`

Implements all interface members using `CargoDryDbContext.ConsignmentAgreements`. `GetPagedAsync` builds a dynamic EF Core query with conditional `.Where()` clauses for each filter parameter.

### EF Configuration

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Persistence/Configurations/CargoDryConsignmentAgreementEntityConfiguration.cs`

Maps all column names, lengths, precision, and default values to match the migration exactly.

---

## F. Module Controller

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry/Controllers/CargoDryConsignmentAgreementsController.cs`

Route base: `api/v1/cargodry/admin/consignment/agreements`  
Auth: `[Authorize(Roles = "Admin,SuperAdmin")]`

| Method | Route | Handler |
|---|---|---|
| `GET` | `/` | `GetConsignmentAgreementsPagedQuery` with query-string filters |
| `GET` | `/{id}` | `GetConsignmentAgreementByIdQuery` — 404 if null |
| `GET` | `/code/{code}` | `GetConsignmentAgreementByCodeQuery` — 404 if null |
| `GET` | `/provider/{providerProfileId}/active?productCode=X` | `GetActiveConsignmentAgreementForProviderQuery` — 404 if null |
| `POST` | `/` | `CreateConsignmentAgreementCommand` — 201 Created |
| `PUT` | `/{id}` | `UpdateConsignmentAgreementCommand` — 200 OK |
| `POST` | `/{id}/activate` | `ActivateConsignmentAgreementCommand` — 200 OK |
| `POST` | `/{id}/suspend` | `SuspendConsignmentAgreementCommand` — 200 OK |
| `POST` | `/{id}/terminate` | `TerminateConsignmentAgreementCommand` — 200 OK |

Request models co-located in the controller file: `CreateConsignmentAgreementRequest`, `UpdateConsignmentAgreementRequest`, `AgreementReasonRequest`.

---

## G. BFF Layer

### DTOs

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminCargoDry/Dto/CargoDryConsignmentAgreementBffDtos.cs`

Three types:
- `ConsignmentAgreementBffDto` — full detail (all fields + `StatusName`, `RemainingKitCount`)
- `ConsignmentAgreementListItemBffDto` — lightweight list row
- `ConsignmentAgreementPagedBffDto` — paged wrapper (`Items`, `Total`, `Page`, `PageSize`)

### Remote Call Interface

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IAdminCargoDryBffRemoteCall.cs`

9 new methods added in the `// ── Consignment Agreements` section using `AizenRemoteCallGet/Post/Put` attributes. Refit deserializes module JSON responses directly into BFF DTO types — no manual mapping needed.

### BFF Commands — 5

| Command | File path |
|---|---|
| `CreateConsignmentAgreementBffCommand` | `AdminCargoDry/Command/CreateConsignmentAgreement/` |
| `UpdateConsignmentAgreementBffCommand` | `AdminCargoDry/Command/UpdateConsignmentAgreement/` |
| `ActivateConsignmentAgreementBffCommand` | `AdminCargoDry/Command/ActivateConsignmentAgreement/` |
| `SuspendConsignmentAgreementBffCommand` | `AdminCargoDry/Command/SuspendConsignmentAgreement/` |
| `TerminateConsignmentAgreementBffCommand` | `AdminCargoDry/Command/TerminateConsignmentAgreement/` |

Each file contains both the command class (extends `AizenCommand<TResponse>`) and the response wrapper class.

Each handler (extends `AizenCommandHandler<TCmd, TResponse>`) injects `IAdminCargoDryBffRemoteCall` and delegates to the corresponding remote call method, wrapping the result in the response class.

### BFF Queries — 4

| Query | File path |
|---|---|
| `GetConsignmentAgreementsPagedBffQuery` | `AdminCargoDry/Query/GetConsignmentAgreementsPaged/` |
| `GetConsignmentAgreementByIdBffQuery` | `AdminCargoDry/Query/GetConsignmentAgreementById/` |
| `GetConsignmentAgreementByCodeBffQuery` | `AdminCargoDry/Query/GetConsignmentAgreementByCode/` |
| `GetActiveConsignmentAgreementForProviderBffQuery` | `AdminCargoDry/Query/GetActiveConsignmentAgreementForProvider/` |

Nullable queries (`GetById`, `GetByCode`, `GetActiveForProvider`) have response type `TResponse?` and return `null` when the remote call returns `null`.

### BFF Controller

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminCargoDryController.cs`

9 new endpoints added in `// ── Consignment Agreements` section, all using `IAizenCQRSProcessor` + `SetResponse()` pattern. Body request records (`AgreementReasonBodyRequest`, `CreateConsignmentAgreementBodyRequest`, `UpdateConsignmentAgreementBodyRequest`) co-located at the end of the controller file.

---

## H. EF Migration

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Migrations/20260703110000_AddCargoDryConsignmentAgreements.cs`

### Table: `cargodry.consignment_agreements`

Schema: `cargodry`  
Primary key: `Id bigint GENERATED BY DEFAULT AS IDENTITY`

Column types used:
- `character varying(N)` for all string fields
- `numeric(6,4)` for `ConsignmentRate`
- `numeric(18,2)` for `MinimumSettlementAmount`
- `integer` for `Status`, `MaxKitCount`, `AllocatedKitCount`
- `timestamp with time zone` for all date fields (PostgreSQL-safe UTC)
- `boolean` for `IsDeleted`, `IsActive`
- `uuid` for `PublicId`

Default values:
- `CurrencyCode`: `'TRY'`
- `AllocatedKitCount`: `0`

### Indexes — 7 total

| Index | Type |
|---|---|
| `IX_consignment_agreements_AgreementCode` | UNIQUE |
| `IX_consignment_agreements_ProviderProfileId` | Standard |
| `IX_consignment_agreements_ProductCode` | Standard |
| `IX_consignment_agreements_Status` | Standard |
| `IX_consignment_agreements_ProviderProfileId_ProductCode_Status` | Composite (allocation lookups) |
| `IX_consignment_agreements_StartDateUtc` | Standard |
| `IX_consignment_agreements_EndDateUtc` | Standard |

---

## I. DI Registration

### DbContext

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Persistence/CargoDryDbContext.cs`

```csharp
public DbSet<CargoDryConsignmentAgreementEntity> ConsignmentAgreements => Set<CargoDryConsignmentAgreementEntity>();
```

### Repository

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/DependencyInjection.cs`

```csharp
services.AddScoped<ICargoDryConsignmentAgreementRepository, CargoDryConsignmentAgreementRepository>();
```

Registered in the `// ── PostgreSQL` section alongside the existing Product, Kit, and Batch repositories.

---

## J. Out-of-Scope Decisions (Phase 1 Boundaries)

The following were explicitly excluded from Phase 1 and must not be implemented until owner approval:

| Item | Reason |
|---|---|
| `CargoDryProviderInventoryEntity` | Phase 2 — stock movement tracking |
| `CargoDryInventoryMovementEntity` | Phase 2 — per-kit transfer ledger |
| `AllocateBatchToProviderCommand` | Phase 2 — physically linking a batch to an agreement |
| Sell-through settlement entity | Phase 3 — commission trigger on kit activation |
| Settlement background job | Phase 3 |
| Payment integration / invoice generation | Covered by Payment module — separate scope |
| Admin Web pages (React frontend) | Not part of Phase 1 scope |
| Identity/Profile existence validation | Structural validation only; no cross-module HTTP call during CRUD |

---

## K. File Index

### Module — CargoDry

```
Domain/Entities/CargoDryConsignmentAgreementEntity.cs
Abstraction/Enum/ConsignmentAgreementStatus.cs
Abstraction/Dto/CargoDryConsignmentAgreementDto.cs
Domain/Interface/Repository/ICargoDryConsignmentAgreementRepository.cs
Application/Commands/CreateConsignmentAgreement/CreateConsignmentAgreementCommand.cs
Application/Commands/CreateConsignmentAgreement/CreateConsignmentAgreementCommandHandler.cs
Application/Commands/CreateConsignmentAgreement/CreateConsignmentAgreementCommandValidator.cs
Application/Commands/UpdateConsignmentAgreement/UpdateConsignmentAgreementCommand.cs
Application/Commands/UpdateConsignmentAgreement/UpdateConsignmentAgreementCommandHandler.cs
Application/Commands/UpdateConsignmentAgreement/UpdateConsignmentAgreementCommandValidator.cs
Application/Commands/ActivateConsignmentAgreement/ActivateConsignmentAgreementCommand.cs
Application/Commands/ActivateConsignmentAgreement/ActivateConsignmentAgreementCommandHandler.cs
Application/Commands/ActivateConsignmentAgreement/ActivateConsignmentAgreementCommandValidator.cs
Application/Commands/SuspendConsignmentAgreement/SuspendConsignmentAgreementCommand.cs
Application/Commands/SuspendConsignmentAgreement/SuspendConsignmentAgreementCommandHandler.cs
Application/Commands/SuspendConsignmentAgreement/SuspendConsignmentAgreementCommandValidator.cs
Application/Commands/TerminateConsignmentAgreement/TerminateConsignmentAgreementCommand.cs
Application/Commands/TerminateConsignmentAgreement/TerminateConsignmentAgreementCommandHandler.cs
Application/Commands/TerminateConsignmentAgreement/TerminateConsignmentAgreementCommandValidator.cs
Application/Queries/GetConsignmentAgreementsPaged/GetConsignmentAgreementsPagedQuery.cs
Application/Queries/GetConsignmentAgreementsPaged/GetConsignmentAgreementsPagedQueryHandler.cs
Application/Queries/GetConsignmentAgreementById/GetConsignmentAgreementByIdQuery.cs
Application/Queries/GetConsignmentAgreementById/GetConsignmentAgreementByIdQueryHandler.cs
Application/Queries/GetConsignmentAgreementByCode/GetConsignmentAgreementByCodeQuery.cs
Application/Queries/GetConsignmentAgreementByCode/GetConsignmentAgreementByCodeQueryHandler.cs
Application/Queries/GetActiveConsignmentAgreementForProvider/GetActiveConsignmentAgreementForProviderQuery.cs
Application/Queries/GetActiveConsignmentAgreementForProvider/GetActiveConsignmentAgreementForProviderQueryHandler.cs
Repository/Repositories/CargoDryConsignmentAgreementRepository.cs
Repository/Persistence/Configurations/CargoDryConsignmentAgreementEntityConfiguration.cs
Repository/Persistence/CargoDryDbContext.cs  ← EDITED (added DbSet)
Repository/DependencyInjection.cs            ← EDITED (added AddScoped)
Repository/Migrations/20260703110000_AddCargoDryConsignmentAgreements.cs
Controllers/CargoDryConsignmentAgreementsController.cs
```

### BFF — AdminPanel

```
Application/AdminCargoDry/Dto/CargoDryConsignmentAgreementBffDtos.cs
Application/AdminCargoDry/Command/CreateConsignmentAgreement/CreateConsignmentAgreementBffCommand.cs
Application/AdminCargoDry/Command/CreateConsignmentAgreement/CreateConsignmentAgreementBffCommandHandler.cs
Application/AdminCargoDry/Command/UpdateConsignmentAgreement/UpdateConsignmentAgreementBffCommand.cs
Application/AdminCargoDry/Command/UpdateConsignmentAgreement/UpdateConsignmentAgreementBffCommandHandler.cs
Application/AdminCargoDry/Command/ActivateConsignmentAgreement/ActivateConsignmentAgreementBffCommand.cs
Application/AdminCargoDry/Command/ActivateConsignmentAgreement/ActivateConsignmentAgreementBffCommandHandler.cs
Application/AdminCargoDry/Command/SuspendConsignmentAgreement/SuspendConsignmentAgreementBffCommand.cs
Application/AdminCargoDry/Command/SuspendConsignmentAgreement/SuspendConsignmentAgreementBffCommandHandler.cs
Application/AdminCargoDry/Command/TerminateConsignmentAgreement/TerminateConsignmentAgreementBffCommand.cs
Application/AdminCargoDry/Command/TerminateConsignmentAgreement/TerminateConsignmentAgreementBffCommandHandler.cs
Application/AdminCargoDry/Query/GetConsignmentAgreementsPaged/GetConsignmentAgreementsPagedBffQuery.cs
Application/AdminCargoDry/Query/GetConsignmentAgreementsPaged/GetConsignmentAgreementsPagedBffQueryHandler.cs
Application/AdminCargoDry/Query/GetConsignmentAgreementById/GetConsignmentAgreementByIdBffQuery.cs
Application/AdminCargoDry/Query/GetConsignmentAgreementById/GetConsignmentAgreementByIdBffQueryHandler.cs
Application/AdminCargoDry/Query/GetConsignmentAgreementByCode/GetConsignmentAgreementByCodeBffQuery.cs
Application/AdminCargoDry/Query/GetConsignmentAgreementByCode/GetConsignmentAgreementByCodeBffQueryHandler.cs
Application/AdminCargoDry/Query/GetActiveConsignmentAgreementForProvider/GetActiveConsignmentAgreementForProviderBffQuery.cs
Application/AdminCargoDry/Query/GetActiveConsignmentAgreementForProvider/GetActiveConsignmentAgreementForProviderBffQueryHandler.cs
Application/Common/RemoteClients/IAdminCargoDryBffRemoteCall.cs  ← EDITED (9 new methods)
AdminPanel/Controllers/V1/AdminCargoDryController.cs             ← EDITED (9 new endpoints + 3 body records)
```

---

## L. Phase 2 Readiness

The Phase 1 foundation is designed to support Phase 2 without breaking changes:

- `AllocatedKitCount` and `IncreaseAllocatedKitCount(count)` are already on the entity, ready for the allocation command.
- `CanAllocate(count, nowUtc)` domain guard is implemented and tested-ready.
- `ExistsActiveForProviderProductAsync(...)` repository method enforces one-active-agreement-per-provider-product business rule — Phase 2 allocation will call this guard.
- `GetActiveForProviderProductAsync(...)` is the Phase 2 entry point for kit allocation.
- No settlement, inventory movement, or payment entities are present — these are clean Phase 2 additions.

**Awaiting owner approval before proceeding to Phase 2.**
