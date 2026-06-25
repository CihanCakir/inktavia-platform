# Vessel Cross-Module Integration Report

## Scope
Cross-module joins required for the Vessel UI BFF contract (AG Vessel Detail Overview page).

## Target Integrations

### CargoDry Module — NOT INTEGRATED

**Status:** CargoDry module does not exist in this repository.

**Contract requirement:** `GET /api/v1/admin-panel/vessels/{id}` should return `cargoDryKits[]` with kit activation data.

**Current behavior:** Returns `cargoDryKits: []` (empty array) in `VesselDetailBffDto`.

**Implementation:** `GetAdminVesselDetailBffQueryHandler` explicitly sets `CargoDryKits = new List<CargoDryKitBffDto>()`.

**Required follow-up:** When CargoDry module is created, implement `ICargoDryAdminBffRemoteCall` with `GetKitsByVesselAsync(vesselId)` and integrate into the detail handler.

**DTO ready:** `CargoDryKitBffDto` exists in BFF Dto folder with all required fields.

---

### ServiceRequest Module — INTEGRATED (null-safe)

**Status:** Integrated via `IServiceRequestAdminBffRemoteCall.GetAdminServiceRequestList`.

**Endpoint used:** `GET /api/v1/admin/service-requests?vesselId={id}&pageSize=10`

**Fields mapped:**
| BFF Field | Source |
|-----------|--------|
| `id` | ServiceRequest.Id |
| `date` | ServiceRequest.CreateDate |
| `serviceType` | ServiceRequest.ServiceType |
| `status` | ServiceRequest.Status |

**Null-safety:** Entire ServiceRequest call wrapped in try/catch. If module is unavailable, `serviceHistory: []` is returned and a warning is added.

**Limitation:** ServiceRequest list item DTO may not expose all fields needed (e.g., `provider`, `location`, `notes`). Requires ServiceRequest module to expose these fields in the admin list response DTO.

---

## Parallel Aggregation Pattern

`GetAdminVesselDetailBffQueryHandler` uses `Task.WhenAll` with `.ContinueWith(_ => { })` wrappers to prevent exceptions from killing the awaited group. Each result is null-checked before use.

```
GET /vessels/{id}
GET /service-requests?vesselId={id}&pageSize=10
```

Both calls run in parallel. Vessel call failure = 404/error response. ServiceRequest failure = empty array + warning.

## Serialization Notes

- No `IPaginate<T>` used in BFF response DTOs — all BFF DTOs are concrete classes
- `VesselPageBffDto` is a concrete BFF-owned pagination shape (not Vessel module's `Paginate<T>`)
- CargoDry and ServiceRequest sub-arrays are `List<T>`, never nullable

## Remaining Gaps

| Gap | Priority | Notes |
|-----|----------|-------|
| CargoDry kit integration | Post-MVP | Module must be created first |
| ServiceRequest field completeness | Post-MVP | Provider, location, notes not mapped yet |
| OwnerName from Identity | Post-MVP | Requires Identity user profile join in BFF |
| HeroImageUrl pre-signed URL | Post-MVP | Requires FileStorage signed URL generation in BFF |
