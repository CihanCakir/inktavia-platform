using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Trip;

[DocumentationInfo(
    "ServiceRequest trip entity",
    "One live provider trip for an accepted job. Tracking is active ONLY while EnRoute (between the provider's explicit start and arrive/cancel). The raw position trail is stored separately (trip_positions) and PURGED on arrive/cancel — only the last-known position + a coarse summary remain (privacy by default).")]
public sealed class ServiceRequestTripEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long ProviderProfileId { get; private set; }
    public TripStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Last-known position (survives the trail purge so a late owner still sees where the provider ended up).
    public decimal? LastLatitude { get; private set; }
    public decimal? LastLongitude { get; private set; }
    public decimal? LastHeading { get; private set; }
    public DateTime? LastPingAt { get; private set; }

    // Summary computed from the trail on arrive/cancel, then the raw rows are purged.
    public decimal? TotalDistanceKm { get; private set; }
    public int? DurationSeconds { get; private set; }

    public ServiceRequestTripEntity() { }

    public static ServiceRequestTripEntity Start(long serviceRequestId, long providerProfileId)
        => new()
        {
            ServiceRequestId = serviceRequestId,
            ProviderProfileId = providerProfileId,
            Status = TripStatus.EnRoute,
            StartedAt = DateTime.UtcNow,
            IsActive = true,
        };

    /// <summary>Re-arm a terminal trip row for a fresh en-route (one row per SR). Clears the prior end state,
    /// last-known position and summary. Throws if the trip is still active.</summary>
    public void RestartEnRoute(long providerProfileId)
    {
        if (Status == TripStatus.EnRoute)
            throw new AizenBusinessException("TRIP_ALREADY_ACTIVE");
        ProviderProfileId = providerProfileId;
        Status = TripStatus.EnRoute;
        StartedAt = DateTime.UtcNow;
        ArrivedAt = null;
        CancelledAt = null;
        LastLatitude = null;
        LastLongitude = null;
        LastHeading = null;
        LastPingAt = null;
        TotalDistanceKm = null;
        DurationSeconds = null;
        IsActive = true;
    }

    /// <summary>Record a position ping. Only valid while EnRoute. Updates the last-known position; the raw trail row
    /// is appended by the repository. Server-side throttling (min interval) is enforced by the command handler.</summary>
    public void Ping(decimal latitude, decimal longitude, decimal? heading, DateTime pingAt)
    {
        EnsureEnRoute();
        LastLatitude = latitude;
        LastLongitude = longitude;
        LastHeading = heading;
        LastPingAt = pingAt;
    }

    /// <summary>Mark arrived (terminal). Summary is set separately from the trail before the purge.</summary>
    public void Arrive()
    {
        EnsureEnRoute();
        Status = TripStatus.Arrived;
        ArrivedAt = DateTime.UtcNow;
    }

    /// <summary>Cancel the trip (terminal).</summary>
    public void Cancel()
    {
        EnsureEnRoute();
        Status = TripStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>Store the trail-derived summary (called on arrive/cancel, before purging the raw rows).</summary>
    public void SetSummary(decimal? totalDistanceKm, int? durationSeconds)
    {
        TotalDistanceKm = totalDistanceKm;
        DurationSeconds = durationSeconds;
    }

    public bool IsEnRoute => Status == TripStatus.EnRoute;

    private void EnsureEnRoute()
    {
        if (Status != TripStatus.EnRoute)
            throw new AizenBusinessException("TRIP_NOT_ENROUTE");
    }
}
