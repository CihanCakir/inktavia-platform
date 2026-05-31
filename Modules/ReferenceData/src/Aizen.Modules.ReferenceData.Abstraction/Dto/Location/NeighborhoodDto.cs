namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

public sealed class NeighborhoodDto
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string NeighborhoodCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }
}
