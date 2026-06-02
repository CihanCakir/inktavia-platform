using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Location;

[DocumentationInfo("Update vessel location snapshot request", "Input model for recording a vessel's current location.")]
public sealed class UpdateVesselLocationSnapshotRequest
{
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? MarinaName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? AccuracyMeters { get; set; }
    public string? Source { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
