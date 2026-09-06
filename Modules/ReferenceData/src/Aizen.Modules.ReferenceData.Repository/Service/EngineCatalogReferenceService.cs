using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class EngineCatalogReferenceService : IEngineCatalogReferenceService
{
    private const int MaxTake = 200;
    private readonly IEngineBrandRepository _brands;
    private readonly IEngineModelRepository _models;
    private readonly ReferenceDataDbContext _db;

    public EngineCatalogReferenceService(IEngineBrandRepository brands, IEngineModelRepository models, ReferenceDataDbContext db)
    {
        _brands = brands; _models = models; _db = db;
    }

    public async Task<IReadOnlyList<EngineBrandDto>> SearchBrandsAsync(string? search, bool onlyActive, int take, CancellationToken ct = default)
        => (await _brands.SearchAsync(search, onlyActive, Clamp(take), ct)).Select(ToDto).ToList();

    public async Task<EngineBrandDto?> GetBrandByIdAsync(long id, CancellationToken ct = default)
        => (await _brands.GetByIdAsync(id, ct)) is { } b ? ToDto(b) : null;

    public async Task<IReadOnlyList<EngineModelDto>> GetModelsAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default)
    {
        var brand = await _brands.GetByIdAsync(brandId, ct);
        var models = await _models.SearchAsync(brandId, search, typeCode, onlyActive, Clamp(take), ct);
        return models.Select(m => ToDto(m, brand?.Name)).ToList();
    }

    public async Task<EngineModelDto?> GetModelByIdAsync(long id, CancellationToken ct = default)
    {
        var m = await _models.GetByIdAsync(id, ct);
        if (m is null) return null;
        var brand = await _brands.GetByIdAsync(m.EngineBrandId, ct);
        return ToDto(m, brand?.Name);
    }

    public async Task<EngineBrandDto> SubmitBrandAsync(SubmitEngineBrandRequest req, CancellationToken ct = default)
    {
        var existing = await _brands.GetByNameKeyAsync(CatalogNameNormalizer.Key(req.Name), ct);
        if (existing is not null) return ToDto(existing);

        var entity = EngineBrandEntity.Create(await UniqueBrandCodeAsync(req.Name, ct), req.Name, needsReview: true, source: "owner-submission");
        await _brands.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<EngineModelDto> SubmitModelAsync(SubmitEngineModelRequest req, CancellationToken ct = default)
    {
        var brand = await _brands.GetByIdAsync(req.EngineBrandId, ct) ?? throw new AizenBusinessException("Engine brand not found.");
        var existing = await _models.GetByNameKeyAsync(brand.Id, CatalogNameNormalizer.Key(req.Name), ct);
        if (existing is not null) return ToDto(existing, brand.Name);

        var entity = EngineModelEntity.Create(brand.Id, await UniqueModelCodeAsync(brand.Id, req.Name, ct), req.Name,
            req.HorsePower, req.FuelTypeCode, req.EngineTypeCode, req.YearFrom, req.YearTo, needsReview: true, source: "owner-submission");
        await _models.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity, brand.Name);
    }

    public async Task<EngineBrandDto> CreateBrandAsync(CreateEngineBrandRequest req, CancellationToken ct = default)
    {
        var code = string.IsNullOrWhiteSpace(req.Code) ? await UniqueBrandCodeAsync(req.Name, ct) : req.Code!.Trim().ToUpperInvariant();
        if (await _brands.ExistsByCodeAsync(code, ct)) throw new AizenBusinessException($"Engine brand code '{code}' already exists.");
        var entity = EngineBrandEntity.Create(code, req.Name, needsReview: false, source: "admin");
        await _brands.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<EngineBrandDto> UpdateBrandAsync(long id, UpdateEngineBrandRequest req, CancellationToken ct = default)
    {
        var entity = await _brands.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Engine brand not found.");
        entity.Update(req.Name, req.IsActive);
        _brands.Update(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<EngineModelDto> CreateModelAsync(CreateEngineModelRequest req, CancellationToken ct = default)
    {
        var brand = await _brands.GetByIdAsync(req.EngineBrandId, ct) ?? throw new AizenBusinessException("Engine brand not found.");
        var code = string.IsNullOrWhiteSpace(req.Code) ? await UniqueModelCodeAsync(brand.Id, req.Name, ct) : req.Code!.Trim().ToUpperInvariant();
        if (await _models.GetByCodeAsync(brand.Id, code, ct) is not null) throw new AizenBusinessException($"Engine model code '{code}' already exists in this brand.");
        var entity = EngineModelEntity.Create(brand.Id, code, req.Name, req.HorsePower, req.FuelTypeCode, req.EngineTypeCode, req.YearFrom, req.YearTo, needsReview: false, source: "admin");
        await _models.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity, brand.Name);
    }

    public async Task<EngineModelDto> UpdateModelAsync(long id, UpdateEngineModelRequest req, CancellationToken ct = default)
    {
        var entity = await _models.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Engine model not found.");
        entity.Update(req.Name, req.HorsePower, req.FuelTypeCode, req.EngineTypeCode, req.YearFrom, req.YearTo, req.IsActive);
        _models.Update(entity);
        await _db.SaveChangesAsync(ct);
        var brand = await _brands.GetByIdAsync(entity.EngineBrandId, ct);
        return ToDto(entity, brand?.Name);
    }

    public async Task ApproveBrandAsync(long id, CancellationToken ct = default)
    {
        var e = await _brands.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Engine brand not found.");
        e.Approve(); _brands.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task ApproveModelAsync(long id, CancellationToken ct = default)
    {
        var e = await _models.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Engine model not found.");
        e.Approve(); _models.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task SetBrandActiveAsync(long id, bool active, CancellationToken ct = default)
    {
        var e = await _brands.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Engine brand not found.");
        if (active) e.Activate(); else e.Deactivate();
        _brands.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task SetModelActiveAsync(long id, bool active, CancellationToken ct = default)
    {
        var e = await _models.GetByIdAsync(id, ct) ?? throw new AizenBusinessException("Engine model not found.");
        if (active) e.Activate(); else e.Deactivate();
        _models.Update(e); await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EngineBrandDto>> ListBrandsForReviewAsync(int skip, int take, CancellationToken ct = default)
        => (await _brands.ListNeedsReviewAsync(Math.Max(0, skip), Clamp(take), ct)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<EngineModelDto>> ListModelsForReviewAsync(int skip, int take, CancellationToken ct = default)
        => (await _models.ListNeedsReviewAsync(Math.Max(0, skip), Clamp(take), ct)).Select(m => ToDto(m, null)).ToList();

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

    private static EngineBrandDto ToDto(EngineBrandEntity e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name, IsActive = e.IsActive, NeedsReview = e.NeedsReview, Source = e.Source,
    };

    private static EngineModelDto ToDto(EngineModelEntity e, string? brandName) => new()
    {
        Id = e.Id, EngineBrandId = e.EngineBrandId, BrandName = brandName, Code = e.Code, Name = e.Name,
        HorsePower = e.HorsePower, FuelTypeCode = e.FuelTypeCode, EngineTypeCode = e.EngineTypeCode,
        YearFrom = e.YearFrom, YearTo = e.YearTo, IsActive = e.IsActive, NeedsReview = e.NeedsReview, Source = e.Source,
    };
}
