using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest offer repository", "EF Core implementation of IServiceRequestOfferRepository.")]
public sealed class ServiceRequestOfferRepository : IServiceRequestOfferRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestOfferRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestOfferEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequestOffers
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestOfferEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => await _db.ServiceRequestOffers
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceRequestOfferEntity>> GetByProviderProfileIdAsync(long providerProfileId, int skip, int take, CancellationToken ct = default)
        => await _db.ServiceRequestOffers
            .AsNoTracking()
            .Where(x => x.ProviderProfileId == providerProfileId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceRequestOfferEntity>> GetByProviderProfileIdWithSrAsync(
        long providerProfileId, ServiceRequestOfferStatus? statusFilter, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.ServiceRequestOffers
            .AsNoTracking()
            .Where(x => x.ProviderProfileId == providerProfileId && !x.IsDeleted);

        if (statusFilter.HasValue)
            query = query.Where(x => x.Status == statusFilter.Value);

        return await query
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task AddAsync(ServiceRequestOfferEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestOffers.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestOfferEntity entity) => _db.ServiceRequestOffers.Update(entity);
}
