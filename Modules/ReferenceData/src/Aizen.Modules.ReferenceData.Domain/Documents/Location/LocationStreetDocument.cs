using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

[AizenCollectionInfo(CollectionName = "reference_location_streets")]
public sealed class LocationStreetDocument : AizenDocumentBase
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public string? NeighborhoodCode { get; set; }
    public string StreetCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }
}
