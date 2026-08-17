using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest change order repository", "EF Core implementation of IServiceChangeOrderRepository.")]
public sealed class ServiceChangeOrderRepository : IServiceChangeOrderRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceChangeOrderRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceChangeOrderEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceChangeOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceChangeOrderEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => await _db.ServiceChangeOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(ct);

    public Task<int> CountForOfferAsync(long acceptedOfferId, CancellationToken ct = default)
        => _db.ServiceChangeOrders.CountAsync(x => x.AcceptedOfferId == acceptedOfferId && !x.IsDeleted, ct);

    public Task AddAsync(ServiceChangeOrderEntity entity, CancellationToken ct = default)
        => _db.ServiceChangeOrders.AddAsync(entity, ct).AsTask();

    public void Update(ServiceChangeOrderEntity entity) => _db.ServiceChangeOrders.Update(entity);
}
