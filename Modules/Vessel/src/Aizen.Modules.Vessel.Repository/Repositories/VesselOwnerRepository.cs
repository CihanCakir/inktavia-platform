using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel owner repository", "EF Core implementation of IVesselOwnerRepository.")]
public sealed class VesselOwnerRepository : IVesselOwnerRepository
{
    private readonly VesselDbContext _db;

    public VesselOwnerRepository(VesselDbContext db) => _db = db;

    public Task<VesselOwnerEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.VesselOwners.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<VesselOwnerEntity?> GetByVesselAndUserAsync(long vesselId, long userId, CancellationToken ct = default)
        => _db.VesselOwners.FirstOrDefaultAsync(x => x.VesselId == vesselId && x.UserId == userId, ct);

    public async Task<IReadOnlyList<VesselOwnerEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.VesselOwners.AsNoTracking().Where(x => x.VesselId == vesselId).ToListAsync(ct);

    public Task<VesselOwnerEntity?> GetPrimaryOwnerAsync(long vesselId, CancellationToken ct = default)
        => _db.VesselOwners.FirstOrDefaultAsync(x => x.VesselId == vesselId && x.IsPrimary, ct);

    public Task<bool> UserHasRoleAsync(long vesselId, long userId, VesselOwnershipRole role, CancellationToken ct = default)
        => _db.VesselOwners.AnyAsync(x => x.VesselId == vesselId && x.UserId == userId && x.Role == role, ct);

    public Task AddAsync(VesselOwnerEntity entity, CancellationToken ct = default)
        => _db.VesselOwners.AddAsync(entity, ct).AsTask();

    public void Update(VesselOwnerEntity entity) => _db.VesselOwners.Update(entity);
}
