using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Mapping;

public static class TripMappingExtensions
{
    public static TripDto ToDto(this ServiceRequestTripEntity e, double? etaMinutes = null) => new()
    {
        ServiceRequestId = e.ServiceRequestId,
        Status = e.Status,
        StartedAt = e.StartedAt,
        ArrivedAt = e.ArrivedAt,
        CancelledAt = e.CancelledAt,
        LastLatitude = e.LastLatitude,
        LastLongitude = e.LastLongitude,
        LastHeading = e.LastHeading,
        LastPingAt = e.LastPingAt,
        EtaMinutes = etaMinutes,
        TotalDistanceKm = e.TotalDistanceKm,
        DurationSeconds = e.DurationSeconds,
    };
}
