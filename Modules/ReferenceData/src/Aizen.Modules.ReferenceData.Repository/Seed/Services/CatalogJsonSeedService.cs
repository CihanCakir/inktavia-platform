using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Catalog;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>
/// Seeds the vessel/engine brand+model catalog from JSON. Per-key idempotent upsert (brands by Code; models by
/// BrandId+Code) — deliberately NO whole-table AnyAsync short-circuit, so newly-added rows seed on an already-populated
/// DB. Each row carries its own NeedsReview + Source (manufacturer URL); Source falls back to "seed" when absent.
/// Brands are seeded before models (models resolve BrandCode→id).
/// </summary>
public sealed class CatalogJsonSeedService
{
    private readonly IVesselBrandRepository _vBrands;
    private readonly IVesselModelRepository _vModels;
    private readonly IEngineBrandRepository _eBrands;
    private readonly IEngineModelRepository _eModels;
    private readonly ReferenceDataDbContext _db;
    private readonly IReferenceDataJsonSeedReader _reader;

    public CatalogJsonSeedService(
        IVesselBrandRepository vBrands, IVesselModelRepository vModels,
        IEngineBrandRepository eBrands, IEngineModelRepository eModels,
        ReferenceDataDbContext db, IReferenceDataJsonSeedReader reader)
    {
        _vBrands = vBrands; _vModels = vModels; _eBrands = eBrands; _eModels = eModels; _db = db; _reader = reader;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedVesselBrandsAsync(ct);
        await SeedVesselModelsAsync(ct);
        await SeedEngineBrandsAsync(ct);
        await SeedEngineModelsAsync(ct);
    }

    private async Task SeedVesselBrandsAsync(CancellationToken ct)
    {
        var models = await _reader.ReadListAsync<VesselBrandSeedModel>("Catalog/vessel-brands.json", optional: true, cancellationToken: ct);
        foreach (var m in models)
        {
            if (await _vBrands.GetByCodeAsync(m.Code, ct) is not null) continue;
            var e = VesselBrandEntity.Create(m.Code, m.Name, m.CountryCode, needsReview: m.NeedsReview, source: string.IsNullOrWhiteSpace(m.Source) ? "seed" : m.Source);
            if (!m.IsActive) e.Deactivate();
            await _vBrands.AddAsync(e, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedVesselModelsAsync(CancellationToken ct)
    {
        var models = await _reader.ReadListAsync<VesselModelSeedModel>("Catalog/vessel-models.json", optional: true, cancellationToken: ct);
        foreach (var m in models)
        {
            var brand = await _vBrands.GetByCodeAsync(m.BrandCode, ct);
            if (brand is null) continue; // brand missing → skip (data kickoff will fix)
            if (await _vModels.GetByCodeAsync(brand.Id, m.Code, ct) is not null) continue;
            var e = VesselModelEntity.Create(brand.Id, m.Code, m.Name, m.VesselTypeCode, m.YearFrom, m.YearTo, m.LengthMeters, needsReview: m.NeedsReview, source: string.IsNullOrWhiteSpace(m.Source) ? "seed" : m.Source);
            if (!m.IsActive) e.Deactivate();
            await _vModels.AddAsync(e, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedEngineBrandsAsync(CancellationToken ct)
    {
        var models = await _reader.ReadListAsync<EngineBrandSeedModel>("Catalog/engine-brands.json", optional: true, cancellationToken: ct);
        foreach (var m in models)
        {
            if (await _eBrands.GetByCodeAsync(m.Code, ct) is not null) continue;
            var e = EngineBrandEntity.Create(m.Code, m.Name, needsReview: m.NeedsReview, source: string.IsNullOrWhiteSpace(m.Source) ? "seed" : m.Source);
            if (!m.IsActive) e.Deactivate();
            await _eBrands.AddAsync(e, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedEngineModelsAsync(CancellationToken ct)
    {
        var models = await _reader.ReadListAsync<EngineModelSeedModel>("Catalog/engine-models.json", optional: true, cancellationToken: ct);
        foreach (var m in models)
        {
            var brand = await _eBrands.GetByCodeAsync(m.BrandCode, ct);
            if (brand is null) continue;
            if (await _eModels.GetByCodeAsync(brand.Id, m.Code, ct) is not null) continue;
            var e = EngineModelEntity.Create(brand.Id, m.Code, m.Name, m.HorsePower, m.FuelTypeCode, m.EngineTypeCode, m.YearFrom, m.YearTo, needsReview: m.NeedsReview, source: string.IsNullOrWhiteSpace(m.Source) ? "seed" : m.Source);
            if (!m.IsActive) e.Deactivate();
            await _eModels.AddAsync(e, ct);
        }
        await _db.SaveChangesAsync(ct);
    }
}
