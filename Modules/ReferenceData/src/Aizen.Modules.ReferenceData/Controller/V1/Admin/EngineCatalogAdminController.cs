using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

/// <summary>Admin CRUD + review queue for the engine brand/model catalog.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/reference-data/engine-catalog")]
[Tags("Admin - Engine Catalog")]
public sealed class EngineCatalogAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public EngineCatalogAdminController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpGet("brands/review")]
    public async Task<AizenApiResponse<IReadOnlyList<EngineBrandDto>>> BrandsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<EngineBrandDto>>(new ListEngineBrandsForReviewQuery(skip, take), ct));

    [HttpGet("models/review")]
    public async Task<AizenApiResponse<IReadOnlyList<EngineModelDto>>> ModelsForReview([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<EngineModelDto>>(new ListEngineModelsForReviewQuery(skip, take), ct));

    [HttpPost("brands")]
    public async Task<AizenApiResponse<EngineBrandDto?>> CreateBrand([FromBody] CreateEngineBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<EngineBrandDto>(new CreateEngineBrandCommand(req), ct));

    [HttpPut("brands/{id:long}")]
    public async Task<AizenApiResponse<EngineBrandDto?>> UpdateBrand([FromRoute] long id, [FromBody] UpdateEngineBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<EngineBrandDto>(new UpdateEngineBrandCommand(id, req), ct));

    [HttpPut("brands/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveBrand([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new ApproveEngineBrandCommand(id), ct));

    [HttpPut("brands/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateBrand([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetEngineBrandActiveCommand(id, true), ct));

    [HttpPut("brands/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateBrand([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetEngineBrandActiveCommand(id, false), ct));

    [HttpPost("models")]
    public async Task<AizenApiResponse<EngineModelDto?>> CreateModel([FromBody] CreateEngineModelRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<EngineModelDto>(new CreateEngineModelCommand(req), ct));

    [HttpPut("models/{id:long}")]
    public async Task<AizenApiResponse<EngineModelDto?>> UpdateModel([FromRoute] long id, [FromBody] UpdateEngineModelRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<EngineModelDto>(new UpdateEngineModelCommand(id, req), ct));

    [HttpPut("models/{id:long}/approve")]
    public async Task<AizenApiResponse<BoolResult?>> ApproveModel([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new ApproveEngineModelCommand(id), ct));

    [HttpPut("models/{id:long}/activate")]
    public async Task<AizenApiResponse<BoolResult?>> ActivateModel([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetEngineModelActiveCommand(id, true), ct));

    [HttpPut("models/{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> DeactivateModel([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new SetEngineModelActiveCommand(id, false), ct));
}
