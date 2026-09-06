namespace Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;

// ── "Not in list" owner submissions (mobile) — created with NeedsReview=true, IsActive=true ──
public sealed class SubmitVesselBrandRequest
{
    public string Name { get; set; } = default!;
    public string? CountryCode { get; set; }
}

public sealed class SubmitVesselModelRequest
{
    public long VesselBrandId { get; set; }
    public string Name { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public decimal? LengthMeters { get; set; }
}

public sealed class SubmitEngineBrandRequest
{
    public string Name { get; set; } = default!;
}

public sealed class SubmitEngineModelRequest
{
    public long EngineBrandId { get; set; }
    public string Name { get; set; } = default!;
    public int? HorsePower { get; set; }
    public string? FuelTypeCode { get; set; }
    public string? EngineTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
}

// ── Admin CRUD ──
public sealed class CreateVesselBrandRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
    public string? CountryCode { get; set; }
}

public sealed class UpdateVesselBrandRequest
{
    public string Name { get; set; } = default!;
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateVesselModelRequest
{
    public long VesselBrandId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public decimal? LengthMeters { get; set; }
}

public sealed class UpdateVesselModelRequest
{
    public string Name { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public decimal? LengthMeters { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateEngineBrandRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
}

public sealed class UpdateEngineBrandRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public sealed class CreateEngineModelRequest
{
    public long EngineBrandId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
    public int? HorsePower { get; set; }
    public string? FuelTypeCode { get; set; }
    public string? EngineTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
}

public sealed class UpdateEngineModelRequest
{
    public string Name { get; set; } = default!;
    public int? HorsePower { get; set; }
    public string? FuelTypeCode { get; set; }
    public string? EngineTypeCode { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public bool IsActive { get; set; } = true;
}
