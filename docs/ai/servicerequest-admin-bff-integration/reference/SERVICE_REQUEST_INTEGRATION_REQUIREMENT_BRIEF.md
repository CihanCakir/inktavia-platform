# ServiceRequest Integration Requirement Brief

## Context from Vessel reports

Vessel UI/BFF implementation has completed the 4 MVP Vessel GET endpoints and currently aggregates ServiceRequest history into Vessel Detail.

Current ServiceRequest integration status:
- ServiceRequest is called via `IServiceRequestAdminBffRemoteCall.GetAdminServiceRequestList`.
- BFF currently calls an internal route equivalent to `GET /api/v1/admin/service-requests?vesselId={id}&pageSize=10`.
- Current fields mapped into Vessel Detail serviceHistory are limited to `id`, `date`, `serviceType`, and `status`.
- Missing fields are `provider`, `location`, and `notes`.
- The call is null-safe; if ServiceRequest is unavailable, BFF returns `serviceHistory: []` plus warning.

## Target outcome

ServiceRequest module must expose complete admin list/history DTO fields needed by BFF.
AdminPanel BFF must consume these fields and return stable UI-ready response DTOs.

## Required ServiceHistory DTO shape

```csharp
public record ServiceHistoryItemBffDto(
    string Id,
    DateTime Date,
    string ServiceType,
    string? Provider,
    string? Location,
    string? Notes,
    string Status
);
```

## Required behavior

- Vessel Detail must never fail only because ServiceRequest is down.
- ServiceRequest failure must produce an empty array and warning in Vessel Detail.
- When ServiceRequest is up and data exists, `provider`, `location`, and `notes` must be populated if available in the ServiceRequest domain.
