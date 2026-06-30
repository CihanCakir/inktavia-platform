using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel location snapshot repository", "EF Core implementation of IVesselLocationSnapshotRepository.")]
public sealed class VesselLocationSnapshotRepository : IVesselLocationSnapshotRepository
{
    private readonly VesselDbContext _db;

    public VesselLocationSnapshotRepository(VesselDbContext db) => _db = db;

    public Task<VesselLocationSnapshotEntity?> GetCurrentAsync(long vesselId, CancellationToken ct = default)
        => _db.VesselLocationSnapshots.FirstOrDefaultAsync(x => x.VesselId == vesselId && x.IsCurrent, ct);

    public async Task<IReadOnlyList<VesselLocationSnapshotEntity>> GetHistoryAsync(long vesselId, int limit, CancellationToken ct = default)
        => await _db.VesselLocationSnapshots.AsNoTracking()
            .Where(x => x.VesselId == vesselId)
            .OrderByDescending(x => x.CapturedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task AddAsync(VesselLocationSnapshotEntity entity, CancellationToken ct = default)
        => _db.VesselLocationSnapshots.AddAsync(entity, ct).AsTask();

    public void Update(VesselLocationSnapshotEntity entity) => _db.VesselLocationSnapshots.Update(entity);
}
