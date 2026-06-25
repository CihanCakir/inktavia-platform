# 06 — Implement/Register Vessel Create Endpoint If Missing

The immediate 404 is for the register bootstrap GET endpoint, but verify the form submit route too.

Audit frontend/API expectations if available:

```bash
grep -r "vessels/register\|registerVessel\|createVessel\|/vessels" . --include="*.ts" --include="*.tsx" --include="*.cs" | head -200
```

If there is no BFF create endpoint for the register page, implement according to existing convention.

Preferred route if frontend expects it:

```http
POST /api/v1/admin-panel/vessels/register
```

Alternative existing route if already supported:

```http
POST /api/v1/admin-panel/vessels
```

Do not duplicate if one endpoint already works. Add a compatibility alias only if needed by frontend.

## Request DTO

Use current Vessel module register/create command fields. Suggested BFF request:

```csharp
public sealed class RegisterAdminVesselBffRequest
{
    public string Name { get; set; } = default!;
    public string VesselTypeCode { get; set; } = default!;
    public string FlagCountryCode { get; set; } = "TR";
    public int? AssetType { get; set; }
    public int? OperationalStatus { get; set; }
    public string? HomePort { get; set; }
    public string? MmsiNumber { get; set; }
    public string? CallSign { get; set; }
    public string? ImoCertificateNo { get; set; }
    public long? OwnerUserId { get; set; }
    public long? OwnerProfileId { get; set; }

    public VesselSpecificationCreateBffDto? Specification { get; set; }
    public VesselEngineCreateBffDto? Engine { get; set; }
}
```

Adapt to source code. IDs must be `long`.

## Behavior

- Validate required fields.
- Map BFF request to existing Vessel module command/request.
- Forward service token + user token.
- Return created vessel summary in BFF envelope.
- Do not create ServiceRequest data here.

Document in:

```text
docs/reports/vessel-register-create-endpoint-report.md
```
