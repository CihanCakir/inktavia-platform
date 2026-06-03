using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest repository", "EF Core implementation of IServiceRequestRepository.")]
public sealed class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestEntity?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequests
            .Include(x => x.Items)
            .Include(x => x.StatusHistory)
            .Include(x => x.Attachments)
            .Include(x => x.Messages)
            .Include(x => x.Offers).ThenInclude(o => o.Items)
            .Include(x => x.Assignment).ThenInclude(a => a!.WorkLogs)
            .Include(x => x.Completion)
            .Include(x => x.Dispute)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestEntity?> GetByCodeAsync(string requestCode, CancellationToken ct = default)
        => _db.ServiceRequests.FirstOrDefaultAsync(x => x.RequestCode == requestCode && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetByOwnerUserIdAsync(long ownerUserId, int skip, int take, CancellationToken ct = default)
        => await _db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountByOwnerUserIdAsync(long ownerUserId, CancellationToken ct = default)
        => _db.ServiceRequests.CountAsync(x => x.OwnerUserId == ownerUserId && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.VesselId == vesselId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(ct);

    public Task AddAsync(ServiceRequestEntity entity, CancellationToken ct = default)
        => _db.ServiceRequests.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestEntity entity) => _db.ServiceRequests.Update(entity);
}
