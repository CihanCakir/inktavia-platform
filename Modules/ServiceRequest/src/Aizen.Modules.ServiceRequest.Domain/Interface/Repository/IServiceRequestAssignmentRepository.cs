using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request assignment repository interface", "Data access contract for ServiceRequestAssignment entities.")]
public interface IServiceRequestAssignmentRepository
{
    Task<ServiceRequestAssignmentEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestAssignmentEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestAssignmentEntity>> GetByProviderProfileIdAsync(long providerProfileId, int skip, int take, CancellationToken ct = default);
    Task AddAsync(ServiceRequestAssignmentEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestAssignmentEntity entity);
}
