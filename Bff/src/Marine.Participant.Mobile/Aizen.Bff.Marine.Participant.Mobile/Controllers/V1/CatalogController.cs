using Aizen.Bff.Marine.Participant.Mobile.Application.Catalog;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Vessel wizard brand/model pickers — search reads + "not in list" submissions (created NeedsReview but
/// immediately usable so vessel create never blocks). Participant-authenticated.</summary>
[ApiController]
[Route("api/v1/mobile/catalog")]
[Tags("Mobile - Catalog")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class CatalogController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public CatalogController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpGet("vessel/brands")]
    public async Task<AizenApiResponse<List<VesselBrandDto>>> VesselBrands([FromQuery] string? search, [FromQuery] int take = 100, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetMobileVesselBrandsQuery(search, take), ct));

    [HttpGet("vessel/brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<List<VesselModelDto>>> VesselModels([FromRoute] long brandId, [FromQuery] string? search, [FromQuery] string? typeCode, [FromQuery] int take = 100, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetMobileVesselModelsQuery(brandId, search, typeCode, take), ct));

    [HttpPost("vessel/brands")]
    public async Task<AizenApiResponse<VesselBrandDto>> SubmitVesselBrand([FromBody] SubmitVesselBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubmitMobileVesselBrandCommand(req), ct));

    [HttpPost("vessel/brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<VesselModelDto>> SubmitVesselModel([FromRoute] long brandId, [FromBody] SubmitVesselModelRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubmitMobileVesselModelCommand(brandId, req), ct));

    [HttpGet("engine/brands")]
    public async Task<AizenApiResponse<List<EngineBrandDto>>> EngineBrands([FromQuery] string? search, [FromQuery] int take = 100, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetMobileEngineBrandsQuery(search, take), ct));

    [HttpGet("engine/brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<List<EngineModelDto>>> EngineModels([FromRoute] long brandId, [FromQuery] string? search, [FromQuery] string? typeCode, [FromQuery] int take = 100, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetMobileEngineModelsQuery(brandId, search, typeCode, take), ct));

    [HttpPost("engine/brands")]
    public async Task<AizenApiResponse<EngineBrandDto>> SubmitEngineBrand([FromBody] SubmitEngineBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubmitMobileEngineBrandCommand(req), ct));

    [HttpPost("engine/brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<EngineModelDto>> SubmitEngineModel([FromRoute] long brandId, [FromBody] SubmitEngineModelRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubmitMobileEngineModelCommand(brandId, req), ct));
}
