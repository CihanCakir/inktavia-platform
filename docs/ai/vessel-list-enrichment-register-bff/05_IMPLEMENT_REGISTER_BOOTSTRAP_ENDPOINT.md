# 05 — Implement Vessel Register Bootstrap Endpoint

Implement the endpoint that the React Admin Web Register page expects:

```http
GET /api/v1/admin-panel/vessels/register
```

## Audit first

Check controller route prefix and existing route conventions:

```bash
grep -r "Route(.*admin-panel\|HttpGet\|HttpPost" Bff/src/AdminPanel/Aizen.Bff.AdminPanel* --include="*.cs" | grep -i vessel -n
```

The endpoint must be placed so it does not conflict with:

```http
GET /api/v1/admin-panel/vessels/{vesselId}/detail
GET /api/v1/admin-panel/vessels/{vesselId}/documents
GET /api/v1/admin-panel/vessels/{vesselId}/media
```

Use a literal route:

```csharp
[HttpGet("register")]
```

before ambiguous parameter routes if route order matters.

## Response DTO

Create BFF-owned DTOs, following existing style:

```csharp
public sealed class AdminVesselRegisterBootstrapBffResponse
{
    public VesselRegisterBootstrapBffDto? VesselRegister { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

public sealed class VesselRegisterBootstrapBffDto
{
    public VesselRegisterDefaultsBffDto Defaults { get; set; } = new();
    public VesselRegisterOptionsBffDto Options { get; set; } = new();
}
```

Suggested options:

- `VesselTypes`
- `AssetTypes`
- `OperationalStatuses`
- `OwnershipStatuses`
- `FlagCountries`
- `BuildCountries`
- `HullMaterials`
- `SuperstructureMaterials`
- `HomePorts`
- `OwnerCandidates`

Use the project’s existing option DTO style if available.

## Data sources

Preferred:

1. ReferenceData lookup/country endpoints.
2. Identity/Profile owner candidate endpoint.
3. Static enum fallback for closed Vessel enum-like values.

Fallback must not fail the page. If ReferenceData/Identity is unavailable, return static minimal options and add warnings.

## Handler

Create:

```text
GetAdminVesselRegisterBootstrapQuery
GetAdminVesselRegisterBootstrapQueryHandler
```

The controller action must read `X-Aizen-User-Token` and send it into the query, following existing patterns.

Document in:

```text
docs/reports/vessel-register-bootstrap-endpoint-report.md
```
