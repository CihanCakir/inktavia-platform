using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request completion repository interface", "Data access contract for ServiceRequestCompletion entities.")]
public interface IServiceRequestCompletionRepository
{
    Task<ServiceRequestCompletionEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task AddAsync(ServiceRequestCompletionEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestCompletionEntity entity);
}
