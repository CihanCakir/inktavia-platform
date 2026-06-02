using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Ownership;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;
using Aizen.Modules.Vessel.Application.Command.Ownership;
using Aizen.Modules.Vessel.Application.Query.Ownership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(GetVesselOwnersResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselOwnersResponse?>> GetOwners(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselOwnersResponse>(new GetVesselOwnersQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddVesselOwnerResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddVesselOwnerResponse?>> Add([FromRoute] long vesselId, [FromBody] AddVesselOwnerRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddVesselOwnerResponse>(new AddVesselOwnerCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{ownerId:long}/role")]
    [ProducesResponseType(typeof(UpdateVesselOwnerRoleResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselOwnerRoleResponse?>> UpdateRole([FromRoute] long vesselId, [FromRoute] long ownerId, [FromBody] UpdateVesselOwnerRoleRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselOwnerRoleResponse>(new UpdateVesselOwnerRoleCommand(vesselId, ownerId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{ownerId:long}")]
    [ProducesResponseType(typeof(RemoveVesselOwnerResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselOwnerResponse?>> Remove([FromRoute] long vesselId, [FromRoute] long ownerId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RemoveVesselOwnerResponse>(new RemoveVesselOwnerCommand(vesselId, ownerId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{ownerId:long}/set-primary")]
    [ProducesResponseType(typeof(SetPrimaryVesselOwnerResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SetPrimaryVesselOwnerResponse?>> SetPrimary([FromRoute] long vesselId, [FromRoute] long ownerId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SetPrimaryVesselOwnerResponse>(new SetPrimaryVesselOwnerCommand(vesselId, ownerId), ct);
        return SetResponse(result);
    }

    [HttpPatch("accept-invitation")]
    [ProducesResponseType(typeof(AcceptVesselOwnershipInvitationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AcceptVesselOwnershipInvitationResponse?>> AcceptInvitation([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AcceptVesselOwnershipInvitationResponse>(new AcceptVesselOwnershipInvitationCommand(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPatch("reject-invitation")]
    [ProducesResponseType(typeof(RejectVesselOwnershipInvitationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RejectVesselOwnershipInvitationResponse?>> RejectInvitation([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RejectVesselOwnershipInvitationResponse>(new RejectVesselOwnershipInvitationCommand(vesselId), ct);
        return SetResponse(result);
    }
}
