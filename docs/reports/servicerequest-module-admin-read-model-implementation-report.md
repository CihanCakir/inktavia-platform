# ServiceRequest Module Admin Read Model Implementation Report

## Scope

Changes made to the ServiceRequest module Application, Domain, Repository, and Abstraction layers to support the admin read model.

## Files Changed

### Abstraction Layer

#### `ServiceRequestSummaryDto.cs`
**Path:** `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Abstraction/Dto/ServiceRequestSummaryDto.cs`

**Fields added:**
- `ServiceTypeCode` — service type code (e.g. `SURVEY_ANNUAL`)
- `OwnerUserId` — boat owner user ID
- `ProviderProfileId` — assigned provider profile ID (nullable, from Assignment)
- `LocationMarinaName` — marina/location free-text name
- `LocationCityCode` — city code (e.g. `BODRUM`)
- `LocationCountryCode` — country code (e.g. `TR`)
- `OwnerNotes` — request notes from the boat owner
- `LastActivityAt` — last modification date (mapped from `ModifyDate ?? CreateDate`)

### Domain Layer

#### `IServiceRequestRepository.cs`
**Path:** `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Domain/Interface/Repository/IServiceRequestRepository.cs`

**Methods added:**
```csharp
Task<IReadOnlyList<ServiceRequestEntity>> GetAdminListAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default);
Task<int> CountAdminAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default);
```

### Repository Layer

#### `ServiceRequestRepository.cs`
**Path:** `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Repositories/ServiceRequestRepository.cs`

**Methods added:**
- `GetAdminListAsync` — EF Core query with `BuildAdminQuery` predicate + `.Include(x => x.Assignment)` + ordering/skip/take
- `CountAdminAsync` — count using same `BuildAdminQuery` predicate

**`BuildAdminQuery` supports filters:**
| Filter | Implementation |
|--------|---------------|
| `VesselId` | `x.VesselId == filter.VesselId.Value` |
| `OwnerUserId` | `x.OwnerUserId == filter.OwnerUserId.Value` |
| `ProviderProfileId` | `x.Assignment != null && x.Assignment.ProviderProfileId == ...` |
| `Status` | `x.Status == filter.Status.Value` |
| `Priority` | `x.Priority == filter.Priority.Value` |
| `HasDispute` | `x.Dispute != null / == null` |
| `ServiceCategoryCode` | `x.ServiceCategoryCode == code.ToUpperInvariant()` |
| `CreatedFrom/To` | Date range predicates |
| `SearchTerm` | `Title.ToLower().Contains(term) OR RequestCode.ToLower().Contains(term)` |

**Note:** Query includes `Assignment` navigation property so `ProviderProfileId` and `HasActiveAssignment` are correctly populated.

#### `ServiceRequestMappingExtensions.cs`
**Path:** `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Mapping/ServiceRequestMappingExtensions.cs`

**`ToSummaryDto` updated:** maps all 8 new fields from entity.

### Application Layer

#### `GetAdminServiceRequestListQueryHandler.cs`
**Before:** Only supported `OwnerUserId` filter; returned empty list for all other queries.
**After:** Calls `GetAdminListAsync` + `CountAdminAsync` with full filter support.

## Build Validation

```
dotnet build Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/Aizen.Modules.ServiceRequest.csproj --no-incremental
```
**Result:** ✅ Build succeeded — 0 errors, warnings are pre-existing.

## Architecture Compliance

- CQRS pattern maintained (Query and Handler in separate files)
- Repository interface in Domain layer, implementation in Repository layer
- All changes are additive (no breaking changes to existing fields)
- `DocumentationInfo` preserved on all modified classes
- No domain logic moved into BFF
