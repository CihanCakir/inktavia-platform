using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

[AizenCollectionInfo(CollectionName = "reference_location_neighborhoods")]
public sealed class LocationNeighborhoodDocument : AizenDocumentBase, ISluggableLocation
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string NeighborhoodCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }

    /// <summary>M3 — globally-unique URL slug for the flat by-slug resolver. Backfilled deterministically; null until set.</summary>
    public string? Slug { get; set; }
}
