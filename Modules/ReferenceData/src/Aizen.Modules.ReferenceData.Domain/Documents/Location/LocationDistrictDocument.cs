using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

public sealed class LocationDistrictDocument : AizenDocumentBase
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsCoastalDistrict { get; set; }
    public bool IsActive { get; set; }
}
