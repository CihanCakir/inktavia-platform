using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

[AizenCollectionInfo(CollectionName = "reference_location_countries")]
public sealed class LocationCountryDocument : AizenDocumentBase, ISluggableLocation
{
    public string CountryCode { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string DefaultCurrencyCode { get; set; } = default!;
    public string PhoneCode { get; set; } = default!;
    public bool IsActive { get; set; }

    /// <summary>M3 — globally-unique URL slug for the flat by-slug resolver. Backfilled deterministically; null until set.</summary>
    public string? Slug { get; set; }
}
