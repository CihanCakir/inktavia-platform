using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>Admin CRUD + review queue for the vessel/engine brand-model catalog. Thin passthrough to the ReferenceData
/// module admin endpoints (the admin service token carries the Admin role the module authorizes on).</summary>
[ApiController]
[Route("api/v1/admin-panel/reference-data/catalog")]
[Tags("Admin Panel - Brand/Model Catalog")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class CatalogAdminController : AizenWebApiController
{
    private readonly IReferenceDataRemoteCall _rd;
    private readonly IVesselRemoteCall _vessel;
    public CatalogAdminController(IHttpContextAccessor http, IReferenceDataRemoteCall rd, IVesselRemoteCall vessel) : base(http)
    { _rd = rd; _vessel = vessel; }

    // ── Review queue (enriched with vesselReferenceCount from the Vessel module — review path only) ──
    [HttpGet("vessel/brands/review")]
    public async Task<AizenApiResponse<List<VesselBrandDto>?>> VesselBrandsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var items = (await _rd.GetVesselBrandsForReview(skip, take))?.Body;
        var counts = await SafeCountsAsync();
        if (items is not null && counts is not null)
            foreach (var i in items) i.ReferenceCount = counts.VesselBrand.GetValueOrDefault(i.Id);
        return SetResponse(items);
    }
    [HttpGet("vessel/models/review")]
    public async Task<AizenApiResponse<List<VesselModelDto>?>> VesselModelsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var items = (await _rd.GetVesselModelsForReview(skip, take))?.Body;
        var counts = await SafeCountsAsync();
        if (items is not null && counts is not null)
            foreach (var i in items) i.ReferenceCount = counts.VesselModel.GetValueOrDefault(i.Id);
        return SetResponse(items);
    }
    [HttpGet("engine/brands/review")]
    public async Task<AizenApiResponse<List<EngineBrandDto>?>> EngineBrandsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var items = (await _rd.GetEngineBrandsForReview(skip, take))?.Body;
        var counts = await SafeCountsAsync();
        if (items is not null && counts is not null)
            foreach (var i in items) i.ReferenceCount = counts.EngineBrand.GetValueOrDefault(i.Id);
        return SetResponse(items);
    }
    [HttpGet("engine/models/review")]
    public async Task<AizenApiResponse<List<EngineModelDto>?>> EngineModelsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var items = (await _rd.GetEngineModelsForReview(skip, take))?.Body;
        var counts = await SafeCountsAsync();
        if (items is not null && counts is not null)
            foreach (var i in items) i.ReferenceCount = counts.EngineModel.GetValueOrDefault(i.Id);
        return SetResponse(items);
    }

    // Vessel counts are best-effort enrichment — a Vessel-module hiccup must not fail the review list.
    private async Task<Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference.CatalogReferenceCountsDto?> SafeCountsAsync()
    {
        try { return (await _vessel.GetCatalogReferenceCounts())?.Body; }
        catch { return null; }
    }

    // ── Merge duplicate catalog entry: validate + deactivate source (ReferenceData), then repoint vessel refs
    //    source→target (Vessel). Ordered + idempotent (not a distributed txn) — see the report. ──
    [HttpPost("merge")]
    public async Task<AizenApiResponse<CatalogMergeResultDto?>> Merge([FromBody] MergeCatalogRequest req)
    {
        var merge = (await _rd.MergeCatalogEntry(req))?.Body;   // throws a business error on invalid (cross-brand etc.)
        if (merge is null || !merge.Success) return SetResponse(merge);

        var repoint = (await _vessel.RepointCatalogReference(new Aizen.Modules.Vessel.Abstraction.Request.CatalogReference.RepointCatalogReferenceRequest
        {
            Kind = (Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference.CatalogRefKind)(int)req.Type,
            SourceId = req.SourceId,
            TargetId = req.TargetId,
        }))?.Body;
        merge.RepointedVesselReferences = repoint?.RepointedCount ?? 0;
        return SetResponse(merge);
    }

    // ── Browse (admin sees inactive too) ──
    [HttpGet("vessel/brands")]
    public async Task<AizenApiResponse<List<VesselBrandDto>?>> VesselBrands([FromQuery] string? search, [FromQuery] int take = 50)
        => SetResponse((await _rd.GetVesselBrands(search, false, take))?.Body);
    [HttpGet("vessel/brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<List<VesselModelDto>?>> VesselModels(long brandId, [FromQuery] string? search, [FromQuery] string? typeCode, [FromQuery] int take = 50)
        => SetResponse((await _rd.GetVesselModels(brandId, search, typeCode, false, take))?.Body);
    [HttpGet("engine/brands")]
    public async Task<AizenApiResponse<List<EngineBrandDto>?>> EngineBrands([FromQuery] string? search, [FromQuery] int take = 50)
        => SetResponse((await _rd.GetEngineBrands(search, false, take))?.Body);
    [HttpGet("engine/brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<List<EngineModelDto>?>> EngineModels(long brandId, [FromQuery] string? search, [FromQuery] string? typeCode, [FromQuery] int take = 50)
        => SetResponse((await _rd.GetEngineModels(brandId, search, typeCode, false, take))?.Body);

    // ── Vessel CRUD ──
    [HttpPost("vessel/brands")]
    public async Task<AizenApiResponse<VesselBrandDto?>> CreateVesselBrand([FromBody] CreateVesselBrandRequest req)
        => SetResponse((await _rd.CreateVesselBrand(req))?.Body);
    [HttpPut("vessel/brands/{id:long}")]
    public async Task<AizenApiResponse<VesselBrandDto?>> UpdateVesselBrand(long id, [FromBody] UpdateVesselBrandRequest req)
        => SetResponse((await _rd.UpdateVesselBrand(id, req))?.Body);
    [HttpPut("vessel/brands/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveVesselBrand(long id) => SetResponse((await _rd.ApproveVesselBrand(id))?.Body);
    [HttpPut("vessel/brands/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateVesselBrand(long id) => SetResponse((await _rd.ActivateVesselBrand(id))?.Body);
    [HttpPut("vessel/brands/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateVesselBrand(long id) => SetResponse((await _rd.DeactivateVesselBrand(id))?.Body);
    [HttpPost("vessel/models")]
    public async Task<AizenApiResponse<VesselModelDto?>> CreateVesselModel([FromBody] CreateVesselModelRequest req)
        => SetResponse((await _rd.CreateVesselModel(req))?.Body);
    [HttpPut("vessel/models/{id:long}")]
    public async Task<AizenApiResponse<VesselModelDto?>> UpdateVesselModel(long id, [FromBody] UpdateVesselModelRequest req)
        => SetResponse((await _rd.UpdateVesselModel(id, req))?.Body);
    [HttpPut("vessel/models/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveVesselModel(long id) => SetResponse((await _rd.ApproveVesselModel(id))?.Body);
    [HttpPut("vessel/models/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateVesselModel(long id) => SetResponse((await _rd.ActivateVesselModel(id))?.Body);
    [HttpPut("vessel/models/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateVesselModel(long id) => SetResponse((await _rd.DeactivateVesselModel(id))?.Body);

    // ── Engine CRUD ──
    [HttpPost("engine/brands")]
    public async Task<AizenApiResponse<EngineBrandDto?>> CreateEngineBrand([FromBody] CreateEngineBrandRequest req)
        => SetResponse((await _rd.CreateEngineBrand(req))?.Body);
    [HttpPut("engine/brands/{id:long}")]
    public async Task<AizenApiResponse<EngineBrandDto?>> UpdateEngineBrand(long id, [FromBody] UpdateEngineBrandRequest req)
        => SetResponse((await _rd.UpdateEngineBrand(id, req))?.Body);
    [HttpPut("engine/brands/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveEngineBrand(long id) => SetResponse((await _rd.ApproveEngineBrand(id))?.Body);
    [HttpPut("engine/brands/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateEngineBrand(long id) => SetResponse((await _rd.ActivateEngineBrand(id))?.Body);
    [HttpPut("engine/brands/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateEngineBrand(long id) => SetResponse((await _rd.DeactivateEngineBrand(id))?.Body);
    [HttpPost("engine/models")]
    public async Task<AizenApiResponse<EngineModelDto?>> CreateEngineModel([FromBody] CreateEngineModelRequest req)
        => SetResponse((await _rd.CreateEngineModel(req))?.Body);
    [HttpPut("engine/models/{id:long}")]
    public async Task<AizenApiResponse<EngineModelDto?>> UpdateEngineModel(long id, [FromBody] UpdateEngineModelRequest req)
        => SetResponse((await _rd.UpdateEngineModel(id, req))?.Body);
    [HttpPut("engine/models/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveEngineModel(long id) => SetResponse((await _rd.ApproveEngineModel(id))?.Body);
    [HttpPut("engine/models/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateEngineModel(long id) => SetResponse((await _rd.ActivateEngineModel(id))?.Body);
    [HttpPut("engine/models/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateEngineModel(long id) => SetResponse((await _rd.DeactivateEngineModel(id))?.Body);
}
