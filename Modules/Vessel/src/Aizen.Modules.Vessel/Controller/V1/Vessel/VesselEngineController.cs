using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Application.Command.Engine;
using Aizen.Modules.Vessel.Application.Query.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUow.Paging;

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
    [ProducesResponseType(typeof(IPaginate<VesselEngineDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<VesselEngineDto>?>> GetAll(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IPaginate<VesselEngineDto>>(new GetVesselEnginesQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselEngineDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselEngineDto?>> Add([FromRoute] long vesselId, [FromBody] AddVesselEngineRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselEngineDto>(new AddVesselEngineCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{engineId:long}")]
    [ProducesResponseType(typeof(VesselEngineDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselEngineDto?>> Update([FromRoute] long vesselId, [FromRoute] long engineId, [FromBody] UpdateVesselEngineRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselEngineDto>(new UpdateVesselEngineCommand(vesselId, engineId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{engineId:long}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, [FromRoute] long engineId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselEngineCommand(vesselId, engineId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{engineId:long}/set-primary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> SetPrimary([FromRoute] long vesselId, [FromRoute] long engineId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new SetPrimaryVesselEngineCommand(vesselId, engineId), ct);
        return SetResponse<object>(new { success = true });
    }
}
