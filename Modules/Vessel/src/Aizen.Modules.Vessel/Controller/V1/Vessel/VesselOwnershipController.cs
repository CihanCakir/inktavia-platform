using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Ownership;
using Aizen.Modules.Vessel.Application.Command.Ownership;
using Aizen.Modules.Vessel.Application.Query.Ownership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/owners")]
[Tags("Vessel - Ownership")]
[Authorize]
[DocumentationInfo("Vessel ownership endpoints", "Manage vessel owners, roles and invitations.")]
public sealed class VesselOwnershipController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselOwnershipController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IPaginate<VesselOwnerDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<VesselOwnerDto>?>> GetOwners(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IPaginate<VesselOwnerDto>>(new GetVesselOwnersQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselOwnerDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselOwnerDto?>> Add([FromRoute] long vesselId, [FromBody] AddVesselOwnerRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselOwnerDto>(new AddVesselOwnerCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{ownerId:long}/role")]
    [ProducesResponseType(typeof(VesselOwnerDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselOwnerDto?>> UpdateRole([FromRoute] long vesselId, [FromRoute] long ownerId, [FromBody] UpdateVesselOwnerRoleRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselOwnerDto>(new UpdateVesselOwnerRoleCommand(vesselId, ownerId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{ownerId:long}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, [FromRoute] long ownerId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselOwnerCommand(vesselId, ownerId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{ownerId:long}/set-primary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> SetPrimary([FromRoute] long vesselId, [FromRoute] long ownerId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new SetPrimaryVesselOwnerCommand(vesselId, ownerId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("accept-invitation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> AcceptInvitation([FromRoute] long vesselId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new AcceptVesselOwnershipInvitationCommand(vesselId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("reject-invitation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> RejectInvitation([FromRoute] long vesselId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RejectVesselOwnershipInvitationCommand(vesselId), ct);
        return SetResponse<object>(new { success = true });
    }
}
