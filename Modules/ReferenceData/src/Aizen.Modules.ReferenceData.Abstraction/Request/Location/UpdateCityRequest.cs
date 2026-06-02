namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class UpdateCityRequest
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalCity { get; set; }
    public bool IsActive { get; set; }
}
