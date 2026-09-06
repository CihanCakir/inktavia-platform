namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Marina;

/// <summary>Seed model for a Marina entity, read from marinas.json (OSM-derived, ODbL).</summary>
[DocumentationInfo("Seed model representing a marina/fishing-harbour loaded from JSON.", "Maps to MarinaEntity. Idempotency key: Code.")]
public sealed class MarinaSeedModel
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = "MARINA";
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? OsmId { get; set; }
    public bool NeedsReview { get; set; }
    public bool IsActive { get; set; } = true;
}
