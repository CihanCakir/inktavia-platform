using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;
using Aizen.Modules.Vessel.Application.Command.Engine;
using Aizen.Modules.Vessel.Application.Query.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/engines")]
[Tags("Vessel - Engines")]
[Authorize]
[DocumentationInfo("Vessel engine endpoints", "Manage vessel engine records.")]
public sealed class VesselEngineController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselEngineController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetVesselEnginesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselEnginesResponse?>> GetAll(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselEnginesResponse>(new GetVesselEnginesQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddVesselEngineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddVesselEngineResponse?>> Add([FromRoute] long vesselId, [FromBody] AddVesselEngineRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddVesselEngineResponse>(new AddVesselEngineCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{engineId:long}")]
    [ProducesResponseType(typeof(UpdateVesselEngineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselEngineResponse?>> Update([FromRoute] long vesselId, [FromRoute] long engineId, [FromBody] UpdateVesselEngineRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselEngineResponse>(new UpdateVesselEngineCommand(vesselId, engineId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{engineId:long}")]
    [ProducesResponseType(typeof(RemoveVesselEngineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselEngineResponse?>> Remove([FromRoute] long vesselId, [FromRoute] long engineId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RemoveVesselEngineResponse>(new RemoveVesselEngineCommand(vesselId, engineId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{engineId:long}/set-primary")]
    [ProducesResponseType(typeof(SetPrimaryVesselEngineResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SetPrimaryVesselEngineResponse?>> SetPrimary([FromRoute] long vesselId, [FromRoute] long engineId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SetPrimaryVesselEngineResponse>(new SetPrimaryVesselEngineCommand(vesselId, engineId), ct);
        return SetResponse(result);
    }
}
