using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Identity admin BFF remote call", "Defines synchronous BFF-to-Identity calls for admin profile management and search.")]
public interface IIdentityAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/identity/profiles")]
    Task<AizenApiResponse<PagedProfileListResult>> SearchProfiles(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string? firstName = null,
        [Refit.Query] string? lastName = null,
        [Refit.Query] string? roleContext = null,
        [Refit.Query] string? approvalStatus = null,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/identity/profiles/{profileId}")]
    Task<AizenApiResponse<ProfileDetailResult>> GetProfileById(
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/profiles/{profileId}/with-roles")]
    Task<AizenApiResponse<ProfileWithRolesResult>> GetProfileWithRoles(
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles")]
    Task<AizenApiResponse<PagedOrganizerProfileResult>> SearchOrganizerProfiles(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}")]
    Task<AizenApiResponse<OrganizerProfileResult>> GetOrganizerProfileById(
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}/with-user")]
    Task<AizenApiResponse<OrganizerProfileWithUserResult>> GetOrganizerProfileWithUser(
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles")]
    Task<AizenApiResponse<PagedVenueProfileResult>> SearchVenueProfiles(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles/{profileId}")]
    Task<AizenApiResponse<VenueProfileResult>> GetVenueProfileById(
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/participant/profiles")]
    Task<AizenApiResponse<PagedParticipantProfileResult>> SearchParticipantProfiles(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/identity/participant/profiles/{profileId}")]
    Task<AizenApiResponse<ParticipantProfileResult>> GetParticipantProfileById(
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve")]
    Task<AizenApiResponse<EmptyResult>> ApproveOrganizerProfile(
        long userId,
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject")]
    Task<AizenApiResponse<EmptyResult>> RejectOrganizerProfile(
        long userId,
        Guid profileId,
        [AizenRemoteCallBody] RejectProfileRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve")]
    Task<AizenApiResponse<EmptyResult>> ApproveVenueProfile(
        long userId,
        Guid profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject")]
    Task<AizenApiResponse<EmptyResult>> RejectVenueProfile(
        long userId,
        Guid profileId,
        [AizenRemoteCallBody] RejectProfileRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/auth/login/username")]
    Task<AizenApiResponse<UserLoginResponse>> LoginWithUsername(
        [AizenRemoteCallBody] LoginWithUsernameRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/auth/login/phone")]
    Task<AizenApiResponse<UserLoginResponse>> LoginWithPhone(
        [AizenRemoteCallBody] LoginWithPhoneRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/auth/login/otp")]
    Task<AizenApiResponse<UserLoginResponse>> LoginWithOtp(
        [AizenRemoteCallBody] LoginWithOtpRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/auth/otp/send")]
    Task<AizenApiResponse<SendOtpDto>> SendOtp(
        [AizenRemoteCallBody] SendOtpRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/auth/otp/check")]
    Task<AizenApiResponse<CheckOtpDto>> CheckOtp(
        [AizenRemoteCallBody] CheckOtpRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/auth/refresh")]
    Task<AizenApiResponse<UserLoginResponse>> Refresh(
        [AizenRemoteCallBody] RefreshLoginHttpRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/auth/password/change")]
    Task<AizenApiResponse<ChangePasswordDto>> ChangePassword(
        [AizenRemoteCallBody] ChangePasswordRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/admin/users/profiles/bulk")]
    Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByUserIds(
        [Refit.Query(CollectionFormat.Multi)] long[] userIds,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/admin/users/profiles/bulk-by-profile-ids")]
    Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByProfileIds(
        [Refit.Query(CollectionFormat.Multi)] long[] profileIds,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/identity/profiles/{profileId}")]
    Task<AizenApiResponse<UserProfileDetailDto>> GetAdminUserProfileDetail(
        long profileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);
}

// Lightweight result wrappers for Identity HTTP responses
public sealed class PagedProfileListResult { public List<UserProfileListItemDto>? Items { get; set; } public int TotalCount { get; set; } }
public sealed class ProfileDetailResult { public UserProfileDetailDto? Profile { get; set; } }
public sealed class ProfileWithRolesResult { public UserProfileWithRolesDto? Profile { get; set; } }
public sealed class PagedOrganizerProfileResult { public List<OrganizerProfileListItemDto>? Items { get; set; } public int TotalCount { get; set; } }
public sealed class OrganizerProfileResult { public OrganizerProfileDetailDto? Profile { get; set; } }
public sealed class OrganizerProfileWithUserResult { public OrganizerProfileWithUserDetailDto? Profile { get; set; } }
public sealed class PagedVenueProfileResult { public List<VenueProfileListItemDto>? Items { get; set; } public int TotalCount { get; set; } }
public sealed class VenueProfileResult { public VenueProfileDetailDto? Profile { get; set; } }
public sealed class PagedParticipantProfileResult { public List<ParticipantProfileListItemDto>? Items { get; set; } public int TotalCount { get; set; } }
public sealed class ParticipantProfileResult { public ParticipantProfileDetailDto? Profile { get; set; } }
public sealed class EmptyResult { }
