using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.Catalog;

public sealed class VesselBrandRepository : IVesselBrandRepository
{
    private readonly ReferenceDataDbContext _db;
    public VesselBrandRepository(ReferenceDataDbContext db) => _db = db;

    public Task<VesselBrandEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.VesselBrands.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<VesselBrandEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.VesselBrands.FirstOrDefaultAsync(x => x.Code == c, ct);
    }

    public Task<VesselBrandEntity?> GetByNameKeyAsync(string nameKey, CancellationToken ct = default)
        => _db.VesselBrands.FirstOrDefaultAsync(x => x.Name.ToUpper() == nameKey, ct);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.VesselBrands.AnyAsync(x => x.Code == c, ct);
    }

    public async Task<IReadOnlyList<VesselBrandEntity>> SearchAsync(string? search, bool onlyActive, int take, CancellationToken ct = default)
    {
        var q = _db.VesselBrands.AsNoTracking().AsQueryable();
        if (onlyActive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Name.ToUpper().Contains(s));
        }
        return await q.OrderBy(x => x.Name).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<VesselBrandEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default)
        => await _db.VesselBrands.AsNoTracking().Where(x => x.NeedsReview)
            .OrderBy(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);

    public Task AddAsync(VesselBrandEntity entity, CancellationToken ct = default) => _db.VesselBrands.AddAsync(entity, ct).AsTask();
    public void Update(VesselBrandEntity entity) => _db.VesselBrands.Update(entity);
}

public sealed class VesselModelRepository : IVesselModelRepository
{
    private readonly ReferenceDataDbContext _db;
    public VesselModelRepository(ReferenceDataDbContext db) => _db = db;

    public Task<VesselModelEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.VesselModels.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<VesselModelEntity?> GetByCodeAsync(long brandId, string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.VesselModels.FirstOrDefaultAsync(x => x.VesselBrandId == brandId && x.Code == c, ct);
    }

    public Task<VesselModelEntity?> GetByNameKeyAsync(long brandId, string nameKey, CancellationToken ct = default)
        => _db.VesselModels.FirstOrDefaultAsync(x => x.VesselBrandId == brandId && x.Name.ToUpper() == nameKey, ct);

    public async Task<IReadOnlyList<VesselModelEntity>> SearchAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default)
    {
        var q = _db.VesselModels.AsNoTracking().Where(x => x.VesselBrandId == brandId);
        if (onlyActive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(typeCode))
        {
            var t = typeCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.VesselTypeCode == t);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Name.ToUpper().Contains(s));
        }
        return await q.OrderBy(x => x.Name).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<VesselModelEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default)
        => await _db.VesselModels.AsNoTracking().Where(x => x.NeedsReview)
            .OrderBy(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);

    public Task AddAsync(VesselModelEntity entity, CancellationToken ct = default) => _db.VesselModels.AddAsync(entity, ct).AsTask();
    public void Update(VesselModelEntity entity) => _db.VesselModels.Update(entity);
}

public sealed class EngineBrandRepository : IEngineBrandRepository
{
    private readonly ReferenceDataDbContext _db;
    public EngineBrandRepository(ReferenceDataDbContext db) => _db = db;

    public Task<EngineBrandEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.EngineBrands.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<EngineBrandEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.EngineBrands.FirstOrDefaultAsync(x => x.Code == c, ct);
    }

    public Task<EngineBrandEntity?> GetByNameKeyAsync(string nameKey, CancellationToken ct = default)
        => _db.EngineBrands.FirstOrDefaultAsync(x => x.Name.ToUpper() == nameKey, ct);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.EngineBrands.AnyAsync(x => x.Code == c, ct);
    }

    public async Task<IReadOnlyList<EngineBrandEntity>> SearchAsync(string? search, bool onlyActive, int take, CancellationToken ct = default)
    {
        var q = _db.EngineBrands.AsNoTracking().AsQueryable();
        if (onlyActive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Name.ToUpper().Contains(s));
        }
        return await q.OrderBy(x => x.Name).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EngineBrandEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default)
        => await _db.EngineBrands.AsNoTracking().Where(x => x.NeedsReview)
            .OrderBy(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);

    public Task AddAsync(EngineBrandEntity entity, CancellationToken ct = default) => _db.EngineBrands.AddAsync(entity, ct).AsTask();
    public void Update(EngineBrandEntity entity) => _db.EngineBrands.Update(entity);
}

public sealed class EngineModelRepository : IEngineModelRepository
{
    private readonly ReferenceDataDbContext _db;
    public EngineModelRepository(ReferenceDataDbContext db) => _db = db;

    public Task<EngineModelEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.EngineModels.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<EngineModelEntity?> GetByCodeAsync(long brandId, string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.EngineModels.FirstOrDefaultAsync(x => x.EngineBrandId == brandId && x.Code == c, ct);
    }

    public Task<EngineModelEntity?> GetByNameKeyAsync(long brandId, string nameKey, CancellationToken ct = default)
        => _db.EngineModels.FirstOrDefaultAsync(x => x.EngineBrandId == brandId && x.Name.ToUpper() == nameKey, ct);

    public async Task<IReadOnlyList<EngineModelEntity>> SearchAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default)
    {
        var q = _db.EngineModels.AsNoTracking().Where(x => x.EngineBrandId == brandId);
        if (onlyActive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(typeCode))
        {
            var t = typeCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.EngineTypeCode == t);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Name.ToUpper().Contains(s));
        }
        return await q.OrderBy(x => x.Name).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EngineModelEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default)
        => await _db.EngineModels.AsNoTracking().Where(x => x.NeedsReview)
            .OrderBy(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);

    public Task AddAsync(EngineModelEntity entity, CancellationToken ct = default) => _db.EngineModels.AddAsync(entity, ct).AsTask();
    public void Update(EngineModelEntity entity) => _db.EngineModels.Update(entity);
}
