using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Application.Command.Engine;
using Aizen.Modules.Vessel.Application.Query.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<VesselEngineDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<VesselEngineDto>?>> GetAll([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<VesselEngineDto>>(new GetVesselEnginesQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselEngineDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselEngineDto?>> Add([FromRoute] long vesselId, [FromBody] AddVesselEngineRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselEngineDto>(new AddVesselEngineCommand(vesselId, req, CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpPut("{engineId:long}")]
    [ProducesResponseType(typeof(VesselEngineDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselEngineDto?>> Update([FromRoute] long vesselId, [FromRoute] long engineId, [FromBody] UpdateVesselEngineRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselEngineDto>(new UpdateVesselEngineCommand(vesselId, engineId, req, CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpDelete("{engineId:long}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, [FromRoute] long engineId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselEngineCommand(vesselId, engineId, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{engineId:long}/set-primary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> SetPrimary([FromRoute] long vesselId, [FromRoute] long engineId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new SetPrimaryVesselEngineCommand(vesselId, engineId, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }
}
