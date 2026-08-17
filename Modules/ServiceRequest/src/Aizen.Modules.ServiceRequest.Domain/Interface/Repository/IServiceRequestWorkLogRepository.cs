using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request work log repository interface", "Data access contract for ServiceRequestWorkLog entities.")]
public interface IServiceRequestWorkLogRepository
{
    Task<IReadOnlyList<ServiceRequestWorkLogEntity>> GetByAssignmentIdAsync(long assignmentId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestWorkLogEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task AddAsync(ServiceRequestWorkLogEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestWorkLogEntity entity);
}
