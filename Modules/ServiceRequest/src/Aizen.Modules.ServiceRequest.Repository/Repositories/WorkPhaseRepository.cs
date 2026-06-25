using Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

public sealed class WorkPhaseRepository : IWorkPhaseRepository
{
    private readonly ServiceRequestDbContext _db;
    public WorkPhaseRepository(ServiceRequestDbContext db) => _db = db;

    public async Task<IReadOnlyList<WorkPhaseEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => await _db.WorkPhases
            .AsNoTracking()
            .Where(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);

    public async Task<WorkPhaseEntity?> GetByPhaseNumberAsync(long serviceRequestId, int phaseNumber, CancellationToken ct = default)
        => await _db.WorkPhases
            .Where(x => x.ServiceRequestId == serviceRequestId && x.PhaseNumber == phaseNumber && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(WorkPhaseEntity entity, CancellationToken ct = default)
        => _db.WorkPhases.AddAsync(entity, ct).AsTask();

    public void Update(WorkPhaseEntity entity) => _db.WorkPhases.Update(entity);
}
