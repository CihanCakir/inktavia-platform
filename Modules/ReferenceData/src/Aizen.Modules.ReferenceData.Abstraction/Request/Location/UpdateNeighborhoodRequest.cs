namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class UpdateNeighborhoodRequest
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string NeighborhoodCode { get; set; } = default!;
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }
}
