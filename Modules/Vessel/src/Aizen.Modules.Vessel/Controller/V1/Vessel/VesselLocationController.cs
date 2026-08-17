using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Request.Location;
using Aizen.Modules.Vessel.Abstraction.Response.Location;
using Aizen.Modules.Vessel.Application.Command.Location;
using Aizen.Modules.Vessel.Application.Query.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("current")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetCurrentVesselLocationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetCurrentVesselLocationResponse?>> GetCurrent([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetCurrentVesselLocationResponse>(new GetCurrentVesselLocationQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UpdateVesselLocationSnapshotResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselLocationSnapshotResponse?>> Update([FromRoute] long vesselId, [FromBody] UpdateVesselLocationSnapshotRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselLocationSnapshotResponse>(new UpdateVesselLocationSnapshotCommand(vesselId, req), ct);
        return SetResponse(result);
    }
}
