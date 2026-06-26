using Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;
using Aizen.Bff.AdminPanel.Application.AdminIdentity.Dto;
using Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Identity")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class IdentityController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public IdentityController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("identity/profiles")]
    [ProducesResponseType(typeof(AdminUserOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminUserOverviewResponse>> GetProfiles(
        [FromQuery] string? roleContext,
        [FromQuery] string? approvalStatus,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminProfilesQuery(roleContext, approvalStatus, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(ProfileDetailResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileDetailResult>> GetProfileDetail(
        Guid profileId,
        [FromQuery] string roleContext = "General",
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminProfileDetailQuery(profileId, roleContext), ct);
        return SetResponse(result);
    }

    [HttpPost("identity/organizers/{userId:long}/profiles/{profileId:guid}/approve")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> ApproveOrganizerProfile(
        long userId, Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveOrganizerProfileCommand(userId, profileId), ct);
        return SetResponse(result);
    }

    [HttpPost("identity/organizers/{userId:long}/profiles/{profileId:guid}/reject")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> RejectOrganizerProfile(
        long userId, Guid profileId, [FromBody] AdminBffRejectRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectOrganizerProfileCommand(userId, profileId, request.Reason), ct);
        return SetResponse(result);
    }

    [HttpPost("identity/venues/{userId:long}/profiles/{profileId:guid}/approve")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> ApproveVenueProfile(
        long userId, Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveVenueProfileCommand(userId, profileId), ct);
        return SetResponse(result);
    }

    [HttpPost("identity/venues/{userId:long}/profiles/{profileId:guid}/reject")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> RejectVenueProfile(
        long userId, Guid profileId, [FromBody] AdminBffRejectRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectVenueProfileCommand(userId, profileId, request.Reason), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/profiles/{profileId:guid}/with-roles")]
    [ProducesResponseType(typeof(ProfileWithRolesResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileWithRolesResult>> GetProfileWithRoles(
        Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetProfileWithRolesQuery(profileId), ct);
        return SetResponse(result);
    }
}
