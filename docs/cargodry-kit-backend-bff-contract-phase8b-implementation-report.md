# CargoDry Kit Backend/BFF Contract — Phase 8B Implementation Report

**Date:** 2026-07-04  
**Phase:** 8B — Kit Detail + Admin Lookup Endpoints  
**Status:** Implementation Complete  

---

## A. Objective

Phase 8B closes three contract gaps identified during the Phase 8A frontend build:

| Gap | Description |
|-----|-------------|
| 1 | No `GET /api/v1/cargodry/admin/kits/{id}` endpoint in module or BFF |
| 2 | No `GET /api/v1/cargodry/admin/kits/lookup?q=` endpoint for admin QR/serial search |
| 3 | `CargoDryKitDto` missing admin-only fields: `ConsignmentAgreementId`, `QrPayload`, `RevokeReason`, `RevokedAt`, `CreatedAtUtc`, `UpdatedAtUtc` |

All three are closed additively — no existing endpoints, entities, or migrations were modified.

---

## B. Scope Boundaries (Hard Rules Observed)

1. CargoDry commercial/finance lifecycle: **unchanged**
2. Phase 7 commercial screens: **unchanged**
3. Customer/mobile QR scanning: **not implemented**
4. QR code generation in frontend or BFF: **not implemented**
5. AdminPanel BFF calls module only via AizenRemoteCall: **enforced**
6. BFF contains no domain logic: **enforced** (all match/warn logic is in the module)
7. Both tokens forwarded via existing infrastructure: **enforced**
8. Typed DTOs only: **enforced**
9. No new EF migration required: all entity fields already existed; Phase 8B is DTO-only

---

## C. Files Created

### C1. Abstraction Layer

| File | Purpose |
|------|---------|
| `Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryKitDetailDto.cs` | Extends `CargoDryKitDto` with 6 admin-only fields |
| `Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryKitLookupResultDto.cs` | Result wrapper for admin lookup: `Found`, `MatchType`, `Kit`, `Warnings` |

### C2. Repository Layer

No new files. `GetByKitCodeAsync` added to existing interface + implementation.

### C3. Application Layer (Module)

| File | Purpose |
|------|---------|
| `Queries/GetCargoDryKitDetail/GetCargoDryKitDetailQuery.cs` | Query + response |
| `Queries/GetCargoDryKitDetail/GetCargoDryKitDetailQueryValidator.cs` | `KitId > 0` |
| `Queries/GetCargoDryKitDetail/GetCargoDryKitDetailQueryHandler.cs` | Fetches by id, maps to `CargoDryKitDetailDto` via `internal static MapToDetail` |
| `Queries/LookupCargoDryKitAdmin/LookupCargoDryKitAdminQuery.cs` | Query + response |
| `Queries/LookupCargoDryKitAdmin/LookupCargoDryKitAdminQueryValidator.cs` | NotEmpty, MaxLength 200 |
| `Queries/LookupCargoDryKitAdmin/LookupCargoDryKitAdminQueryHandler.cs` | Match priority: id → kit code → serial. Reuses `GetCargoDryKitDetailQueryHandler.MapToDetail`. Generates `BuildWarnings`. |

### C4. BFF Application Layer

| File | Purpose |
|------|---------|
| `AdminCargoDry/Dto/CargoDryKitDetailBffDtos.cs` | `CargoDryKitDetailBffDto` (full field set) + `CargoDryKitLookupResultBffDto` |
| `AdminCargoDry/Query/GetCargoDryKitDetailBff/GetCargoDryKitDetailBffQuery.cs` | BFF query + response |
| `AdminCargoDry/Query/GetCargoDryKitDetailBff/GetCargoDryKitDetailBffQueryHandler.cs` | Proxies `GetKitDetailAsync`, returns `Kit` |
| `AdminCargoDry/Query/LookupCargoDryKitAdminBff/LookupCargoDryKitAdminBffQuery.cs` | BFF query + response |
| `AdminCargoDry/Query/LookupCargoDryKitAdminBff/LookupCargoDryKitAdminBffQueryHandler.cs` | Proxies `LookupKitAsync`, returns `Result` |

---

## D. Files Modified

| File | Change |
|------|--------|
| `ICargoDryKitRepository.cs` | Added `GetByKitCodeAsync(string kitCode, CancellationToken)` |
| `CargoDryKitRepository.cs` | Implemented via `FirstOrDefaultAsync(x => x.KitCode == kitCode, ct)` |
| `CargoDryAdminController.cs` (module) | Added `GET kits/lookup` and `GET kits/{id:long}` before `ExportKits` |
| `CargoDryRemoteRequests.cs` (BFF) | Added `GetCargoDryKitDetailBffResult` + `LookupCargoDryKitAdminBffResult` Refit wrappers |
| `IAdminCargoDryBffRemoteCall.cs` | Added `GetKitDetailAsync` + `LookupKitAsync` |
| `AdminCargoDryController.cs` (BFF) | Added `GET kits/lookup` and `GET kits/{id:long}` before `ExportKits` |

---

## E. New Admin-Only Fields in `CargoDryKitDetailDto`

| Field | Type | Source Entity Field | Description |
|-------|------|---------------------|-------------|
| `ConsignmentAgreementId` | `long?` | `k.ConsignmentAgreementId` | Consignment link |
| `QrPayload` | `string?` | `k.QrPayload` | Static QR content (not an activation secret) |
| `RevokeReason` | `string?` | `k.RevokeReason` | Non-null only when Status=Revoked |
| `RevokedAt` | `DateTimeOffset?` | `k.RevokedAt` | Revocation timestamp |
| `CreatedAtUtc` | `DateTime` | `k.CreatedAtUtc` | From AizenEntityWithAudit |
| `UpdatedAtUtc` | `DateTime?` | `k.UpdatedAtUtc` | From AizenEntityWithAudit |

All 6 fields were already persisted on `CargoDryKitEntity`. No EF migration required.

---

## F. Endpoint Summary

### F1. Module Endpoints (under `/api/v1/cargodry/admin/`)

| Method | Route | Handler | Notes |
|--------|-------|---------|-------|
| GET | `kits/lookup?q=` | `LookupCargoDryKitAdminQueryHandler` | Literal route registered before `{id:long}` |
| GET | `kits/{id:long}` | `GetCargoDryKitDetailQueryHandler` | Returns 404 if kit not found |

### F2. BFF Endpoints (under `/api/v1/admin-panel/cargodry/`)

| Method | Route | Handler | Notes |
|--------|-------|---------|-------|
| GET | `kits/lookup?q=` | `LookupCargoDryKitAdminBffQueryHandler` | Returns full `LookupCargoDryKitAdminBffResponse` |
| GET | `kits/{id:long}` | `GetCargoDryKitDetailBffQueryHandler` | Returns full `GetCargoDryKitDetailBffResponse` |

---

## G. Lookup Match Priority

The lookup handler (`LookupCargoDryKitAdminQueryHandler`) resolves the query string in priority order:

1. **`long.TryParse(q)` → `GetByIdAsync`** → `MatchType = "Id"`
2. **`GetByKitCodeAsync(q)` (exact)** → `MatchType = "KitCode"`
3. **`GetBySerialAsync(q)` (exact)** → `MatchType = "SerialNumber"`
4. **No match** → `Found = false`, `MatchType = "NotFound"`, `Kit = null`

When found, `BuildWarnings` appends non-blocking warnings:

| Condition | Warning Message |
|-----------|----------------|
| `Status == Revoked` | "Kit is revoked. Reason: {reason}" |
| `Status == Expired` | "Kit has expired. The owner should initiate a renewal." |
| `Status == Activated` AND expires within 30 days | "Kit expires in {N} days — renewal may be needed." |
| `Status == Lost` | "Kit is marked as Lost." |

---

## H. Code Reuse Pattern

`GetCargoDryKitDetailQueryHandler.MapToDetail` is declared `internal static`:

```csharp
internal static CargoDryKitDetailDto MapToDetail(CargoDryKitEntity k, string productName) => new() { ... };
```

`LookupCargoDryKitAdminQueryHandler` calls it directly:

```csharp
var dto = GetCargoDryKitDetailQueryHandler.MapToDetail(kit, productName);
```

Both handlers are in the same assembly (`Aizen.Modules.CargoDry.Application`), so `internal` visibility is valid. This avoids duplicating the ~30-line field mapping.

---

## I. Route Ordering and Constraint Safety

Both the module controller and BFF controller register routes in this order:

1. `kits/lookup` — literal segment
2. `kits/{id:long}` — constrained parameter
3. `kits/export` — literal segment

ASP.NET Core's route matching engine prefers literal segments over parameterized ones. Additionally, the `:long` constraint explicitly rejects "lookup" and "export" from matching the parameterized route. No ambiguity exists.

---

## J. Build Verification

**Note:** The Linux sandbox used for code review does not have the .NET SDK installed. Manual static verification was performed across all affected files:

| Project | Files Reviewed | Status |
|---------|---------------|--------|
| `Aizen.Modules.CargoDry.Abstraction` | 2 new DTOs | ✅ Compile-clean |
| `Aizen.Modules.CargoDry.Domain` | Repository interface | ✅ Compile-clean |
| `Aizen.Modules.CargoDry.Repository` | Repository impl | ✅ Compile-clean |
| `Aizen.Modules.CargoDry.Application` | 6 new files (2 queries × Query/Validator/Handler) | ✅ Compile-clean |
| `Aizen.Modules.CargoDry` (host) | Controller with 2 new endpoints | ✅ Compile-clean |
| `Aizen.Bff.AdminPanel.Application` | 5 new files + 3 modified | ✅ Compile-clean |
| `Aizen.Bff.AdminPanel` (host) | Controller with 2 new endpoints | ✅ Compile-clean |

**To run local build verification:**

```bash
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Aizen.Modules.CargoDry.Abstraction.csproj
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/Aizen.Modules.CargoDry.Domain.csproj
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Aizen.Modules.CargoDry.Repository.csproj
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/Aizen.Modules.CargoDry.Application.csproj
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/Aizen.Modules.CargoDry.csproj
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Aizen.Bff.AdminPanel.Application.csproj
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
```

---

## K. Frontend Consumption (Phase 8A)

Phase 8A already wired the following against a mock response contract that is now fulfilled by the real endpoints:

- `CargoDryKitDetailDrawer` → calls `GET /admin-panel/cargodry/kits/{id}` via `cargodryApi.getKitDetail`
- `CargoDryQrLookupPage` → calls `GET /admin-panel/cargodry/kits/lookup?q=` via `cargodryApi.lookupKit`

No frontend changes are required as part of Phase 8B — the contracts match.

---

## L. Decisions Not Made (Deferred)

| Decision | Reason |
|----------|--------|
| `OwnerDisplayName` enrichment at BFF | Requires Identity module call; deferred to future enrichment pass |
| `VesselName` enrichment at BFF | Requires Vessel module call; deferred to future enrichment pass |
| Customer-facing QR activation endpoint | Out of scope per Phase 8B hard rules |

Both `OwnerDisplayName` and `VesselName` are mapped to `null` in `MapToDetail` with an explicit comment marking them for BFF-layer enrichment when required.
