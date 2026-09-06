using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IVesselCatalogReferenceService
{
    // Reads
    Task<IReadOnlyList<VesselBrandDto>> SearchBrandsAsync(string? search, bool onlyActive, int take, CancellationToken ct = default);
    Task<VesselBrandDto?> GetBrandByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<VesselModelDto>> GetModelsAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default);
    Task<VesselModelDto?> GetModelByIdAsync(long id, CancellationToken ct = default);
    // "Not in list" submissions (NeedsReview=true, IsActive=true, dedupe by normalized name)
    Task<VesselBrandDto> SubmitBrandAsync(SubmitVesselBrandRequest req, CancellationToken ct = default);
    Task<VesselModelDto> SubmitModelAsync(SubmitVesselModelRequest req, CancellationToken ct = default);
    // Admin
    Task<VesselBrandDto> CreateBrandAsync(CreateVesselBrandRequest req, CancellationToken ct = default);
    Task<VesselBrandDto> UpdateBrandAsync(long id, UpdateVesselBrandRequest req, CancellationToken ct = default);
    Task<VesselModelDto> CreateModelAsync(CreateVesselModelRequest req, CancellationToken ct = default);
    Task<VesselModelDto> UpdateModelAsync(long id, UpdateVesselModelRequest req, CancellationToken ct = default);
    Task ApproveBrandAsync(long id, CancellationToken ct = default);
    Task ApproveModelAsync(long id, CancellationToken ct = default);
    Task SetBrandActiveAsync(long id, bool active, CancellationToken ct = default);
    Task SetModelActiveAsync(long id, bool active, CancellationToken ct = default);
    Task<IReadOnlyList<VesselBrandDto>> ListBrandsForReviewAsync(int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<VesselModelDto>> ListModelsForReviewAsync(int skip, int take, CancellationToken ct = default);
}

public interface IEngineCatalogReferenceService
{
    Task<IReadOnlyList<EngineBrandDto>> SearchBrandsAsync(string? search, bool onlyActive, int take, CancellationToken ct = default);
    Task<EngineBrandDto?> GetBrandByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<EngineModelDto>> GetModelsAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default);
    Task<EngineModelDto?> GetModelByIdAsync(long id, CancellationToken ct = default);
    Task<EngineBrandDto> SubmitBrandAsync(SubmitEngineBrandRequest req, CancellationToken ct = default);
    Task<EngineModelDto> SubmitModelAsync(SubmitEngineModelRequest req, CancellationToken ct = default);
    Task<EngineBrandDto> CreateBrandAsync(CreateEngineBrandRequest req, CancellationToken ct = default);
    Task<EngineBrandDto> UpdateBrandAsync(long id, UpdateEngineBrandRequest req, CancellationToken ct = default);
    Task<EngineModelDto> CreateModelAsync(CreateEngineModelRequest req, CancellationToken ct = default);
    Task<EngineModelDto> UpdateModelAsync(long id, UpdateEngineModelRequest req, CancellationToken ct = default);
    Task ApproveBrandAsync(long id, CancellationToken ct = default);
    Task ApproveModelAsync(long id, CancellationToken ct = default);
    Task SetBrandActiveAsync(long id, bool active, CancellationToken ct = default);
    Task SetModelActiveAsync(long id, bool active, CancellationToken ct = default);
    Task<IReadOnlyList<EngineBrandDto>> ListBrandsForReviewAsync(int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<EngineModelDto>> ListModelsForReviewAsync(int skip, int take, CancellationToken ct = default);
}
