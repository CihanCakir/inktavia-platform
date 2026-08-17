using Aizen.Core.Domain;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel location snapshot entity", "Point-in-time location snapshot for a vessel. Supports approximate and marina-level locations only. No radius search.")]
public sealed class VesselLocationSnapshotEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public string? CountryCode { get; private set; }
    public string? CityCode { get; private set; }
    public string? DistrictCode { get; private set; }
    public string? MarinaName { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal? AccuracyMeters { get; private set; }
    public string? Source { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public bool IsCurrent { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselLocationSnapshotEntity() { }

    public static VesselLocationSnapshotEntity Create(
        long vesselId, string? countryCode, string? cityCode, string? districtCode,
        string? marinaName, decimal? latitude, decimal? longitude, decimal? accuracyMeters,
        string? source, DateTime capturedAt)
    {
        return new VesselLocationSnapshotEntity
        {
            VesselId = vesselId,
            CountryCode = countryCode?.ToUpperInvariant(),
            CityCode = cityCode?.ToUpperInvariant(),
            DistrictCode = districtCode?.ToUpperInvariant(),
            MarinaName = marinaName,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracyMeters,
            Source = source,
            CapturedAt = capturedAt,
            IsCurrent = true,
            IsActive = true
        };
    }

    public void MarkAsCurrent() => IsCurrent = true;
    public void MarkAsHistorical() => IsCurrent = false;
}
