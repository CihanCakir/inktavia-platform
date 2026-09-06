using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest trip repository", "EF Core implementation of IServiceRequestTripRepository. Positions are hard-purged on arrive/cancel (privacy).")]
public sealed class ServiceRequestTripRepository : IServiceRequestTripRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestTripRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestTripEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.Set<ServiceRequestTripEntity>()
            .FirstOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted, ct);

    public Task AddAsync(ServiceRequestTripEntity entity, CancellationToken ct = default)
        => _db.Set<ServiceRequestTripEntity>().AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestTripEntity entity) => _db.Set<ServiceRequestTripEntity>().Update(entity);

    public Task AddPositionAsync(ServiceRequestTripPositionEntity position, CancellationToken ct = default)
        => _db.Set<ServiceRequestTripPositionEntity>().AddAsync(position, ct).AsTask();

    public async Task<IReadOnlyList<ServiceRequestTripPositionEntity>> GetPositionsAsync(long tripId, CancellationToken ct = default)
        => await _db.Set<ServiceRequestTripPositionEntity>()
            .AsNoTracking()
            .Where(x => x.TripId == tripId && !x.IsDeleted)
            .OrderBy(x => x.PingAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceRequestTripPositionEntity>> GetRecentPositionsAsync(long tripId, int take, CancellationToken ct = default)
        => await _db.Set<ServiceRequestTripPositionEntity>()
            .AsNoTracking()
            .Where(x => x.TripId == tripId && !x.IsDeleted)
            .OrderByDescending(x => x.PingAt)
            .Take(take)
            .ToListAsync(ct);

    // Hard delete — the raw trail must not survive completion (privacy by default). ExecuteDelete bypasses soft-delete.
    public Task PurgePositionsAsync(long tripId, CancellationToken ct = default)
        => _db.Set<ServiceRequestTripPositionEntity>().Where(x => x.TripId == tripId).ExecuteDeleteAsync(ct);
}
