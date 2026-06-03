using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request dispute repository interface", "Data access contract for ServiceRequestDispute entities.")]
public interface IServiceRequestDisputeRepository
{
    Task<ServiceRequestDisputeEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestDisputeEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestDisputeEntity>> GetAllOpenAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(ServiceRequestDisputeEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestDisputeEntity entity);
}
