using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel media repository", "EF Core implementation of IVesselMediaRepository.")]
public sealed class VesselMediaRepository : IVesselMediaRepository
{
    private readonly VesselDbContext _db;

    public VesselMediaRepository(VesselDbContext db) => _db = db;

    public Task<VesselMediaEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.VesselMedia.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<VesselMediaEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.VesselMedia.AsNoTracking().Where(x => x.VesselId == vesselId).OrderBy(x => x.SortOrder).ToListAsync(ct);

    public Task<VesselMediaEntity?> GetCoverAsync(long vesselId, CancellationToken ct = default)
        => _db.VesselMedia.FirstOrDefaultAsync(x => x.VesselId == vesselId && x.IsCover && x.IsActive, ct);

    public Task AddAsync(VesselMediaEntity entity, CancellationToken ct = default)
        => _db.VesselMedia.AddAsync(entity, ct).AsTask();

    public void Update(VesselMediaEntity entity) => _db.VesselMedia.Update(entity);
}
