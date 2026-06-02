using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel document repository", "EF Core implementation of IVesselDocumentRepository.")]
public sealed class VesselDocumentRepository : IVesselDocumentRepository
{
    private readonly VesselDbContext _db;

    public VesselDocumentRepository(VesselDbContext db) => _db = db;

    public Task<VesselDocumentEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.VesselDocuments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<VesselDocumentEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.VesselDocuments.AsNoTracking().Where(x => x.VesselId == vesselId).ToListAsync(ct);

    public async Task<IReadOnlyList<VesselDocumentEntity>> GetExpiringAsync(int daysAhead, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(daysAhead);
        return await _db.VesselDocuments.AsNoTracking()
            .Where(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value <= threshold && x.IsActive)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(ct);
    }

    public Task AddAsync(VesselDocumentEntity entity, CancellationToken ct = default)
        => _db.VesselDocuments.AddAsync(entity, ct).AsTask();

    public void Update(VesselDocumentEntity entity) => _db.VesselDocuments.Update(entity);
}
