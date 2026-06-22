using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Abstraction.Request.Common;
using Aizen.Modules.Identity.Abstraction.Request.Organizer;
using Aizen.Modules.Identity.Abstraction.Request.Participant;
using Aizen.Modules.Identity.Abstraction.Request.Venue;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

[ApiController]
[Route("api/v1/identity")]
[Tags("Identity - Query")]
public sealed class QueryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public QueryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _sender = cqrsProcessor;
    }

    // Common

    [HttpGet("profile/me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<UserProfileDetailDto>> GetCurrentUserProfile(CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetCurrentUserProfileDetailQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("profiles/{profileId:long}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(UserProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<UserProfileDetailDto>> GetUserProfileDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetUserProfileDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("profiles/{profileId:long}/with-roles")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(UserProfileWithRolesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<UserProfileWithRolesDto>> GetUserProfileWithRoles([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetUserProfileWithRolesQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("profiles")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IPaginate<UserProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<UserProfileListItemDto>>> GetUserProfilesByFilter([FromQuery] GetUserProfilesByFilterRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetUserProfilesByFilterQuery(req.FirstName, req.LastName, req.RoleContext, req.ApprovalStatus, req.PageIndex, req.PageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("profiles/list")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IList<UserProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IList<UserProfileListItemDto>>> GetUserProfileList([FromQuery] GetUserProfileListRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetUserProfileListQuery(req.FirstName, req.LastName, req.RoleContext, req.ApprovalStatus), ct);
        return SetResponse(result);
    }

    [HttpGet("admin/users/profiles/bulk")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(List<UserProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByUserIds(
        [FromQuery] long[] userIds,
        CancellationToken ct)
    {
        if (userIds == null || userIds.Length == 0)
            return SetResponse(new List<UserProfileListItemDto>());

        var result = await _sender.ProcessAsync(new GetUserProfilesByUserIdsQuery(userIds), ct);
        return SetResponse(result?.ToList() ?? new List<UserProfileListItemDto>());
    }

    // Participant

    [HttpGet("participant/profile/me")]
    [Authorize]
    [ProducesResponseType(typeof(ParticipantProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<ParticipantProfileDetailDto>> GetCurrentParticipantProfile(CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetCurrentParticipantProfileDetailQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("participant/profiles/{profileId:long}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(ParticipantProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<ParticipantProfileDetailDto>> GetParticipantProfileDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetParticipantProfileDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("participant/profiles/{profileId:long}/with-user")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(ParticipantProfileWithUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<ParticipantProfileWithUserDetailDto>> GetParticipantProfileWithUserDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetParticipantProfileWithUserDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("participant/profiles")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IPaginate<ParticipantProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<ParticipantProfileListItemDto>>> GetParticipantProfilesByFilter([FromQuery] GetParticipantProfilesByFilterRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetParticipantProfilesByFilterQuery(req.FirstName, req.LastName, req.ApprovalStatus, req.PageIndex, req.PageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("participant/profiles/list")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IList<ParticipantProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IList<ParticipantProfileListItemDto>>> GetParticipantProfileList([FromQuery] GetParticipantProfileListRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetParticipantProfileListQuery(req.FirstName, req.LastName, req.ApprovalStatus), ct);
        return SetResponse(result);
    }

    // Venue

    [HttpGet("venues/profile/me")]
    [Authorize]
    [ProducesResponseType(typeof(VenueProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<VenueProfileDetailDto>> GetCurrentVenueProfile(CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetCurrentVenueProfileDetailQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("venues/profiles/{profileId:long}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(VenueProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<VenueProfileDetailDto>> GetVenueProfileDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetVenueProfileDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("venues/profiles/{profileId:long}/with-user")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(VenueProfileWithUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<VenueProfileWithUserDetailDto>> GetVenueProfileWithUserDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetVenueProfileWithUserDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("venues/profiles")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IPaginate<VenueProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<VenueProfileListItemDto>>> GetVenueProfilesByFilter([FromQuery] GetVenueProfilesByFilterRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetVenueProfilesByFilterQuery(req.FirstName, req.LastName, req.ApprovalStatus, req.PageIndex, req.PageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("venues/profiles/list")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IList<VenueProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IList<VenueProfileListItemDto>>> GetVenueProfileList([FromQuery] GetVenueProfileListRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetVenueProfileListQuery(req.FirstName, req.LastName, req.ApprovalStatus), ct);
        return SetResponse(result);
    }

    // Organizer

    [HttpGet("organizers/profile/me")]
    [Authorize]
    [ProducesResponseType(typeof(OrganizerProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<OrganizerProfileDetailDto>> GetCurrentOrganizerProfile(CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetCurrentOrganizerProfileDetailQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("organizers/profiles/{profileId:long}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(OrganizerProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetOrganizerProfileDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("organizers/profiles/{profileId:long}/with-user")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(OrganizerProfileWithUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<OrganizerProfileWithUserDetailDto>> GetOrganizerProfileWithUserDetail([FromRoute] long profileId, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetOrganizerProfileWithUserDetailQuery(profileId), ct);
        return SetResponse(result);
    }

    [HttpGet("organizers/profiles")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IPaginate<OrganizerProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<OrganizerProfileListItemDto>>> GetOrganizerProfilesByFilter([FromQuery] GetOrganizerProfilesByFilterRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetOrganizerProfilesByFilterQuery(req.FirstName, req.LastName, req.ApprovalStatus, req.PageIndex, req.PageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("organizers/profiles/list")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(IList<OrganizerProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IList<OrganizerProfileListItemDto>>> GetOrganizerProfileList([FromQuery] GetOrganizerProfileListRequest req, CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(
            new GetOrganizerProfileListQuery(req.FirstName, req.LastName, req.ApprovalStatus), ct);
        return SetResponse(result);
    }
}
