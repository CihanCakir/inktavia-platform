using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Identity admin BFF remote call",
    "Defines synchronous BFF-to-Identity calls for admin profile management and search. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IIdentityRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/identity/profiles")]
    Task<AizenApiResponse<PagedProfileListResult>> SearchProfiles(
        [Refit.Query] string? firstName      = null,
        [Refit.Query] string? lastName       = null,
        [Refit.Query] string? roleContext     = null,
        [Refit.Query] string? approvalStatus  = null,
        [Refit.Query] string? status          = null,
        [Refit.Query] string? email           = null,
        [Refit.Query] int     pageIndex       = 0,
        [Refit.Query] int     pageSize        = 20);

    // Generic Keycloak subject → Identity numeric user id (service-token authorized, IdentityRead). Used to resolve
    // the acting admin's numeric user id for the BFF identity assertion — mirrors the provider's by-subject lookup.
    [AizenRemoteCallGet("/api/v1/identity/users/by-subject/{keycloakSubject}")]
    Task<AizenApiResponse<UserBySubjectDto>> GetUserByKeycloakSubject(string keycloakSubject);

    [AizenRemoteCallGet("/api/v1/identity/profiles/{profileId}")]
    Task<AizenApiResponse<ProfileDetailResult>> GetProfileById(Guid profileId);

    [AizenRemoteCallGet("/api/v1/identity/profiles/{profileId}/with-roles")]
    Task<AizenApiResponse<ProfileWithRolesResult>> GetProfileWithRoles(Guid profileId);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles")]
    Task<AizenApiResponse<PagedOrganizerProfileResult>> SearchOrganizerProfiles(
        [Refit.Query] int     pageIndex         = 0,
        [Refit.Query] int     pageSize          = 20,
        [Refit.Query] string? searchTerm        = null,
        [Refit.Query] string? approvalStatus    = null,
        [Refit.Query] string? onboardingStatus  = null,
        [Refit.Query] string? status            = null,
        [Refit.Query] string? city              = null,
        [Refit.Query] string? country           = null);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}")]
    Task<AizenApiResponse<OrganizerProfileResult>> GetOrganizerProfileById(Guid profileId);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}/with-user")]
    Task<AizenApiResponse<OrganizerProfileWithUserResult>> GetOrganizerProfileWithUser(Guid profileId);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles")]
    Task<AizenApiResponse<PagedVenueProfileResult>> SearchVenueProfiles(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles/{profileId}")]
    Task<AizenApiResponse<VenueProfileResult>> GetVenueProfileById(Guid profileId);

    [AizenRemoteCallGet("/api/v1/identity/participant/profiles")]
    Task<AizenApiResponse<PagedParticipantProfileResult>> SearchParticipantProfiles(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/identity/participant/profiles/{profileId}")]
    Task<AizenApiResponse<ParticipantProfileResult>> GetParticipantProfileById(Guid profileId);

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve")]
    Task<AizenApiResponse<EmptyResult>> ApproveOrganizerProfile(long userId, Guid profileId);

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject")]
    Task<AizenApiResponse<EmptyResult>> RejectOrganizerProfile(
        long userId,
        Guid profileId,
        [AizenRemoteCallBody] RejectProfileRequest request);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve")]
    Task<AizenApiResponse<EmptyResult>> ApproveVenueProfile(long userId, Guid profileId);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject")]
    Task<AizenApiResponse<EmptyResult>> RejectVenueProfile(
        long userId,
        Guid profileId,
        [AizenRemoteCallBody] RejectProfileRequest request);

    [AizenRemoteCallPost("/api/v1/auth/login/username")]
    Task<AizenApiResponse<UserLoginResponse>> LoginWithUsername(
        [AizenRemoteCallBody] LoginWithUsernameRequest request);

    [AizenRemoteCallPost("/api/v1/auth/login/phone")]
    Task<AizenApiResponse<UserLoginResponse>> LoginWithPhone(
        [AizenRemoteCallBody] LoginWithPhoneRequest request);

    [AizenRemoteCallPost("/api/v1/auth/login/otp")]
    Task<AizenApiResponse<UserLoginResponse>> LoginWithOtp(
        [AizenRemoteCallBody] LoginWithOtpRequest request);

    [AizenRemoteCallPost("/api/v1/auth/otp/send")]
    Task<AizenApiResponse<SendOtpDto>> SendOtp(
        [AizenRemoteCallBody] SendOtpRequest request);

    [AizenRemoteCallPost("/api/v1/auth/otp/check")]
    Task<AizenApiResponse<CheckOtpDto>> CheckOtp(
        [AizenRemoteCallBody] CheckOtpRequest request);

    [AizenRemoteCallPost("/api/v1/auth/refresh")]
    Task<AizenApiResponse<UserLoginResponse>> Refresh(
        [AizenRemoteCallBody] RefreshLoginHttpRequest request);

    [AizenRemoteCallPost("/api/v1/auth/password/change")]
    Task<AizenApiResponse<ChangePasswordDto>> ChangePassword(
        [AizenRemoteCallBody] ChangePasswordRequest request);

    // ── Admin OTP → Keycloak login (delegated to Identity; mirrors provider-otp-login) ──
    // Mints a Keycloak token with the "Admin" realm role and returns a LoginTicket for the handoff — NOT an HS256 token.
    [AizenRemoteCallPost("/api/v1/identity/auth/admin-otp-login/request")]
    Task<AizenApiResponse<RequestProviderOtpLoginResponse>> RequestAdminOtpLogin(
        [AizenRemoteCallBody] RequestProviderOtpLoginRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/admin-otp-login/verify")]
    Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> VerifyAdminOtpLogin(
        [AizenRemoteCallBody] VerifyProviderOtpLoginRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/admin-otp-login/resend")]
    Task<AizenApiResponse<ResendProviderOtpLoginResponse>> ResendAdminOtpLogin(
        [AizenRemoteCallBody] ResendProviderOtpLoginRequest request);

    [AizenRemoteCallGet("/api/v1/identity/admin/users/profiles/bulk")]
    Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByUserIds(
        [Refit.Query(CollectionFormat.Multi)] long[] userIds);

    [AizenRemoteCallGet("/api/v1/identity/admin/users/profiles/bulk-by-profile-ids")]
    Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByProfileIds(
        [Refit.Query(CollectionFormat.Multi)] long[] profileIds);

    [AizenRemoteCallGet("/api/v1/identity/profiles/{profileId}")]
    Task<AizenApiResponse<UserProfileDetailDto>> GetAdminUserProfileDetail(long profileId);

    [AizenRemoteCallGet("/api/v1/identity/admin/users/active-today-count")]
    Task<AizenApiResponse<UserActiveTodayCountDto>> GetActiveTodayUserCount();

    [AizenRemoteCallGet("/api/v1/identity/admin/users/{userId}/login-history")]
    Task<AizenApiResponse<List<UserLoginHistoryItemDto>>> GetUserLoginHistory(
        long userId,
        [Refit.Query] int pageSize = 50);

    // ── Admin Profile Approval Queue ──────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles")]
    Task<AizenApiResponse<OrganizerProfilePagedAdminResult>> GetAdminOrganizerProfilesByStatus(
        [Refit.Query] string? approvalStatus  = null,
        [Refit.Query] string? onboardingStatus = null,
        [Refit.Query] int     pageIndex       = 0,
        [Refit.Query] int     pageSize        = 100);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetAdminOrganizerProfileOnly(long profileId);

    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}/with-user")]
    Task<AizenApiResponse<OrganizerProfileWithUserDetailDto>> GetAdminOrganizerProfileWithUser(long profileId);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles")]
    Task<AizenApiResponse<VenueProfilePagedAdminResult>> GetAdminVenueProfilesByStatus(
        [Refit.Query] string? approvalStatus = null,
        [Refit.Query] int     pageIndex      = 0,
        [Refit.Query] int     pageSize       = 100);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles/{profileId}")]
    Task<AizenApiResponse<VenueProfileDetailDto>> GetAdminVenueProfileOnly(long profileId);

    [AizenRemoteCallGet("/api/v1/identity/venues/profiles/{profileId}/with-user")]
    Task<AizenApiResponse<VenueProfileWithUserDetailDto>> GetAdminVenueProfileWithUser(long profileId);

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve")]
    Task<AizenApiResponse<EmptyResult>> ApproveOrganizerProfileAdmin(long userId, long profileId);

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject")]
    Task<AizenApiResponse<EmptyResult>> RejectOrganizerProfileAdmin(
        long userId,
        long profileId,
        [AizenRemoteCallBody] RejectProfileRequest request);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve")]
    Task<AizenApiResponse<EmptyResult>> ApproveVenueProfileAdmin(long userId, long profileId);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject")]
    Task<AizenApiResponse<EmptyResult>> RejectVenueProfileAdmin(
        long userId,
        long profileId,
        [AizenRemoteCallBody] RejectProfileRequest request);

    // ── Provider Onboarding (admin read + revision) ───────────────────────────

    /// <summary>
    /// GET /api/v1/identity/provider-onboarding/{profileId}
    /// Fetches provider onboarding state (step statuses, draft data, documents) for admin review.
    /// </summary>
    [AizenRemoteCallGet("/api/v1/identity/provider-onboarding/{profileId}")]
    Task<AizenApiResponse<ProviderOnboardingResponse>> GetProviderOnboardingAdmin(long profileId);

    /// <summary>
    /// POST /api/v1/identity/provider-onboarding/{profileId}/revision
    /// Admin requests the provider to revise specific onboarding steps.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/identity/provider-onboarding/{profileId}/revision")]
    Task<AizenApiResponse<RequestProviderOnboardingRevisionResponse>> RequestProviderOnboardingRevisionAdmin(
        long profileId,
        [AizenRemoteCallBody] RequestProviderOnboardingRevisionRequest request);

    // ── Verification Document Registration ───────────────────────────────────

    [AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/documents")]
    Task<AizenApiResponse<RegisterVerificationDocumentResult>> RegisterOrganizerVerificationDocument(
        long userId,
        long profileId,
        [AizenRemoteCallBody] RegisterVerificationDocumentRequest request);

    [AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/documents")]
    Task<AizenApiResponse<RegisterVerificationDocumentResult>> RegisterVenueVerificationDocument(
        long userId,
        long profileId,
        [AizenRemoteCallBody] RegisterVerificationDocumentRequest request);
}

// Lightweight result wrappers for Identity HTTP responses
public sealed class PagedProfileListResult
{
    public List<UserProfileListItemDto>? Items { get; set; }
    public long Count { get; set; }
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
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

public sealed class OrganizerProfilePagedAdminResult
{
    public List<OrganizerProfileListItemDto>? Items { get; set; }
    public long Count { get; set; }
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

public sealed class VenueProfilePagedAdminResult
{
    public List<VenueProfileListItemDto>? Items { get; set; }
    public long Count { get; set; }
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

public sealed class RegisterVerificationDocumentRequest
{
    public string FileId { get; set; } = default!;
    public string DocumentType { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Format { get; set; }
    public string? FileSizeDisplay { get; set; }
    public string? Issuer { get; set; }
}

public sealed class RegisterVerificationDocumentResult
{
    public long DocumentId { get; set; }
    public string FileId { get; set; } = default!;
}
