using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Location;

/// <summary>Seed model for a LocationNeighborhood document, read from neighborhood JSON files.</summary>
[DocumentationInfo("Seed model representing a neighborhood location document loaded from JSON.", "Maps to LocationNeighborhoodDocument. Idempotency key: CountryCode + CityCode + DistrictCode + NeighborhoodCode.")]
public sealed class LocationNeighborhoodSeedModel
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string NeighborhoodCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; } = true;
}
