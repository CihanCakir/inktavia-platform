
namespace Aizen.Modules.Vessel.Abstraction.Dto.Location;

[DocumentationInfo("Vessel location snapshot DTO", "Point-in-time location snapshot for a vessel. No radius search supported.")]
public sealed class VesselLocationSnapshotDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? MarinaName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? AccuracyMeters { get; set; }
    public string? Source { get; set; }
    public DateTime CapturedAt { get; set; }
    public bool IsCurrent { get; set; }
}
