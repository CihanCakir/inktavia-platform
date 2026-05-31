namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

public sealed class DistrictDto
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalDistrict { get; set; }
    public bool IsActive { get; set; }
}
