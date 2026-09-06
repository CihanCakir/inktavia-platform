using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

/// <summary>Engine brand/model catalog reads + "not in list" owner submissions.</summary>
[ApiController]
[Authorize]
[Route("api/v1/reference-data/engine-catalog")]
[Tags("Engine Catalog")]
public sealed class EngineCatalogController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public EngineCatalogController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpGet("brands")]
    public async Task<AizenApiResponse<IReadOnlyList<EngineBrandDto>>> Brands(
        [FromQuery] string? search, [FromQuery] bool onlyActive = true, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<EngineBrandDto>>(new SearchEngineBrandsQuery(search, onlyActive, take), ct));

    [HttpGet("brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<IReadOnlyList<EngineModelDto>>> Models(
        [FromRoute] long brandId, [FromQuery] string? search, [FromQuery] string? typeCode,
        [FromQuery] bool onlyActive = true, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<EngineModelDto>>(new GetEngineModelsQuery(brandId, search, typeCode, onlyActive, take), ct));

    [HttpGet("models/{id:long}")]
    public async Task<AizenApiResponse<EngineModelDto?>> Model([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<EngineModelDto?>(new GetEngineModelByIdQuery(id), ct));

    [HttpPost("brands")]
    public async Task<AizenApiResponse<EngineBrandDto?>> SubmitBrand([FromBody] SubmitEngineBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<EngineBrandDto>(new SubmitEngineBrandCommand(req), ct));

    [HttpPost("brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<EngineModelDto?>> SubmitModel([FromRoute] long brandId, [FromBody] SubmitEngineModelRequest req, CancellationToken ct = default)
    {
        req.EngineBrandId = brandId;
        return SetResponse(await _cqrs.ProcessAsync<EngineModelDto>(new SubmitEngineModelCommand(req), ct));
    }
}
