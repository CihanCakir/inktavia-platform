using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Trip;

[DocumentationInfo(
    "ServiceRequest trip position",
    "Append-only raw position ping for an active trip. PURGED on arrive/cancel (retention: none after completion — privacy by default). Managed directly via the repository (not loaded onto the trip aggregate) since the trail is high-volume and short-lived.")]
public sealed class ServiceRequestTripPositionEntity : AizenEntityWithAudit
{
    public long TripId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal? Heading { get; private set; }
    public DateTime PingAt { get; private set; }

    public ServiceRequestTripPositionEntity() { }

    public static ServiceRequestTripPositionEntity Create(long tripId, decimal latitude, decimal longitude, decimal? heading, DateTime pingAt)
        => new()
        {
            TripId = tripId,
            Latitude = latitude,
            Longitude = longitude,
            Heading = heading,
            PingAt = pingAt,
            IsActive = true,
        };
}
