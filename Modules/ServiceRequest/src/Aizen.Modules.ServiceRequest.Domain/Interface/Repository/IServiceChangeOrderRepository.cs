using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service change order repository interface", "Data access contract for BE-S11b post-acceptance change orders.")]
public interface IServiceChangeOrderRepository
{
    Task<ServiceChangeOrderEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceChangeOrderEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    /// <summary>Count of change orders already proposed for the accepted offer — drives the next 1-based SequenceNo.</summary>
    Task<int> CountForOfferAsync(long acceptedOfferId, CancellationToken ct = default);
    Task AddAsync(ServiceChangeOrderEntity entity, CancellationToken ct = default);
    void Update(ServiceChangeOrderEntity entity);
}
