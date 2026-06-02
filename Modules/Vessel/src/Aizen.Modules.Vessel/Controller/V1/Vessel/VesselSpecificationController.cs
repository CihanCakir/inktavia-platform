using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Application.Command.Specification;
using Aizen.Modules.Vessel.Application.Query.Specification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/specification")]
[Tags("Vessel - Specification")]
[Authorize]
[DocumentationInfo("Vessel specification endpoints", "Upsert and query vessel physical specification.")]
public sealed class VesselSpecificationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselSpecificationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VesselSpecificationDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselSpecificationDto?>> Get([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselSpecificationDto>(new GetVesselSpecificationQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(VesselSpecificationDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselSpecificationDto?>> Upsert([FromRoute] long vesselId, [FromBody] UpsertVesselSpecificationRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselSpecificationDto>(new UpsertVesselSpecificationCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselSpecificationCommand(vesselId), ct);
        return SetResponse<object>(new { success = true });
    }
}
