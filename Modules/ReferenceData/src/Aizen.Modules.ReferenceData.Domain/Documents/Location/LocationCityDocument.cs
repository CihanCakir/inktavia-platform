using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

public sealed class LocationCityDocument : AizenDocumentBase
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalCity { get; set; }
    public bool IsActive { get; set; }
}
