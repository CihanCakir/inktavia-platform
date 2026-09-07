using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Catalog;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Microsoft.EntityFrameworkCore;

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

    // Legacy coarse VesselTypeCode → real ReferenceData VESSEL_TYPE lookup code. Early seed rows stored the coarse
    // guess codes (SAILBOAT/MOTORYACHT/PWC/SUPERYACHT), which the models query — filtering VesselTypeCode by exact
    // equality against the lookup code the picker sends — never matched, so the mobile model list came back empty.
    // The generator now emits the real codes; this reconciles rows already seeded with the legacy values.
    private static readonly (string Legacy, string Real)[] VesselTypeCodeReconcile =
    {
        ("MOTORYACHT", "MOTOR_YACHT"),
        ("SUPERYACHT", "MOTOR_YACHT"),
        ("SAILBOAT", "SAILING_BOAT"),
        ("PWC", "JET_SKI"),
    };

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedVesselBrandsAsync(ct);
        await SeedVesselModelsAsync(ct);
        await SeedEngineBrandsAsync(ct);
        await SeedEngineModelsAsync(ct);
        await ReconcileVesselTypeCodesAsync(ct);
    }

    /// <summary>
    /// Idempotent: rewrites any VesselModel still holding a legacy coarse VesselTypeCode to the real VESSEL_TYPE
    /// lookup code. A no-op once every row is reconciled (and on fresh DBs, where the seeder already wrote real codes).
    /// Only touches rows carrying a known legacy literal, so admin-corrected codes are never clobbered.
    /// </summary>
    private async Task ReconcileVesselTypeCodesAsync(CancellationToken ct)
    {
        // ExecuteUpdate is a relational-only operation (the in-memory provider used by tests doesn't support it).
        // Fresh/test DBs seed the real codes directly, so skipping the reconcile there changes nothing.
        if (!_db.Database.IsRelational()) return;

        foreach (var (legacy, real) in VesselTypeCodeReconcile)
        {
            await _db.VesselModels
                .Where(m => m.VesselTypeCode == legacy)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.VesselTypeCode, real), ct);
        }
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
