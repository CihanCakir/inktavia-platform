using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel repository", "EF Core implementation of IVesselRepository.")]
public sealed class VesselRepository : IVesselRepository
{
    private readonly VesselDbContext _db;

    public VesselRepository(VesselDbContext db) => _db = db;

    public Task<VesselEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.Vessels.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<VesselEntity?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
        => _db.Vessels
            .Include(x => x.Owners)
            .Include(x => x.Specification)
            .Include(x => x.Engines)
            .Include(x => x.Documents)
            .Include(x => x.Media)
            .Include(x => x.LocationSnapshots)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<VesselEntity?> GetByCodeAsync(string vesselCode, CancellationToken ct = default)
        => _db.Vessels.FirstOrDefaultAsync(x => x.VesselCode == vesselCode && !x.IsDeleted, ct);

    public Task<VesselEntity?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Vessels.FirstOrDefaultAsync(x => x.Slug == slug && !x.IsDeleted, ct);

    public Task<bool> ExistsByCodeAsync(string vesselCode, CancellationToken ct = default)
        => _db.Vessels.AnyAsync(x => x.VesselCode == vesselCode && !x.IsDeleted, ct);

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Vessels.AnyAsync(x => x.Slug == slug && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<VesselEntity>> GetByOwnerUserIdAsync(long userId, CancellationToken ct = default)
        => await _db.Vessels
            .AsNoTracking()
            .Where(v => !v.IsDeleted && v.Owners.Any(o => o.UserId == userId && o.IsActive))
            .OrderBy(v => v.Name)
            .ToListAsync(ct);

    public Task AddAsync(VesselEntity entity, CancellationToken ct = default)
        => _db.Vessels.AddAsync(entity, ct).AsTask();

    public void Update(VesselEntity entity) => _db.Vessels.Update(entity);
}
