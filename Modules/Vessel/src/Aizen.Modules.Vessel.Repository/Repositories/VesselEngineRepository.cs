using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel engine repository", "EF Core implementation of IVesselEngineRepository.")]
public sealed class VesselEngineRepository : IVesselEngineRepository
{
    private readonly VesselDbContext _db;

    public VesselEngineRepository(VesselDbContext db) => _db = db;

    public Task<VesselEngineEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.VesselEngines.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<VesselEngineEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.VesselEngines.AsNoTracking().Where(x => x.VesselId == vesselId).ToListAsync(ct);

    public Task<VesselEngineEntity?> GetPrimaryEngineAsync(long vesselId, CancellationToken ct = default)
        => _db.VesselEngines.FirstOrDefaultAsync(x => x.VesselId == vesselId && x.IsPrimary, ct);

    public Task AddAsync(VesselEngineEntity entity, CancellationToken ct = default)
        => _db.VesselEngines.AddAsync(entity, ct).AsTask();

    public void Update(VesselEngineEntity entity) => _db.VesselEngines.Update(entity);
}
