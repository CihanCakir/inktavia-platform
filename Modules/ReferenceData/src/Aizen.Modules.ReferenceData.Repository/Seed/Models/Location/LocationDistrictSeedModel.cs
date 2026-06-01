using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Location;

/// <summary>Seed model for a LocationDistrict document, read from district JSON files.</summary>
[DocumentationInfo("Seed model representing a district location document loaded from JSON.", "Maps to LocationDistrictDocument. Idempotency key: CountryCode + CityCode + DistrictCode.")]
public sealed class LocationDistrictSeedModel
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalDistrict { get; set; }
    public bool IsActive { get; set; } = true;
}
