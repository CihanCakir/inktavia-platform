using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("ServiceRequest trip repository interface", "Data access for live provider trips + their append-only position trail.")]
public interface IServiceRequestTripRepository
{
    Task<ServiceRequestTripEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceRequestTripEntity entity, CancellationToken cancellationToken = default);
    void Update(ServiceRequestTripEntity entity);

    Task AddPositionAsync(ServiceRequestTripPositionEntity position, CancellationToken cancellationToken = default);
    /// <summary>Positions for a trip, oldest→newest (used to compute the arrive/cancel summary + recent-speed ETA).</summary>
    Task<IReadOnlyList<ServiceRequestTripPositionEntity>> GetPositionsAsync(long tripId, CancellationToken cancellationToken = default);
    /// <summary>Most recent N positions, newest→oldest (recent-average-speed ETA).</summary>
    Task<IReadOnlyList<ServiceRequestTripPositionEntity>> GetRecentPositionsAsync(long tripId, int take, CancellationToken cancellationToken = default);
    /// <summary>Hard-delete the raw trail for a trip (privacy purge on arrive/cancel).</summary>
    Task PurgePositionsAsync(long tripId, CancellationToken cancellationToken = default);
}
