using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

/// <summary>Vessel brand/model catalog reads + "not in list" owner submissions. Authenticated (BFFs forward the
/// service token). Submissions create rows with NeedsReview=true but IsActive=true (immediately usable).</summary>
[ApiController]
[Authorize]
[Route("api/v1/reference-data/vessel-catalog")]
[Tags("Vessel Catalog")]
public sealed class VesselCatalogController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public VesselCatalogController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpGet("brands")]
    public async Task<AizenApiResponse<IReadOnlyList<VesselBrandDto>>> Brands(
        [FromQuery] string? search, [FromQuery] bool onlyActive = true, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<VesselBrandDto>>(new SearchVesselBrandsQuery(search, onlyActive, take), ct));

    [HttpGet("brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<IReadOnlyList<VesselModelDto>>> Models(
        [FromRoute] long brandId, [FromQuery] string? search, [FromQuery] string? typeCode,
        [FromQuery] bool onlyActive = true, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<IReadOnlyList<VesselModelDto>>(new GetVesselModelsQuery(brandId, search, typeCode, onlyActive, take), ct));

    [HttpGet("models/{id:long}")]
    public async Task<AizenApiResponse<VesselModelDto?>> Model([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<VesselModelDto?>(new GetVesselModelByIdQuery(id), ct));

    [HttpPost("brands")]
    public async Task<AizenApiResponse<VesselBrandDto?>> SubmitBrand([FromBody] SubmitVesselBrandRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<VesselBrandDto>(new SubmitVesselBrandCommand(req), ct));

    [HttpPost("brands/{brandId:long}/models")]
    public async Task<AizenApiResponse<VesselModelDto?>> SubmitModel([FromRoute] long brandId, [FromBody] SubmitVesselModelRequest req, CancellationToken ct = default)
    {
        req.VesselBrandId = brandId;
        return SetResponse(await _cqrs.ProcessAsync<VesselModelDto>(new SubmitVesselModelCommand(req), ct));
    }
}
