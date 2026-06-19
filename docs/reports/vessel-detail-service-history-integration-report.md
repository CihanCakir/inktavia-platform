# Vessel Detail Service History Integration Report

## Scope

End-to-end status of the Vessel Detail `serviceHistory[]` integration between AdminPanel BFF and ServiceRequest module.

## Integration Path

```
Browser → AdminPanel BFF (GET /api/v1/admin-panel/vessels/{id})
  → Task.WhenAll:
      ├─ IVesselAdminBffRemoteCall.GetVesselById(id)          → Vessel module
      └─ IServiceRequestAdminBffRemoteCall.GetAdminServiceRequestList(vesselId=id, pageSize=10) → ServiceRequest module
  → Aggregate into AdminVesselDetailBffResponse
```

## serviceHistory[] Field Mapping (After Changes)

| BFF Field | Source | Status |
|-----------|--------|--------|
| `id` | `ServiceRequestSummaryDto.Id` | ✅ |
| `date` | `RequestedStartDate ?? CreatedAt` | ✅ |
| `serviceType` | `ServiceTypeCode ?? ServiceCategoryCode` | ✅ Improved |
| `provider` | `null` | ⚠️ Gap — requires Identity/Profile |
| `location` | `LocationMarinaName` | ✅ NEW |
| `notes` | `OwnerNotes ?? Title` | ✅ Improved |
| `status` | `Status.ToString().ToLowerInvariant()` | ✅ Normalized |

## Null-Safety Behavior

- ServiceRequest call is wrapped in `Task.WhenAll` with `ContinueWith(_ => { })` to prevent exception propagation
- If ServiceRequest is unavailable: `serviceHistory: []` + `warning: { module: "ServiceRequest.History", ... }`
- Vessel detail never fails only because ServiceRequest is down

## ServiceRequest Module VesselId Filter — Fixed

**Before:** `GetAdminServiceRequestListQueryHandler` returned empty list when no `OwnerUserId` was provided, even with `VesselId` filter.

**After:** Handler now calls `GetAdminListAsync(filter)` which supports `VesselId` filter directly in EF query.

## ServiceHistoryItemBffDto Shape

```csharp
public sealed class ServiceHistoryItemBffDto
{
    public long Id { get; set; }
    public DateTime? Date { get; set; }
    public string? ServiceType { get; set; }
    public string? Provider { get; set; }    // NEW — null pending Profile integration
    public string? Location { get; set; }    // NEW — mapped from LocationMarinaName
    public string? Notes { get; set; }
    public string? Status { get; set; }
}
```

## Dedicated History Endpoint

A new dedicated endpoint was added to the BFF for vessel-level service history:

```
GET /api/v1/admin-panel/service-requests/by-vessel/{vesselId}/history?take=10
```

**Response:**
```json
{
  "serviceHistory": [
    {
      "id": 1,
      "date": "2026-03-10T00:00:00Z",
      "serviceType": "ANNUAL_SURVEY",
      "provider": null,
      "location": "Bodrum Marina",
      "notes": "Annual certificate renewal",
      "status": "completed"
    }
  ],
  "warnings": []
}
```

## Remaining Gaps

| Gap | Priority | Notes |
|-----|----------|-------|
| `provider` field null | P1 | Requires Identity/Profile module — ProviderProfileId is available |
| `vesselName` not in service history | P2 | Not needed in history item, available in vessel detail context |
| Pre-signed URLs for attachments | P2 | Not relevant for service history list |
