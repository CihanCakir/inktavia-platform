using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

[AizenCollectionInfo(CollectionName = "reference_location_cities")]
public sealed class LocationCityDocument : AizenDocumentBase, ISluggableLocation
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalCity { get; set; }
    public bool IsActive { get; set; }

    /// <summary>M3 — globally-unique URL slug for the flat by-slug resolver. Backfilled deterministically; null until set.</summary>
    public string? Slug { get; set; }
}
