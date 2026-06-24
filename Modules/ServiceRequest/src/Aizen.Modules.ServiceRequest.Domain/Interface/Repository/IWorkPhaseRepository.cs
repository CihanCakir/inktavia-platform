using Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

public interface IWorkPhaseRepository
{
    Task<IReadOnlyList<WorkPhaseEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task AddAsync(WorkPhaseEntity entity, CancellationToken ct = default);
    void Update(WorkPhaseEntity entity);
}
