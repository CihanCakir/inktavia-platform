namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Catalog;

public sealed class VesselBrandSeedModel
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Per-row provenance URL (manufacturer official page). Falls back to "seed" when absent.</summary>
    public string? Source { get; set; }
    /// <summary>Flags uncertain rows (unknown year range, unverified detail) for the admin review queue.</summary>
    public bool NeedsReview { get; set; }
}

public sealed class VesselModelSeedModel
{
    /// <summary>References the parent brand by its Code (resolved to id at seed time).</summary>
    public string BrandCode { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    /// <summary>Production start year — null when undocumented (row should carry NeedsReview=true).</summary>
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public decimal? LengthMeters { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Per-row provenance URL (manufacturer official page). Falls back to "seed" when absent.</summary>
    public string? Source { get; set; }
    /// <summary>Flags uncertain rows (unknown year range, unverified detail) for the admin review queue.</summary>
    public bool NeedsReview { get; set; }
}

public sealed class EngineBrandSeedModel
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    /// <summary>Per-row provenance URL (manufacturer official page). Falls back to "seed" when absent.</summary>
    public string? Source { get; set; }
    /// <summary>Flags uncertain rows (unknown year range, unverified detail) for the admin review queue.</summary>
    public bool NeedsReview { get; set; }
}

public sealed class EngineModelSeedModel
{
    public string BrandCode { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int? HorsePower { get; set; }
    public string? FuelTypeCode { get; set; }
    public string? EngineTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Per-row provenance URL (manufacturer official page). Falls back to "seed" when absent.</summary>
    public string? Source { get; set; }
    /// <summary>Flags uncertain rows (unknown year range, unverified detail) for the admin review queue.</summary>
    public bool NeedsReview { get; set; }
}
