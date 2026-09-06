namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;

/// <summary>Simple success envelope for admin lifecycle actions (approve/activate/deactivate).</summary>
public sealed record BoolResult(bool Success);

public sealed class VesselBrandDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; }
    public bool NeedsReview { get; set; }
    public string? Source { get; set; }
}

public sealed class VesselModelDto
{
    public long Id { get; set; }
    public long VesselBrandId { get; set; }
    /// <summary>Denormalized brand name — lets the wizard/BFF render + denormalize without a second call.</summary>
    public string? BrandName { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public decimal? LengthMeters { get; set; }
    public bool IsActive { get; set; }
    public bool NeedsReview { get; set; }
    public string? Source { get; set; }
}

public sealed class EngineBrandDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool NeedsReview { get; set; }
    public string? Source { get; set; }
}

public sealed class EngineModelDto
{
    public long Id { get; set; }
    public long EngineBrandId { get; set; }
    public string? BrandName { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int? HorsePower { get; set; }
    public string? FuelTypeCode { get; set; }
    public string? EngineTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public bool IsActive { get; set; }
    public bool NeedsReview { get; set; }
    public string? Source { get; set; }
}
