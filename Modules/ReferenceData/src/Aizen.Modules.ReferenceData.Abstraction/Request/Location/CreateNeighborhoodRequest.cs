namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class CreateNeighborhoodRequest
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string NeighborhoodCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? PostalCode { get; set; }
}
