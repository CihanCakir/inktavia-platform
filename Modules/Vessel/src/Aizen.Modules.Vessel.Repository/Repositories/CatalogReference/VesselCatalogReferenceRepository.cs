using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories.CatalogReference;

/// <summary>Reads/repoints the soft catalog FK columns on vessel_specifications + vessel_engines.</summary>
public sealed class VesselCatalogReferenceRepository : IVesselCatalogReferenceRepository
{
    private readonly VesselDbContext _db;
    public VesselCatalogReferenceRepository(VesselDbContext db) => _db = db;

    public async Task<CatalogReferenceCountsDto> GetReferenceCountsAsync(CancellationToken cancellationToken = default)
    {
        var vBrand = await _db.VesselSpecifications.AsNoTracking()
            .Where(s => s.VesselBrandId != null)
            .GroupBy(s => s.VesselBrandId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var vModel = await _db.VesselSpecifications.AsNoTracking()
            .Where(s => s.VesselModelId != null)
            .GroupBy(s => s.VesselModelId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var eBrand = await _db.VesselEngines.AsNoTracking()
            .Where(e => e.EngineBrandId != null)
            .GroupBy(e => e.EngineBrandId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var eModel = await _db.VesselEngines.AsNoTracking()
            .Where(e => e.EngineModelId != null)
            .GroupBy(e => e.EngineModelId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new CatalogReferenceCountsDto
        {
            VesselBrand = vBrand.ToDictionary(x => x.Id, x => x.Count),
            VesselModel = vModel.ToDictionary(x => x.Id, x => x.Count),
            EngineBrand = eBrand.ToDictionary(x => x.Id, x => x.Count),
            EngineModel = eModel.ToDictionary(x => x.Id, x => x.Count),
        };
    }

    public async Task<int> RepointAsync(CatalogRefKind kind, long sourceId, long targetId, CancellationToken cancellationToken = default)
    {
        // No-op when source == target (nothing to move) — keeps re-merge safe.
        if (sourceId == targetId) return 0;

        int moved = 0;
        switch (kind)
        {
            case CatalogRefKind.VesselBrand:
            {
                var rows = await _db.VesselSpecifications.Where(s => s.VesselBrandId == sourceId).ToListAsync(cancellationToken);
                foreach (var s in rows) s.SetBrandModel(targetId, s.VesselModelId);
                moved = rows.Count;
                break;
            }
            case CatalogRefKind.VesselModel:
            {
                var rows = await _db.VesselSpecifications.Where(s => s.VesselModelId == sourceId).ToListAsync(cancellationToken);
                foreach (var s in rows) s.SetBrandModel(s.VesselBrandId, targetId);
                moved = rows.Count;
                break;
            }
            case CatalogRefKind.EngineBrand:
            {
                var rows = await _db.VesselEngines.Where(e => e.EngineBrandId == sourceId).ToListAsync(cancellationToken);
                foreach (var e in rows) e.SetBrandModel(targetId, e.EngineModelId);
                moved = rows.Count;
                break;
            }
            case CatalogRefKind.EngineModel:
            {
                var rows = await _db.VesselEngines.Where(e => e.EngineModelId == sourceId).ToListAsync(cancellationToken);
                foreach (var e in rows) e.SetBrandModel(e.EngineBrandId, targetId);
                moved = rows.Count;
                break;
            }
        }

        if (moved > 0)
            await _db.SaveChangesAsync(cancellationToken); // single SaveChanges = one transaction

        return moved;
    }
}
