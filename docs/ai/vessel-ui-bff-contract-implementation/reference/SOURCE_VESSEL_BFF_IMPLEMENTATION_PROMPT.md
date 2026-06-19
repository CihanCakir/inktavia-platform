# VESSEL_BFF_IMPLEMENTATION_PROMPT.md
# Inktavia Marine OS — Vessel Module BFF Implementation
# Copilot Agent Prompt (paste entirely into a Copilot Agent session)

---

You are implementing the **Vessel Module BFF endpoints** for Inktavia Marine OS.
The target project is the **.NET 8 AdminPanel BFF** (`Aizen.AdminPanel.BFF`).

You will read the API contract document, read the existing project structure,
then implement exactly what is specified. This is an ADDITIVE implementation —
never delete or rename existing files unless explicitly required for a migration.

---

## STEP 0 — Read the API contract (MANDATORY)

Read the complete API specification first:

```
docs/ai/bff-vessel-v1/VESSEL_BFF_API_CONTRACT.md
```

This document defines:
- All 8 BFF endpoints with exact request/response shapes
- Which backend modules each endpoint calls
- Which entity fields are new vs existing
- EF Core migration requirements
- Priority order (MVP vs Post-MVP)

Do NOT write a single line of code before reading this document fully.

---

## STEP 1 — Discover the existing BFF project structure

Run the following commands and read every file they reveal:

```bash
# Find the BFF project root
find . -name "*.csproj" | grep -i "bff\|adminpanel" | head -20

# Find existing controllers
find . -path "*/Controllers/*.cs" | grep -iv "test" | sort

# Find existing service registrations and DI setup
find . -name "Program.cs" -o -name "Startup.cs" | head -5

# Find existing DTOs / response models
find . -path "*/Dto*/*.cs" -o -path "*/Models*/*.cs" | grep -iv test | sort | head -40

# Find existing Vessel-related BFF files (if any)
find . -name "*Vessel*" -o -name "*vessel*" | grep -iv test | sort

# Find the response envelope type
grep -r "AizenBffResponse\|BffResponse\|ApiResponse" --include="*.cs" -l | head -10
```

Read the output of each command. Then read:
1. One existing controller fully (e.g. the Identity or ReferenceData BFF controller)
2. One existing BFF service/handler class
3. The `Program.cs` or `Startup.cs` for DI registration pattern
4. The response envelope class (e.g. `AizenBffResponse<T>`)
5. The authentication middleware (how dual headers are validated)

**The existing code patterns are the source of truth. All new code must match them exactly.**

---

## STEP 2 — Discover the Vessel module internal structure

The Vessel module is a separate project in the same solution. Find and read:

```bash
# Find Vessel module project
find . -name "*.csproj" | grep -i vessel | head -10

# Find Vessel module queries and commands
find . -path "*/Vessel*" -name "*Query*.cs" -o -path "*/Vessel*" -name "*Command*.cs" | sort

# Find Vessel entities
find . -path "*/Vessel*" -name "*Entity*.cs" -o -path "*/Vessel*" -name "*Domain*.cs" | sort

# Find Vessel repository/service interfaces
grep -r "IVesselRepository\|IVesselService\|IVesselQuery" --include="*.cs" -l | head -20
```

Read the relevant files. Note:
- Exact interface method signatures for vessel list, detail, documents, media
- Exact entity property names (may differ from BFF contract field names)
- How other modules expose their services to BFF (direct service call? MediatR? HTTP?)

---

## STEP 3 — Discover CargoDry and ServiceRequest modules

```bash
# CargoDry
find . -name "*.csproj" | grep -i cargo | head -5
grep -r "IKitActivation\|ICargoDry\|KitActivationQuery" --include="*.cs" -l | head -10

# ServiceRequest
find . -name "*.csproj" | grep -i service | head -5
grep -r "IServiceRequest\|ServiceHistoryQuery\|VesselId" --include="*.cs" -l | head -10
```

Determine if:
- CargoDry `KitActivation` has a `VesselId` field → if not, note it as missing
- ServiceRequest has a `VesselId` field → if not, note it as missing

---

## STEP 4 — Implement entity/domain changes (ONLY missing fields)

For each field marked **YENİ ALAN** in the contract, check if it already exists.
Only add what is genuinely missing.

### 4a. Vessel entity additions

In `Aizen.Modules.Vessel` domain project, add to the `Vessel` entity:
- `OperationalStatus` (int) — enum: 1=InService, 2=Refit, 3=Idle, 4=Decommissioned
- `AssetType` (int) — enum: 1=MotorYacht, 2=SailingYacht, 3=Superyacht, 4=Catamaran, 5=RIB, 6=Commercial

Use the existing entity mutation pattern (private set, domain method, etc.).

### 4b. VesselDocument additions (if missing)
- `DocumentCategory` (string?)
- `IssuingAuthority` (string?)
- `ApprovedAt` (DateTime?)
- `ApprovedByUserId` (Guid?)

### 4c. VesselDocumentVersion additions (if missing)
- `ThumbnailUrl` (string?)
- `MimeType` (string?)
- `FileSizeBytes` (long?)
- `UploadedByUserId` (Guid?)
- `Notes` (string?)

### 4d. VesselMedia additions (if missing)
- `Title` (string?)
- `Description` (string?)
- `ThumbnailUrl` (string?)
- `MimeType` (string?)
- `FileSizeBytes` (long?)
- `UploadedByUserId` (Guid?)
- `SortOrder` (int, default 0)

### 4e. VesselEngine entity — CREATE if not exists

```csharp
namespace Aizen.Modules.Vessel.Domain.Entities;

public class VesselEngine
{
    public int Id { get; private set; }
    public int VesselId { get; private set; }
    public string? EngineType { get; private set; }
    public int? EngineCount { get; private set; }
    public decimal? EnginePowerKw { get; private set; }
    public string? EngineModel { get; private set; }
    public string? PropulsionType { get; private set; }
    public string? FuelType { get; private set; }
    public int? FuelCapacityL { get; private set; }
    public decimal? MaxSpeedKnots { get; private set; }
    public decimal? CruisingSpeedKnots { get; private set; }
    public int? RangeNm { get; private set; }

    private VesselEngine() { } // EF Core

    public static VesselEngine Create(int vesselId, /* all fields */) { ... }

    public void Update(/* fields */) { ... }
}
```

Place the file in the same directory as other Vessel domain entities.

### 4f. VesselOwnership addition (if missing)
- `OwnershipStatus` (int) — 1=Private, 2=Charter, 3=Corporate

---

## STEP 5 — EF Core migrations

For the Vessel module's `DbContext`, create migrations for all new fields.
Follow the EXACT migration naming pattern already used in the project.

```bash
# Check existing migration naming convention
ls -la src/*/Migrations/ | grep ".cs" | tail -10

# Generate migration (adjust project/context name to match actual)
dotnet ef migrations add AddVesselOperationalAndAssetType \
  --project src/Aizen.Modules.Vessel.Infrastructure \
  --startup-project src/Aizen.AdminPanel.BFF

dotnet ef migrations add AddVesselEngineTable \
  --project src/Aizen.Modules.Vessel.Infrastructure \
  --startup-project src/Aizen.AdminPanel.BFF

# ... add one migration per group of related changes
```

All DateTime columns must use UTC. Use `defaultValueSql: "NOW()"` for PostgreSQL where appropriate.

---

## STEP 6 — Vessel module query extensions

Extend existing vessel query services to include new filter parameters and new fields.

### 6a. VesselListQuery — add new filters

In the existing `VesselListQuery` (or equivalent CQRS command/query):

```csharp
// Add to existing query model — do NOT remove existing fields
public int[]? AssetTypes { get; init; }
public int[]? OwnershipStatuses { get; init; }
public int[]? OperationalStatuses { get; init; }
public string? Search { get; init; }
```

In the handler/repository, apply these filters:
```csharp
if (query.AssetTypes?.Length > 0)
    q = q.Where(v => query.AssetTypes.Contains(v.AssetType));

if (query.OwnershipStatuses?.Length > 0)
    q = q.Where(v => v.Ownership != null && query.OwnershipStatuses.Contains(v.Ownership.OwnershipStatus));

if (query.OperationalStatuses?.Length > 0)
    q = q.Where(v => query.OperationalStatuses.Contains(v.OperationalStatus));

if (!string.IsNullOrWhiteSpace(query.Search))
{
    var s = query.Search.ToLower();
    q = q.Where(v => v.Name.ToLower().Contains(s)
                  || v.VesselCode.ToLower().Contains(s)
                  || (v.Ownership != null && v.Ownership.OwnerName.ToLower().Contains(s)));
}
```

Extend the result projection to include the new fields from the contract:
`thumbnailUrl`, `ownerName`, `ownerAvatarUrl`, `operationalStatus`, `assetType`, `ownershipStatus`,
`latitude`, `longitude`, `lastPositionDate`.

### 6b. VesselDetailQuery — add aggregate data

The detail query must eagerly load:
- `VesselSpec`
- `VesselEngine`
- `VesselOwnership`
- `VesselLocationSnapshot` (latest record only)
- `VesselMedia` (isPrimary=true only, for hero image)
- `VesselDocuments` (top 5 by expiry date, summary fields only)

CargoDry kits and service history are fetched separately by BFF (Step 7).

### 6c. VesselDocumentQuery — add version list

Ensure `GetDocumentsByVesselAsync` returns each document with its full `VesselDocumentVersion[]` list,
ordered by `VersionNumber DESC` (latest first). The `isCurrent` flag = `version.Id == document.CurrentVersionId`.

### 6d. VesselMediaQuery — add new fields

Ensure `GetMediaByVesselAsync` returns `title`, `description`, `thumbnailUrl`, `mimeType`,
`fileSizeBytes`, `uploadedByUserId`, `isPrimary`, `sortOrder`, ordered by `SortOrder ASC`.

### 6e. VesselDocument commands — add Approve and Replace

**ApproveDocumentCommand:**
```csharp
// Set VesselDocument.ApprovedAt = DateTime.UtcNow
// Set VesselDocument.ApprovedByUserId = adminUserId from auth context
// Dispatch domain event: VesselDocumentApprovedEvent
```

**ReplaceDocumentCommand:**
```csharp
// Create new VesselDocumentVersion with uploaded file details
// Set VesselDocument.CurrentVersionId = newVersion.Id
// Reset VesselDocument.ApprovedAt = null (requires re-approval)
// Dispatch domain event: VesselDocumentReplacedEvent
```

---

## STEP 7 — BFF DTOs

Create BFF-layer DTO classes in the BFF project.
Follow the existing DTO naming and directory pattern found in Step 1.

Suggested path: `Aizen.AdminPanel.BFF/Dtos/Vessel/`

```csharp
// VesselListItemBffDto.cs
public record VesselListItemBffDto(
    int Id,
    string VesselCode,
    string Name,
    string Slug,
    string VesselTypeCode,
    string FlagCountryCode,
    string? ThumbnailUrl,
    string? OwnerName,
    string? OwnerAvatarUrl,
    decimal? LengthMeters,
    decimal? GrossTonnage,
    decimal? Latitude,
    decimal? Longitude,
    DateTime? LastPositionDate,
    int OperationalStatus,
    int? AssetType,
    int? OwnershipStatus,
    int Status,
    bool IsArchived,
    DateTime CreateDate
);

// VesselListBffResponse.cs — wraps pagination
public record VesselPageBffDto(
    int From,
    int Index,
    int Size,
    int Count,
    int Pages,
    bool HasPrevious,
    bool HasNext,
    IReadOnlyList<VesselListItemBffDto> Items
);

// VesselEngineBffDto.cs
public record VesselEngineBffDto(
    string? EngineType,
    int? EngineCount,
    decimal? EnginePowerKw,
    string? EngineModel,
    string? PropulsionType,
    string? FuelType,
    int? FuelCapacityL,
    decimal? MaxSpeedKnots,
    decimal? CruisingSpeedKnots,
    int? RangeNm
);

// CargoDryKitBffDto.cs
public record CargoDryKitBffDto(
    string KitId,
    string KitCode,
    string ProductName,
    DateTime ActivatedDate,
    DateTime ExpiryDate,
    int DaysUntilExpiry,
    int EfficiencyPercent,
    string Status  // "active"|"expiring"|"expired"
);

// ServiceHistoryBffDto.cs
public record ServiceHistoryBffDto(
    string Id,
    DateTime Date,
    string ServiceType,
    string? Provider,
    string? Location,
    string? Notes,
    string Status
);

// VesselDocumentSummaryBffDto.cs (for detail page)
public record VesselDocumentSummaryBffDto(
    string Id,
    string DocumentType,
    DateTime? ExpiryDate,
    int? DaysUntilExpiry,
    string DocumentStatus
);

// VesselDetailBffDto.cs — all fields, see contract Section 4.2
public record VesselDetailBffDto(
    int Id,
    string VesselCode,
    string Name,
    string Slug,
    string VesselTypeCode,
    string FlagCountryCode,
    string? HeroImageUrl,
    int? YearBuilt,
    string? BuilderName,
    string? BuildCountry,
    string? HullMaterial,
    string? SuperstructureMaterial,
    decimal? BeamMeters,
    decimal? DraftMeters,
    decimal? LengthMeters,
    decimal? GrossTonnage,
    decimal? NetTonnage,
    int? PassengerCapacity,
    int? CrewCapacity,
    string? ImoCertificateNo,
    string? MmsiNumber,
    string? CallSign,
    string? HomePort,
    decimal? Latitude,
    decimal? Longitude,
    DateTime? LastPositionDate,
    int OperationalStatus,
    int Status,
    bool IsArchived,
    DateTime CreateDate,
    VesselEngineBffDto? Engine,
    IReadOnlyList<CargoDryKitBffDto> CargoDryKits,
    IReadOnlyList<ServiceHistoryBffDto> ServiceHistory,
    IReadOnlyList<VesselDocumentSummaryBffDto> DocumentSummaries
);

// VesselDocumentVersionBffDto.cs
public record VesselDocumentVersionBffDto(
    string VersionId,
    int VersionNumber,
    DateTime UploadedAt,
    string? UploadedByName,
    long? FileSizeBytes,
    string? Notes,
    bool IsCurrent
);

// VesselDocumentBffDto.cs
public record VesselDocumentBffDto(
    string Id,
    string DocumentType,
    string? DocumentCategory,
    DateTime? IssueDate,
    DateTime? ExpiryDate,
    int? DaysUntilExpiry,
    string? IssuingAuthority,
    string DocumentStatus,
    string? FileUrl,
    string? ThumbnailUrl,
    string? MimeType,
    long? FileSizeBytes,
    DateTime? ApprovedAt,
    string? ApprovedByName,
    IReadOnlyList<VesselDocumentVersionBffDto> Versions
);

// VesselMediaBffDto.cs
public record VesselMediaBffDto(
    string Id,
    string MediaType,
    string? Title,
    string? Description,
    string Url,
    string? ThumbnailUrl,
    string? MimeType,
    long? FileSizeBytes,
    DateTime UploadedAt,
    string? UploadedByName,
    bool IsPrimary,
    int SortOrder
);
```

---

## STEP 8 — BFF Service class

Create `Aizen.AdminPanel.BFF/Services/VesselBffService.cs` following the existing BFF service pattern.

```csharp
namespace Aizen.AdminPanel.BFF.Services;

public class VesselBffService
{
    private readonly IVesselQueryService _vesselQuery;
    private readonly IVesselDocumentQueryService _documentQuery;
    private readonly IVesselMediaQueryService _mediaQuery;
    private readonly IVesselCommandService _vesselCommand;
    private readonly IVesselDocumentCommandService _documentCommand;
    private readonly ICargoDryQueryService _cargoDryQuery;   // null-safe: may not exist yet
    private readonly IServiceRequestQueryService _srQuery;    // null-safe: may not exist yet

    // Constructor — inject all. Use IServiceProvider for optional dependencies
    // that may not be registered yet (CargoDry, ServiceRequest cross-module).

    // ── List ──────────────────────────────────────────────────────────────────
    public async Task<VesselPageBffDto> GetVesselListAsync(
        int index, int size, string? search,
        int[]? assetTypes, int[]? ownershipStatuses, int[]? operationalStatuses,
        CancellationToken ct)
    {
        // Call vessel query → map to BffDto → return page
    }

    // ── Detail (aggregate) ───────────────────────────────────────────────────
    public async Task<VesselDetailBffDto?> GetVesselDetailAsync(int id, CancellationToken ct)
    {
        // Parallel fetch: vessel detail + cargoDry kits + service history
        var vesselTask  = _vesselQuery.GetDetailAsync(id, ct);
        var kitsTask    = _cargoDryQuery?.GetKitsByVesselAsync(id, ct) ?? Task.FromResult<IEnumerable<KitActivationDto>>(Array.Empty<KitActivationDto>());
        var historyTask = _srQuery?.GetHistoryByVesselAsync(id, ct) ?? Task.FromResult<IEnumerable<ServiceHistoryDto>>(Array.Empty<ServiceHistoryDto>());

        await Task.WhenAll(vesselTask, kitsTask, historyTask);

        var vessel = vesselTask.Result;
        if (vessel == null) return null;

        return MapToDetailDto(vessel, kitsTask.Result, historyTask.Result);
    }

    // ── Documents ────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<VesselDocumentBffDto>> GetDocumentsAsync(
        int vesselId, string? statusFilter, CancellationToken ct)
    {
        var docs = await _documentQuery.GetByVesselAsync(vesselId, ct);
        var today = DateTime.UtcNow.Date;

        return docs
            .Select(d => {
                var daysUntilExpiry = d.ExpiryDate.HasValue
                    ? (int)(d.ExpiryDate.Value.Date - today).TotalDays
                    : (int?)null;
                var status = ResolveDocumentStatus(daysUntilExpiry, d.IsArchived);
                return MapToDocumentDto(d, daysUntilExpiry, status);
            })
            .Where(d => statusFilter == null || d.DocumentStatus == statusFilter)
            .ToList();
    }

    // ── Media ─────────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<VesselMediaBffDto>> GetMediaAsync(
        int vesselId, string? mediaType, CancellationToken ct)
    {
        var media = await _mediaQuery.GetByVesselAsync(vesselId, ct);
        return media
            .Where(m => mediaType == null || m.MediaType.ToString().ToLower() == mediaType)
            .OrderBy(m => m.SortOrder)
            .Select(MapToMediaDto)
            .ToList();
    }

    // ── Approve ───────────────────────────────────────────────────────────────
    public async Task<VesselDocumentBffDto> ApproveDocumentAsync(
        int vesselId, string documentId, Guid adminUserId, CancellationToken ct)
    {
        await _documentCommand.ApproveAsync(vesselId, Guid.Parse(documentId), adminUserId, ct);
        var updated = await _documentQuery.GetByIdAsync(vesselId, Guid.Parse(documentId), ct);
        return MapToDocumentDto(updated!, null, "valid");
    }

    // ── Replace ───────────────────────────────────────────────────────────────
    public async Task<VesselDocumentBffDto> ReplaceDocumentAsync(
        int vesselId, string documentId, IFormFile file, string? notes,
        Guid adminUserId, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        await _documentCommand.ReplaceAsync(vesselId, Guid.Parse(documentId),
            stream, file.FileName, file.ContentType, file.Length, notes, adminUserId, ct);
        var updated = await _documentQuery.GetByIdAsync(vesselId, Guid.Parse(documentId), ct);
        return MapToDocumentDto(updated!, null, ResolveDocumentStatus(null, false));
    }

    // ── Register ──────────────────────────────────────────────────────────────
    public async Task<object> RegisterVesselAsync(RegisterVesselBffRequest request, CancellationToken ct)
    {
        var result = await _vesselCommand.RegisterAsync(MapToRegisterCommand(request), ct);
        return new { result.Id, result.VesselCode, result.Name, result.Slug, result.CreateDate };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string ResolveDocumentStatus(int? daysUntilExpiry, bool isArchived)
    {
        if (isArchived) return "archived";
        if (daysUntilExpiry == null) return "valid";
        if (daysUntilExpiry <= 0) return "expired";
        if (daysUntilExpiry <= 90) return "expiring";
        return "valid";
    }
}
```

---

## STEP 9 — BFF Controller

Create `Aizen.AdminPanel.BFF/Controllers/Vessel/VesselBffController.cs`.
Follow the EXACT controller base class, attribute, and response pattern from existing controllers (Step 1).

```csharp
[ApiController]
[Route("bff/vessels")]
[Authorize]  // use whatever auth attribute existing controllers use
public class VesselBffController : AizenBffControllerBase  // use actual base class
{
    private readonly VesselBffService _vesselService;

    public VesselBffController(VesselBffService vesselService) =>
        _vesselService = vesselService;

    // GET /bff/vessels
    [HttpGet]
    public async Task<IActionResult> GetVesselList(
        [FromQuery] int index = 0,
        [FromQuery] int size = 20,
        [FromQuery] string? search = null,
        [FromQuery] int[]? assetTypes = null,
        [FromQuery] int[]? ownershipStatuses = null,
        [FromQuery] int[]? operationalStatuses = null,
        CancellationToken ct = default)
    {
        var result = await _vesselService.GetVesselListAsync(
            index, size, search, assetTypes, ownershipStatuses, operationalStatuses, ct);
        return OkBff(new { vessels = result });  // use actual OkBff/envelope helper
    }

    // GET /bff/vessels/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVesselDetail(int id, CancellationToken ct)
    {
        var result = await _vesselService.GetVesselDetailAsync(id, ct);
        if (result == null) return NotFoundBff("vessel.notFound");
        return OkBff(new { vessel = result });
    }

    // GET /bff/vessels/export
    [HttpGet("export")]
    public async Task<IActionResult> ExportVesselRegistry(
        [FromQuery] string format = "xlsx",
        [FromQuery] string? search = null,
        [FromQuery] int[]? assetTypes = null,
        [FromQuery] int[]? ownershipStatuses = null,
        [FromQuery] int[]? operationalStatuses = null,
        CancellationToken ct = default)
    {
        // Export implementation — return file stream
        // For MVP: return 501 Not Implemented if not ready
        return StatusCode(501);
    }

    // POST /bff/vessels
    [HttpPost]
    public async Task<IActionResult> RegisterVessel(
        [FromBody] RegisterVesselBffRequest request,
        CancellationToken ct)
    {
        var result = await _vesselService.RegisterVesselAsync(request, ct);
        return OkBff(new { vessel = result });
    }

    // GET /bff/vessels/{id}/documents
    [HttpGet("{id:int}/documents")]
    public async Task<IActionResult> GetVesselDocuments(
        int id,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _vesselService.GetDocumentsAsync(id, status, ct);
        return OkBff(new { documents = result });
    }

    // POST /bff/vessels/{id}/documents/{documentId}/approve
    [HttpPost("{id:int}/documents/{documentId}/approve")]
    public async Task<IActionResult> ApproveDocument(
        int id, string documentId, CancellationToken ct)
    {
        var adminUserId = GetCurrentAdminUserId();  // from base class or HttpContext
        var result = await _vesselService.ApproveDocumentAsync(id, documentId, adminUserId, ct);
        return OkBff(new { document = result });
    }

    // POST /bff/vessels/{id}/documents/{documentId}/replace
    [HttpPost("{id:int}/documents/{documentId}/replace")]
    [RequestSizeLimit(50 * 1024 * 1024)]  // 50MB
    public async Task<IActionResult> ReplaceDocument(
        int id, string documentId,
        IFormFile file,
        [FromForm] string? notes = null,
        CancellationToken ct = default)
    {
        var adminUserId = GetCurrentAdminUserId();
        var result = await _vesselService.ReplaceDocumentAsync(id, documentId, file, notes, adminUserId, ct);
        return OkBff(new { document = result });
    }

    // GET /bff/vessels/{id}/media
    [HttpGet("{id:int}/media")]
    public async Task<IActionResult> GetVesselMedia(
        int id,
        [FromQuery] string? mediaType = null,
        CancellationToken ct = default)
    {
        var result = await _vesselService.GetMediaAsync(id, mediaType, ct);
        return OkBff(new { media = result });
    }
}
```

---

## STEP 10 — DI Registration

In `Program.cs` (or the DI extension file used by the project), register the new service:

```csharp
builder.Services.AddScoped<VesselBffService>();
```

Follow the exact registration style used for existing BFF services (found in Step 1).

---

## STEP 11 — Build and validate

```bash
# Build entire solution
dotnet build --no-incremental

# Fix ALL errors before proceeding

# Run existing tests
dotnet test --filter "Category!=Integration"
```

Fix all compile errors. Common issues:
- Nullable reference mismatches — use `?` and null-conditional operators correctly
- DTO record constructors — ensure all parameters match mapper assignments
- `Task.WhenAll` result access — use `.Result` only after `await Task.WhenAll(...)`
- Missing using directives — add namespaces for all new types

---

## STEP 12 — Smoke test with curl

Ensure the BFF is running locally, then verify:

```bash
# Get admin token first (or use existing test token)
TOKEN=$(curl -s -X POST http://localhost:5000/bff/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@inktavia.local","password":"Admin!123"}' | jq -r '.body.keycloakAccessToken')

# Test vessel list
curl -s http://localhost:5000/bff/vessels?index=0&size=5 \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.header.isSuccess, .body.vessels.count'

# Test vessel detail (use a known vessel id)
curl -s http://localhost:5000/bff/vessels/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.header.isSuccess, .body.vessel.name'

# Test documents
curl -s http://localhost:5000/bff/vessels/1/documents \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.header.isSuccess, (.body.documents | length)'

# Test media
curl -s http://localhost:5000/bff/vessels/1/media \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.header.isSuccess, (.body.media | length)'
```

Expected result: `header.isSuccess = true` for all endpoints.
If `404` → controller route is wrong.
If `401` → auth header name mismatch.
If `500` → log the exception and fix the query/mapping.

---

## NON-NEGOTIABLE RULES

1. **Read Step 0 (API contract) before writing code.** The contract defines the exact response shapes.

2. **Match existing code patterns exactly** — base controller class, DI style, response envelope, auth attribute, repository pattern. Found by reading Step 1.

3. **Additive only** — never delete or rename existing files, entities, or endpoints.

4. **All DateTime values in UTC** — `DateTime.UtcNow`, PostgreSQL `timestamp with time zone`.

5. **BFF computes derived fields** — `daysUntilExpiry`, `documentStatus`, CargoDry kit `status`, `daysUntilExpiry` for kits. Never push this logic to the database.

6. **Cross-module calls are null-safe** — CargoDry and ServiceRequest may not be available. Use optional DI or try/catch to return empty arrays if those services are unregistered.

7. **`id` is `int` not `Guid`** — vessel IDs are integers in this system.

8. **No hardcoded strings** — all error codes go through the existing error response mechanism.

9. **MVP first** — implement the 4 GET endpoints first (list, detail, documents, media).
   Export and file-upload endpoints can return `501 Not Implemented` initially.

10. **Document every decision** — if an entity field doesn't exist and you skip it, add a `// TODO:` comment with the field name so it's easy to find later.
