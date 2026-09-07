using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class VesselCatalogReferenceService : IVesselCatalogReferenceService
{
    private const int MaxTake = 200;
    private readonly IVesselBrandRepository _brands;
    private readonly IVesselModelRepository _models;
    private readonly ReferenceDataDbContext _db;

    public VesselCatalogReferenceService(IVesselBrandRepository brands, IVesselModelRepository models, ReferenceDataDbContext db)
    {
        _brands = brands; _models = models; _db = db;
    }

    public async Task<IReadOnlyList<VesselBrandDto>> SearchBrandsAsync(string? search, bool onlyActive, int take, CancellationToken ct = default)
        => (await _brands.SearchAsync(search, onlyActive, Clamp(take), ct)).Select(ToDto).ToList();

    public async Task<VesselBrandDto?> GetBrandByIdAsync(long id, CancellationToken ct = default)
        => (await _brands.GetByIdAsync(id, ct)) is { } b ? ToDto(b) : null;

    public async Task<IReadOnlyList<VesselModelDto>> GetModelsAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default)
    {
        var brand = await _brands.GetByIdAsync(brandId, ct);
        var models = await _models.SearchAsync(brandId, search, typeCode, onlyActive, Clamp(take), ct);
        return models.Select(m => ToDto(m, brand?.Name)).ToList();
    }

    public async Task<VesselModelDto?> GetModelByIdAsync(long id, CancellationToken ct = default)
    {
        var m = await _models.GetByIdAsync(id, ct);
        if (m is null) return null;
        var brand = await _brands.GetByIdAsync(m.VesselBrandId, ct);
        return ToDto(m, brand?.Name);
    }

    public async Task<VesselBrandDto> SubmitBrandAsync(SubmitVesselBrandRequest req, CancellationToken ct = default)
    {
        var existing = await _brands.GetByNameKeyAsync(CatalogNameNormalizer.Key(req.Name), ct);
        if (existing is not null) return ToDto(existing);

        var entity = VesselBrandEntity.Create(await UniqueBrandCodeAsync(req.Name, ct), req.Name, req.CountryCode, needsReview: true, source: "owner-submission");
        await _brands.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<VesselModelDto> SubmitModelAsync(SubmitVesselModelRequest req, CancellationToken ct = default)
    {
        var brand = await _brands.GetByIdAsync(req.VesselBrandId, ct) ?? throw new AizenBusinessException("Vessel brand not found.");
        var existing = await _models.GetByNameKeyAsync(brand.Id, CatalogNameNormalizer.Key(req.Name), ct);
        if (existing is not null) return ToDto(existing, brand.Name);

        var entity = VesselModelEntity.Create(brand.Id, await UniqueModelCodeAsync(brand.Id, req.Name, ct), req.Name,
            req.VesselTypeCode, req.YearFrom, req.YearTo, req.LengthMeters, needsReview: true, source: "owner-submission");
        await _models.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity, brand.Name);
    }

    public async Task<VesselBrandDto> CreateBrandAsync(CreateVesselBrandRequest req, CancellationToken ct = default)
    {
        var code = string.IsNullOrWhiteSpace(req.Code) ? await UniqueBrandCodeAsync(req.Name, ct) : req.Code!.Trim().ToUpperInvariant();
        if (await _brands.ExistsByCodeAsync(code, ct)) throw new AizenBusinessException($"Vessel brand code '{code}' already exists.");
        var entity = VesselBrandEntity.Create(code, req.Name, req.CountryCode, needsReview: false, source: "admin");
        await _brands.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<VesselBrandDto> UpdateBrandAsync(long id, UpdateVesselBrandRequest req, CancellationToken ct = default)
    {
        var entity = await _brands.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Vessel brand not found.");
        entity.Update(req.Name, req.CountryCode, req.IsActive);
        _brands.Update(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<VesselModelDto> CreateModelAsync(CreateVesselModelRequest req, CancellationToken ct = default)
    {
        var brand = await _brands.GetByIdAsync(req.VesselBrandId, ct) ?? throw new AizenBusinessException("Vessel brand not found.");
        var code = string.IsNullOrWhiteSpace(req.Code) ? await UniqueModelCodeAsync(brand.Id, req.Name, ct) : req.Code!.Trim().ToUpperInvariant();
        if (await _models.GetByCodeAsync(brand.Id, code, ct) is not null) throw new AizenBusinessException($"Vessel model code '{code}' already exists in this brand.");
        var entity = VesselModelEntity.Create(brand.Id, code, req.Name, req.VesselTypeCode, req.YearFrom, req.YearTo, req.LengthMeters, needsReview: false, source: "admin");
        await _models.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity, brand.Name);
    }

    public async Task<VesselModelDto> UpdateModelAsync(long id, UpdateVesselModelRequest req, CancellationToken ct = default)
    {
        var entity = await _models.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Vessel model not found.");
        entity.Update(req.Name, req.VesselTypeCode, req.YearFrom, req.YearTo, req.LengthMeters, req.IsActive);
        _models.Update(entity);
        await _db.SaveChangesAsync(ct);
        var brand = await _brands.GetByIdAsync(entity.VesselBrandId, ct);
        return ToDto(entity, brand?.Name);
    }

    public async Task ApproveBrandAsync(long id, CancellationToken ct = default)
    {
        var e = await _brands.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Vessel brand not found.");
        e.Approve(); _brands.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task ApproveModelAsync(long id, CancellationToken ct = default)
    {
        var e = await _models.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Vessel model not found.");
        e.Approve(); _models.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task SetBrandActiveAsync(long id, bool active, CancellationToken ct = default)
    {
        var e = await _brands.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Vessel brand not found.");
        if (active) e.Activate(); else e.Deactivate();
        _brands.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task SetModelActiveAsync(long id, bool active, CancellationToken ct = default)
    {
        var e = await _models.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Vessel model not found.");
        if (active) e.Activate(); else e.Deactivate();
        _models.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<VesselBrandDto>> ListBrandsForReviewAsync(int skip, int take, CancellationToken ct = default)
        => (await _brands.ListNeedsReviewAsync(Math.Max(0, skip), Clamp(take), ct)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<VesselModelDto>> ListModelsForReviewAsync(int skip, int take, CancellationToken ct = default)
        => (await _models.ListNeedsReviewAsync(Math.Max(0, skip), Clamp(take), ct)).Select(m => ToDto(m, null)).ToList();

    // ── helpers ──
    private static int Clamp(int take) => Math.Clamp(take <= 0 ? 20 : take, 1, MaxTake);

    private async Task<string> UniqueBrandCodeAsync(string name, CancellationToken ct)
    {
        var baseCode = CatalogNameNormalizer.Slug(name);
        var code = baseCode;
        for (var i = 2; await _brands.ExistsByCodeAsync(code, ct); i++) code = $"{baseCode}_{i}";
        return code;
    }

    private async Task<string> UniqueModelCodeAsync(long brandId, string name, CancellationToken ct)
    {
        var baseCode = CatalogNameNormalizer.Slug(name);
        var code = baseCode;
        for (var i = 2; await _models.GetByCodeAsync(brandId, code, ct) is not null; i++) code = $"{baseCode}_{i}";
        return code;
    }

    private static VesselBrandDto ToDto(VesselBrandEntity e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name, CountryCode = e.CountryCode,
        IsActive = e.IsActive, NeedsReview = e.NeedsReview, Source = e.Source, CreatedAt = e.CreateDate,
    };

    private static VesselModelDto ToDto(VesselModelEntity e, string? brandName) => new()
    {
        Id = e.Id, VesselBrandId = e.VesselBrandId, BrandName = brandName, Code = e.Code, Name = e.Name,
        VesselTypeCode = e.VesselTypeCode, YearFrom = e.YearFrom, YearTo = e.YearTo, LengthMeters = e.LengthMeters,
        IsActive = e.IsActive, NeedsReview = e.NeedsReview, Source = e.Source, CreatedAt = e.CreateDate,
    };
}
