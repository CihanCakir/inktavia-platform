using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
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

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetAdminListAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default)
    {
        var skip = filter.PageIndex * filter.PageSize;
        return await BuildAdminQuery(filter)
            .Include(x => x.Assignment)
            .OrderByDescending(x => x.ModifyDate ?? x.CreateDate)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAdminAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default)
        => BuildAdminQuery(filter).CountAsync(ct);

    public Task AddAsync(ServiceRequestEntity entity, CancellationToken ct = default)
        => _db.ServiceRequests.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestEntity entity) => _db.ServiceRequests.Update(entity);

    private IQueryable<ServiceRequestEntity> BuildAdminQuery(AdminServiceRequestFilterRequest filter)
    {
        var query = _db.ServiceRequests.AsNoTracking().Where(x => !x.IsDeleted);

        if (filter.VesselId.HasValue)
            query = query.Where(x => x.VesselId == filter.VesselId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(x => x.OwnerUserId == filter.OwnerUserId.Value);

        if (filter.ProviderProfileId.HasValue)
            query = query.Where(x => x.Assignment != null && x.Assignment.ProviderProfileId == filter.ProviderProfileId.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(x => x.Priority == filter.Priority.Value);

        if (filter.HasDispute.HasValue)
            query = query.Where(x => filter.HasDispute.Value ? x.Dispute != null : x.Dispute == null);

        if (!string.IsNullOrWhiteSpace(filter.ServiceCategoryCode))
            query = query.Where(x => x.ServiceCategoryCode == filter.ServiceCategoryCode.ToUpperInvariant());

        if (filter.CreatedFrom.HasValue)
            query = query.Where(x => x.CreateDate >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query = query.Where(x => x.CreateDate <= filter.CreatedTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.RequestCode.ToLower().Contains(term));
        }

        return query;
    }
}
