using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request dispute repository interface", "Data access contract for ServiceRequestDispute entities.")]
public interface IServiceRequestDisputeRepository
{
    Task<ServiceRequestDisputeEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestDisputeEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestDisputeEntity>> GetAllOpenAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>
    /// Paginated disputes of <b>all</b> statuses, newest-opened first, optionally narrowed to a single
    /// <paramref name="status"/> (null = every status). Returns the page + the filtered total count.
    /// </summary>
    Task<(IReadOnlyList<ServiceRequestDisputeEntity> Items, int Total)> GetAllAsync(
        ServiceRequestDisputeStatus? status, int skip, int take, CancellationToken ct = default);
    Task AddAsync(ServiceRequestDisputeEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestDisputeEntity entity);
}
