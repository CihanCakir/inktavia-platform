# Vessel Register Create Endpoint Report

## Scope
Implementation of `POST /api/v1/admin-panel/vessels/register` — the form submission endpoint for the React Admin Web Vessel Register page.

## Files Created / Modified

| File | Action |
|------|--------|
| `Modules/Vessel/src/Aizen.Modules.Vessel.Abstraction/Request/Vessel/CreateAdminVesselRequest.cs` | Created |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Command/Vessel/CreateAdminVessel/CreateAdminVesselCommand.cs` | Created |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Command/Vessel/CreateAdminVessel/CreateAdminVesselCommandHandler.cs` | Created |
| `Modules/Vessel/src/Aizen.Modules.Vessel/Controller/V1/Admin/Vessel/VesselAdminController.cs` | Modified — added `POST /api/v1/admin/vessels` |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Command/RegisterAdminVesselBffCommand.cs` | Created — request model + command |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Command/RegisterAdminVesselBffCommandHandler.cs` | Created |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IVesselAdminBffRemoteCall.cs` | Modified — added `CreateAdminVessel` |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminVesselsController.cs` | Modified — added `[HttpPost("vessels/register")]` |

## Existing Endpoint Analysis
The Vessel module had `POST /api/v1/vessels` (user-facing, uses caller identity as owner). This does not support admin-specified owner assignment. A new admin-specific endpoint was added.

## New Module Endpoint
```
POST /api/v1/admin/vessels
Authorization: Roles = Admin
```
Uses `CreateAdminVesselCommandHandler` which accepts an explicit `OwnerUserId` and `OwnerProfileId` instead of reading from `IAizenInfoAccessor`. This is the only difference from the user-facing create command.

## New BFF Endpoint
```
POST /api/v1/admin-panel/vessels/register
Authorization: AdminPanelAccess
X-Aizen-User-Token: Bearer <identityAccessToken>
```

### BFF Request Model
```csharp
public sealed class RegisterAdminVesselBffRequest
{
    public string Name { get; set; }
    public string VesselTypeCode { get; set; }
    public string? FlagCountryCode { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? MmsiNumber { get; set; }
    public string? ImoNumber { get; set; }
    public string? CallSign { get; set; }
    public VesselVisibility Visibility { get; set; }
    public long OwnerUserId { get; set; }   // long, not Guid
    public long? OwnerProfileId { get; set; }
    // ...home location fields
}
```

### Response
Returns the created vessel via the existing `CreateVesselResponse(VesselDto)` envelope.

## ID Convention
- `OwnerUserId`: `long` ✅
- `OwnerProfileId`: `long?` ✅
- No `Guid` introduced ✅

## Auth Flow
```
Browser → POST /api/v1/admin-panel/vessels/register
  X-Aizen-User-Token: Bearer <identityToken>

AdminPanel BFF → POST /api/v1/admin/vessels
  Authorization: Bearer <keycloak-service-token>
  X-Aizen-User-Token: Bearer <identityToken>
```

## Build Validation
```
dotnet build Aizen.sln --no-incremental
→ 0 Error(s)
```

## Remaining Gaps
- No FluentValidation validator added for `RegisterAdminVesselBffRequest` — add if frontend does not enforce required fields.
- Slug uniqueness check deferred to Vessel module domain layer (already present in existing `CreateVesselCommandHandler`).
