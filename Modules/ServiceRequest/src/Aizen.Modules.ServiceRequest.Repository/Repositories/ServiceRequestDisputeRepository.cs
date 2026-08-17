using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest dispute repository", "EF Core implementation of IServiceRequestDisputeRepository.")]
public sealed class ServiceRequestDisputeRepository : IServiceRequestDisputeRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestDisputeRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestDisputeEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequestDisputes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestDisputeEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.ServiceRequestDisputes.FirstOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted, ct);

    // Back-compat wrapper — the Open-only list is just the all-status list narrowed to Open.
    public async Task<IReadOnlyList<ServiceRequestDisputeEntity>> GetAllOpenAsync(int skip, int take, CancellationToken ct = default)
        => (await GetAllAsync(ServiceRequestDisputeStatus.Open, skip, take, ct)).Items;

    public async Task<(IReadOnlyList<ServiceRequestDisputeEntity> Items, int Total)> GetAllAsync(
        ServiceRequestDisputeStatus? status, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.ServiceRequestDisputes
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.OpenedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task AddAsync(ServiceRequestDisputeEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestDisputes.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestDisputeEntity entity) => _db.ServiceRequestDisputes.Update(entity);
}
