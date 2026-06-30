
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Location;

/// <summary>Seed model for a LocationCity document, read from cities.json.</summary>
[DocumentationInfo("Seed model representing a city location document loaded from JSON.", "Maps to LocationCityDocument. Idempotency key: CountryCode + CityCode.")]
public sealed class LocationCitySeedModel
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalCity { get; set; }
    public bool IsActive { get; set; } = true;
}
