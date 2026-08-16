using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;
using Aizen.Modules.Identity.Application.AdminUsers.GetAdminUserIds;
using Aizen.Modules.Identity.Application.ParticipantLookup.GetParticipantProfileIdByUserId;
using Aizen.Modules.Identity.Application.ParticipantLookup.GetProfileContactEmail;
using Aizen.Modules.Identity.Application.ProviderEligibility.GetProviderAreaAvailability;
using Aizen.Modules.Identity.Application.ProviderEligibility.GetProvidersForArea;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
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
            new GetUserProfilesByFilterQuery(req.FirstName, req.LastName, req.RoleContext, req.ApprovalStatus, req.Status, req.Email, req.PageIndex, req.PageSize), ct);
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

    [HttpGet("admin/users/active-today-count")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.IdentityAdmin)]
    [ProducesResponseType(typeof(UserActiveTodayCountDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UserActiveTodayCountDto>> GetActiveTodayUserCount(CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetActiveTodayUserCountQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("admin/users/profiles/bulk")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.IdentityAdmin)]
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

    [HttpGet("admin/users/profiles/bulk-by-profile-ids")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.IdentityAdmin)]
    [ProducesResponseType(typeof(List<UserProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByProfileIds(
        [FromQuery] long[] profileIds,
        CancellationToken ct)
    {
        if (profileIds == null || profileIds.Length == 0)
            return SetResponse(new List<UserProfileListItemDto>());

        var result = await _sender.ProcessAsync(new GetUserProfilesByProfileIdsQuery(profileIds), ct);
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
            new GetOrganizerProfilesByFilterQuery(req.FirstName, req.LastName, req.ApprovalStatus, req.SearchTerm, req.Status, req.City, req.Country, req.OnboardingStatus, req.PageIndex, req.PageSize), ct);
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

    // ─── I2 provider-eligibility read-model (internal) ───────────────────────────
    // Returns active+approved provider profile/user ids operating in a city (+ optional category). Internal,
    // module-to-module read (called by the Notification worker for N-C region fan-out, and travel S4). Follows the
    // established internal-read pattern (ReferenceData LocationController): [AllowAnonymous] because these Aizen module
    // APIs have no public ingress — only the BFFs are admitted by the cluster NetworkPolicy. Returns ids only (no PII).
    [HttpGet("providers/for-area")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<ProviderForAreaDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IList<ProviderForAreaDto>>> GetProvidersForArea(
        [FromQuery] string cityCode,
        [FromQuery] string? categoryCode = null,
        [FromQuery] int take = 500,
        CancellationToken ct = default)
    {
        var result = await _sender.ProcessAsync(
            new GetProvidersForAreaQuery { CityCode = cityCode, CategoryCode = categoryCode, Take = take }, ct);
        return SetResponse(result);
    }

    // M2 — public COARSE availability for a (city, optional canonical category): available / limited / none. Same
    // internal-read pattern as providers/for-area ([AllowAnonymous] behind the cluster NetworkPolicy). Unlike
    // providers/for-area this returns ONLY the bucketed verdict — no count, no provider ids. The category is a
    // canonical SERVICE_PROVIDER_CATEGORY.Code; the read-model compares it against the stored lower(Code) form.
    [HttpGet("providers/for-area/availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProviderAreaAvailabilityDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderAreaAvailabilityDto>> GetProviderAreaAvailability(
        [FromQuery] string cityCode,
        [FromQuery] string? categoryCode = null,
        CancellationToken ct = default)
    {
        var result = await _sender.ProcessAsync(
            new GetProviderAreaAvailabilityQuery { CityCode = cityCode, CategoryCode = categoryCode }, ct);
        return SetResponse(result);
    }

    // N-D — internal read: numeric UserIds of admin users, for the support-request admin fan-out (ids only, no PII).
    // Same internal-read pattern as providers/for-area (anonymous behind the cluster NetworkPolicy).
    [HttpGet("admin/user-ids")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<long>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IList<long>>> GetAdminUserIds(CancellationToken ct)
    {
        var result = await _sender.ProcessAsync(new GetAdminUserIdsQuery(), ct);
        return SetResponse(result);
    }

    // BE_NF1b — internal read: participant USER id → participant PROFILE id, so the Notification module can file
    // owner-facing notifications under the profile id (where the owner inbox + device tokens resolve). Same
    // internal-read pattern as providers/for-area / admin/user-ids: [AllowAnonymous] behind the cluster NetworkPolicy —
    // notification-api's S2S remote calls carry no bearer token, so the IdentityRead/BFF-service-account policy is not
    // reachable here. Ids only, no PII.
    [HttpGet("participant/profile-id")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParticipantProfileIdDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ParticipantProfileIdDto>> GetParticipantProfileIdByUserId(
        [FromQuery] long userId, CancellationToken ct = default)
    {
        var result = await _sender.ProcessAsync(new GetParticipantProfileIdByUserIdQuery { UserId = userId }, ct);
        return SetResponse(result);
    }

    // BE_NF2 — internal read: a profile's contact email by UserProfiles.Id, so the Notification module can address an
    // Email-channel delivery to the same recipient the InApp notification is filed under. [AllowAnonymous] like the
    // other internal reads (notification-api's S2S calls carry no token). PRIVACY NOTE: unlike the ids-only endpoints,
    // this returns email (PII) — it stays internal behind the cluster NetworkPolicy; prod hardening (a notification-api
    // service token → IdentityRead) is a recommended follow-up.
    [HttpGet("profiles/contact-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfileContactEmailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileContactEmailDto>> GetProfileContactEmail(
        [FromQuery] long profileId, CancellationToken ct = default)
    {
        var result = await _sender.ProcessAsync(new GetProfileContactEmailByProfileIdQuery { ProfileId = profileId }, ct);
        return SetResponse(result);
    }
}
