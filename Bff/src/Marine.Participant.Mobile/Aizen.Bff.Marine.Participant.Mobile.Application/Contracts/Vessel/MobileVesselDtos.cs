namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;

/// <summary>Compact vessel for the list / Home card / picker. Codes (type/flag) are the stored values; label mapping is optional (M3b).</summary>
public sealed class MobileVesselListItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? TypeCode { get; set; }
    public string? Flag { get; set; }
    public string? Status { get; set; }
    public string? CoverMediaUrl { get; set; }
    public decimal? LengthMeters { get; set; }
    public decimal? GrossTonnage { get; set; }
    public string? MarinaName { get; set; }

    // Owner's explicit choice (null when never set). Present on the card so it can render the chosen region.
    public MobileSelectedLocationDto? SelectedLocation { get; set; }

    // Ready-to-render region label: selected marina/label first, else the last known current-location marina.
    // Lets the card show a region with no extra call and no client-side preference logic.
    public string? DisplayLocationName { get; set; }
}

public sealed class MobileVesselEngineDto
{
    public string? Name { get; set; }
    public string? TypeCode { get; set; }
    public string? FuelTypeCode { get; set; }
    public int? HorsePower { get; set; }
    public bool IsPrimary { get; set; }
}

// ── Location contracts ───────────────────────────────────────────────────────────────────────

/// <summary>Auto-detected current position (the vessel's latest location snapshot).</summary>
public sealed class MobileVesselLocationDto
{
    public string? MarinaName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? CapturedAt { get; set; }
}

/// <summary>The owner's explicit location choice: a marina reference and/or a free-text label.</summary>
public sealed class MobileSelectedLocationDto
{
    public long? MarinaId { get; set; }
    public string? MarinaName { get; set; }
    public string? CustomLabel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? SetAt { get; set; }
}

/// <summary>PUT /api/v1/mobile/vessels/{id}/location/current — record the auto-detected position.</summary>
public sealed class UpdateMobileVesselCurrentLocationRequest
{
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

/// <summary>PUT /api/v1/mobile/vessels/{id}/location/selected — set (or, when all-null, clear) the owner's choice.
/// Provide MarinaId to pick a catalog marina (the BFF resolves its name/coords), and/or a CustomLabel, and/or
/// explicit coordinates.</summary>
public sealed class SetMobileVesselSelectedLocationRequest
{
    public long? MarinaId { get; set; }
    public string? CustomLabel { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

/// <summary>Full vessel detail for the mobile detail screen.</summary>
public sealed class MobileVesselDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? TypeCode { get; set; }
    public string? Flag { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ImoNumber { get; set; }
    public string? MmsiNumber { get; set; }
    public string? CallSign { get; set; }
    public string? HomeMarinaName { get; set; }

    // Specification
    public decimal? LengthMeters { get; set; }
    public decimal? BeamMeters { get; set; }
    public decimal? DraftMeters { get; set; }
    public decimal? GrossTonnage { get; set; }
    public string? HullMaterialCode { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? ProductionYear { get; set; }
    public int? CabinCount { get; set; }

    // Location snapshot (auto-detected CURRENT position). Flat fields kept for back-compat; also exposed
    // structured as CurrentLocation below.
    public string? MarinaName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Structured locations. CurrentLocation = auto-detected device position (append-only snapshot). SelectedLocation
    // = the owner's explicit choice (marina reference or free label). DisplayLocationName is the resolved region
    // label the card should show: SelectedLocation first, else CurrentLocation's marina.
    public MobileVesselLocationDto? CurrentLocation { get; set; }
    public MobileSelectedLocationDto? SelectedLocation { get; set; }
    public string? DisplayLocationName { get; set; }

    public string? CoverMediaUrl { get; set; }
    public List<MobileVesselEngineDto> Engines { get; set; } = new();

    // Archive state (M4d) — an archived vessel is hidden from the active list but its detail stays readable.
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

// ── M4b create payload (the wizard's basic + technical + engine steps, one request) ──────────

/// <summary>Full create payload the mobile wizard submits. Core is required; spec/engine are optional
/// enrichment (the module persists them via separate handlers the BFF orchestrates).</summary>
public sealed class CreateMobileVesselRequest
{
    public MobileVesselCoreInput Core { get; set; } = new();
    public MobileVesselSpecInput? Spec { get; set; }
    public MobileVesselEngineInput? Engine { get; set; }
}

/// <summary>Step 1 — vessel identity. Flag is a country CODE from the countries lookup (not free text).</summary>
public sealed class MobileVesselCoreInput
{
    public string Name { get; set; } = default!;
    public string VesselTypeCode { get; set; } = default!;
    public string? FlagCountryCode { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Description { get; set; }
    public string? ImoNumber { get; set; }
}

/// <summary>Step 2 — technical specs. Length/beam/draft are metres (unit code defaulted server-side).</summary>
public sealed class MobileVesselSpecInput
{
    public int? ProductionYear { get; set; }
    public decimal? LengthMeters { get; set; }
    public decimal? BeamMeters { get; set; }
    public decimal? DraftMeters { get; set; }
    public decimal? GrossTonnage { get; set; }
    public string? HullMaterialCode { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? CabinCount { get; set; }
}

/// <summary>Step 3 — a single engine configuration (the wizard collects one; count is informational).</summary>
public sealed class MobileVesselEngineInput
{
    public string? EngineName { get; set; }
    public string EngineTypeCode { get; set; } = default!;
    public string FuelTypeCode { get; set; } = default!;
    public int? HorsePower { get; set; }
    public int? EnginesCount { get; set; }
}

// ── M4c edit payload — same {core, spec, engine} shape as create ──────────────────────────────

/// <summary>Full edit payload (M4c). Same structured shape as create; the BFF applies patch semantics by
/// merging the provided fields over the vessel's current values, so the module's full-update handlers never
/// null unspecified fields. Every field is optional — omit core/spec/engine (or any field within) to leave
/// that facet unchanged. (Distinct from the create inputs, whose type/fuel codes are required.)</summary>
public sealed class UpdateMobileVesselRequest
{
    public UpdateMobileVesselCoreInput? Core { get; set; }
    public MobileVesselSpecInput? Spec { get; set; }
    public UpdateMobileVesselEngineInput? Engine { get; set; }
}

/// <summary>Editable core fields — all optional (unspecified ⇒ keep current).</summary>
public sealed class UpdateMobileVesselCoreInput
{
    public string? Name { get; set; }
    public string? VesselTypeCode { get; set; }
    public string? FlagCountryCode { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Description { get; set; }
    public string? ImoNumber { get; set; }
}

/// <summary>Editable engine fields — all optional (unspecified ⇒ keep the current primary engine's value).</summary>
public sealed class UpdateMobileVesselEngineInput
{
    public string? EngineName { get; set; }
    public string? EngineTypeCode { get; set; }
    public string? FuelTypeCode { get; set; }
    public int? HorsePower { get; set; }
}

// ── M4d archive / status payloads ──────────────────────────────────────────────────────────────

/// <summary>Archive payload (M4d). All-optional: Reason is a <c>VesselArchiveReason</c> name
/// (Sold/Scrapped/Stolen/Administrative/Other — defaults to Other when blank/unrecognised); Notes is free text.
/// Kept all-optional so a bare archive with no reason never 400s on required-field validation (M4c gotcha).</summary>
public sealed class ArchiveMobileVesselRequest
{
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Status-change payload (M4d). Status is a <c>VesselStatus</c> name — the user-settable operational
/// states are Active / Passive / UnderMaintenance (the module enforces the valid-transition graph and rejects
/// Draft/Sold/Archived, which are system- or archive-driven). Nullable so the body binds without the enum's
/// invalid zero default; the handler rejects a missing/unrecognised status with a clean business error.</summary>
public sealed class UpdateMobileVesselStatusRequest
{
    public string? Status { get; set; }
    public string? Reason { get; set; }
}
