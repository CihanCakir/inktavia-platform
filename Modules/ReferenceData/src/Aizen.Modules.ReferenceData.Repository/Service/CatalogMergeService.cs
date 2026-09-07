using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class CatalogMergeService : ICatalogMergeService
{
    private readonly IVesselBrandRepository _vBrands;
    private readonly IVesselModelRepository _vModels;
    private readonly IEngineBrandRepository _eBrands;
    private readonly IEngineModelRepository _eModels;
    private readonly ReferenceDataDbContext _db;

    public CatalogMergeService(
        IVesselBrandRepository vBrands, IVesselModelRepository vModels,
        IEngineBrandRepository eBrands, IEngineModelRepository eModels,
        ReferenceDataDbContext db)
    {
        _vBrands = vBrands; _vModels = vModels; _eBrands = eBrands; _eModels = eModels; _db = db;
    }

    public async Task<CatalogMergeResultDto> MergeAsync(CatalogMergeType type, long sourceId, long targetId, CancellationToken ct = default)
    {
        if (sourceId == targetId)
            throw new AizenBusinessException("Source and target must be different.");

        var result = new CatalogMergeResultDto { Type = type, SourceId = sourceId, TargetId = targetId };

        switch (type)
        {
            case CatalogMergeType.VesselBrand:
            {
                var source = await _vBrands.GetByIdAsync(sourceId, ct) ?? throw NotFound("vessel brand", sourceId);
                _ = await _vBrands.GetByIdAsync(targetId, ct) ?? throw NotFound("vessel brand", targetId);
                if (source.MergedIntoId is not null) { result.AlreadyMerged = true; result.Success = true; return result; }
                source.MarkMergedInto(targetId);
                _vBrands.Update(source);
                break;
            }
            case CatalogMergeType.VesselModel:
            {
                var source = await _vModels.GetByIdAsync(sourceId, ct) ?? throw NotFound("vessel model", sourceId);
                var target = await _vModels.GetByIdAsync(targetId, ct) ?? throw NotFound("vessel model", targetId);
                if (source.VesselBrandId != target.VesselBrandId)
                    throw new AizenBusinessException("Models can only be merged within the same brand.");
                if (source.MergedIntoId is not null) { result.AlreadyMerged = true; result.Success = true; return result; }
                source.MarkMergedInto(targetId);
                _vModels.Update(source);
                break;
            }
            case CatalogMergeType.EngineBrand:
            {
                var source = await _eBrands.GetByIdAsync(sourceId, ct) ?? throw NotFound("engine brand", sourceId);
                _ = await _eBrands.GetByIdAsync(targetId, ct) ?? throw NotFound("engine brand", targetId);
                if (source.MergedIntoId is not null) { result.AlreadyMerged = true; result.Success = true; return result; }
                source.MarkMergedInto(targetId);
                _eBrands.Update(source);
                break;
            }
            case CatalogMergeType.EngineModel:
            {
                var source = await _eModels.GetByIdAsync(sourceId, ct) ?? throw NotFound("engine model", sourceId);
                var target = await _eModels.GetByIdAsync(targetId, ct) ?? throw NotFound("engine model", targetId);
                if (source.EngineBrandId != target.EngineBrandId)
                    throw new AizenBusinessException("Models can only be merged within the same brand.");
                if (source.MergedIntoId is not null) { result.AlreadyMerged = true; result.Success = true; return result; }
                source.MarkMergedInto(targetId);
                _eModels.Update(source);
                break;
            }
            default:
                throw new AizenBusinessException($"Unknown catalog merge type '{type}'.");
        }

        await _db.SaveChangesAsync(ct);
        result.Success = true;
        return result;
    }

    private static AizenBusinessException NotFound(string what, long id) => new($"Catalog {what} '{id}' not found.");
}
