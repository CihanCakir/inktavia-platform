namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;

/// <summary>A single marina reference record.</summary>
public sealed class MarinaDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; }
}
