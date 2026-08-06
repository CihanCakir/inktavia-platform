using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest completion repository", "EF Core implementation of IServiceRequestCompletionRepository.")]
public sealed class ServiceRequestCompletionRepository : IServiceRequestCompletionRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestCompletionRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestCompletionEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.ServiceRequestCompletions
            .FirstOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted, ct);

    public Task AddAsync(ServiceRequestCompletionEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestCompletions.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestCompletionEntity entity) => _db.ServiceRequestCompletions.Update(entity);

    public async Task<IReadOnlyList<ServiceRequestCompletionEntity>> GetPendingAutoApproveCandidatesAsync(
        DateTime cutoffUtc, int maxBatch, CancellationToken ct = default)
        => await _db.ServiceRequestCompletions
            .Where(x => !x.IsDeleted
                     && x.Status == ServiceRequestCompletionStatus.Submitted
                     && x.AutoApproveAt != null
                     && x.AutoApproveAt <= cutoffUtc)
            .OrderBy(x => x.AutoApproveAt)
            .Take(maxBatch)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
