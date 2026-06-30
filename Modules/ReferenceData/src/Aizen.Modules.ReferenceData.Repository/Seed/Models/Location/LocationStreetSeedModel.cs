
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Location;

/// <summary>Seed model for a LocationStreet document, read from street JSON files.</summary>
[DocumentationInfo("Seed model representing a street location document loaded from JSON.", "Maps to LocationStreetDocument. Idempotency key: CountryCode + CityCode + DistrictCode + NeighborhoodCode + StreetCode.")]
public sealed class LocationStreetSeedModel
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string? NeighborhoodCode { get; set; }
    public string StreetCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; } = true;
}
