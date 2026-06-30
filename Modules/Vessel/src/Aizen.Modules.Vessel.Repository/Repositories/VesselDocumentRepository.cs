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

    public Task<VesselDocumentEntity?> GetByIdWithVesselAsync(long id, CancellationToken ct = default)
        => _db.VesselDocuments
            .Include(x => x.Vessel)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<VesselDocumentEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.VesselDocuments.AsNoTracking().Where(x => x.VesselId == vesselId).ToListAsync(ct);

    public async Task<IReadOnlyList<VesselDocumentEntity>> GetByVesselIdAsync(long vesselId, bool onlyActive, CancellationToken ct = default)
    {
        var query = _db.VesselDocuments.AsNoTracking().Where(x => x.VesselId == vesselId);
        if (onlyActive)
            query = query.Where(x => x.IsActive);
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<VesselDocumentEntity>> GetExpiringAsync(int daysAhead, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(daysAhead);
        return await _db.VesselDocuments.AsNoTracking()
            .Where(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value <= threshold && x.IsActive)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsActiveDocumentTypeAsync(long vesselId, string documentTypeCode, CancellationToken ct = default)
        => _db.VesselDocuments.AnyAsync(
            x => x.VesselId == vesselId &&
                 x.DocumentTypeCode == documentTypeCode.ToUpperInvariant() &&
                 x.IsActive, ct);

    public Task AddAsync(VesselDocumentEntity entity, CancellationToken ct = default)
        => _db.VesselDocuments.AddAsync(entity, ct).AsTask();

    public void Update(VesselDocumentEntity entity) => _db.VesselDocuments.Update(entity);
}

