using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

/// <summary>Admin CRUD + review queue for the vessel brand/model catalog. Admin-role only (service token carries it).</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/reference-data/vessel-catalog")]
[Tags("Admin - Vessel Catalog")]
public sealed class VesselCatalogAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public VesselCatalogAdminController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    // Review queue
    [HttpGet("brands/review")]
    public async Task<AizenApiResponse<IReadOnlyList<VesselBrandDto>>> BrandsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<VesselBrandDto>>(new ListVesselBrandsForReviewQuery(skip, take), ct));

    [HttpGet("models/review")]
    public async Task<AizenApiResponse<IReadOnlyList<VesselModelDto>>> ModelsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<VesselModelDto>>(new ListVesselModelsForReviewQuery(skip, take), ct));

    // Brands CRUD
    [HttpPost("brands")]
    public async Task<AizenApiResponse<VesselBrandDto?>> CreateBrand([FromBody] CreateVesselBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<VesselBrandDto>(new CreateVesselBrandCommand(req), ct));

    [HttpPut("brands/{id:long}")]
    public async Task<AizenApiResponse<VesselBrandDto?>> UpdateBrand([FromRoute] long id, [FromBody] UpdateVesselBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<VesselBrandDto>(new UpdateVesselBrandCommand(id, req), ct));

    [HttpPut("brands/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveBrand([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new ApproveVesselBrandCommand(id), ct));

    [HttpPut("brands/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateBrand([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetVesselBrandActiveCommand(id, true), ct));

    [HttpPut("brands/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateBrand([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetVesselBrandActiveCommand(id, false), ct));

    // Models CRUD
    [HttpPost("models")]
    public async Task<AizenApiResponse<VesselModelDto?>> CreateModel([FromBody] CreateVesselModelRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<VesselModelDto>(new CreateVesselModelCommand(req), ct));

    [HttpPut("models/{id:long}")]
    public async Task<AizenApiResponse<VesselModelDto?>> UpdateModel([FromRoute] long id, [FromBody] UpdateVesselModelRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<VesselModelDto>(new UpdateVesselModelCommand(id, req), ct));

    [HttpPut("models/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveModel([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new ApproveVesselModelCommand(id), ct));

    [HttpPut("models/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateModel([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetVesselModelActiveCommand(id, true), ct));

    [HttpPut("models/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateModel([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetVesselModelActiveCommand(id, false), ct));
}
