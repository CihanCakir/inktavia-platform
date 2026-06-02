namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class CreateCityRequest
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalCity { get; set; }
}
