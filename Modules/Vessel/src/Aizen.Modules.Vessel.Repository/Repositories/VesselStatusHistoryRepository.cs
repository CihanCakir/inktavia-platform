using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel status history repository", "EF Core implementation of IVesselStatusHistoryRepository.")]
public sealed class VesselStatusHistoryRepository : IVesselStatusHistoryRepository
{
    private readonly VesselDbContext _db;

    public VesselStatusHistoryRepository(VesselDbContext db) => _db = db;

    public async Task<IReadOnlyList<VesselStatusHistoryEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.VesselStatusHistories.AsNoTracking()
            .Where(x => x.VesselId == vesselId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync(ct);

    public Task AddAsync(VesselStatusHistoryEntity entity, CancellationToken ct = default)
        => _db.VesselStatusHistories.AddAsync(entity, ct).AsTask();
}
