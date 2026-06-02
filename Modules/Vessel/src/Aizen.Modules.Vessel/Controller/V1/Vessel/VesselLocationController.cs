using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Location;
using Aizen.Modules.Vessel.Application.Command.Location;
using Aizen.Modules.Vessel.Application.Query.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/location")]
[Tags("Vessel - Location")]
[Authorize]
[DocumentationInfo("Vessel location endpoints", "Update and query vessel location snapshots.")]
public sealed class VesselLocationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselLocationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("current")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VesselLocationSnapshotDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselLocationSnapshotDto?>> GetCurrent([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselLocationSnapshotDto>(new GetCurrentVesselLocationQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(VesselLocationSnapshotDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselLocationSnapshotDto?>> Update([FromRoute] long vesselId, [FromBody] UpdateVesselLocationSnapshotRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselLocationSnapshotDto>(new UpdateVesselLocationSnapshotCommand(vesselId, req, CurrentUserId), ct);
        return SetResponse(result);
    }
}
