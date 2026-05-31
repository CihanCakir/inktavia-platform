namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class UpdateDistrictRequest
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalDistrict { get; set; }
    public bool IsActive { get; set; }
}
