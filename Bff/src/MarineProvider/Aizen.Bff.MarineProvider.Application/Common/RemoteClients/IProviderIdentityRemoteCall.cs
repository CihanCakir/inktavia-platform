using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// BFF → Identity module calls. Auth (Keycloak service token) is injected by
/// <c>MarineProviderBffAuthDelegatingHandler</c>. All endpoints are service-token authorized
/// (Identity IdentityRead/IdentityWrite policies) — the provider-portal-bff service account
/// must hold the identity_read / identity_write client roles on identity-api.
/// </summary>
public interface IProviderIdentityRemoteCall : IAizenRemoteCall
{
    // Read the organizer (provider) profile by its profile id.
    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileById(long profileId);

    // Resolve the organizer profile linked to a Keycloak subject (404/null when unlinked).
    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/by-subject/{keycloakSubject}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileByKeycloakSubject(string keycloakSubject);

    // Idempotently provision/link an organizer profile for a Keycloak-authenticated user.
    [AizenRemoteCallPost("/api/v1/identity/organizers/provision-from-keycloak")]
    Task<AizenApiResponse<ProvisionOrganizerFromKeycloakResult>> ProvisionFromKeycloak(
        [AizenRemoteCallBody] ProviderProvisionFromKeycloakRequest request);

    // Persist phone verification on the organizer profile (called only after OTP success).
    [AizenRemoteCallPost("/api/v1/identity/organizers/profiles/{profileId}/phone/mark-verified")]
    Task<AizenApiResponse<ProviderMarkPhoneVerifiedResult>> MarkPhoneVerified(long profileId);

    // Existing phone OTP contracts (optional post-registration verification; not a login method).
    [AizenRemoteCallPost("/api/v1/auth/otp/send")]
    Task<AizenApiResponse<SendOtpDto>> SendOtp([AizenRemoteCallBody] ProviderSendOtpRequest request);

    [AizenRemoteCallPost("/api/v1/auth/otp/check")]
    Task<AizenApiResponse<CheckOtpDto>> CheckOtp([AizenRemoteCallBody] ProviderCheckOtpRequest request);

    // ── Password Recovery (delegated to Identity) ───────────────────────────

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/request")]
    Task<AizenApiResponse<RequestProviderPasswordRecoveryResponse>> RequestProviderPasswordRecovery(
        [AizenRemoteCallBody] RequestProviderPasswordRecoveryRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/verify-otp")]
    Task<AizenApiResponse<VerifyProviderPasswordRecoveryOtpResponse>> VerifyProviderPasswordRecoveryOtp(
        [AizenRemoteCallBody] VerifyProviderPasswordRecoveryOtpRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/reset")]
    Task<AizenApiResponse<ResetProviderPasswordResponse>> ResetProviderPassword(
        [AizenRemoteCallBody] ResetProviderPasswordRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/resend-otp")]
    Task<AizenApiResponse<ResendProviderPasswordRecoveryOtpResponse>> ResendProviderPasswordRecoveryOtp(
        [AizenRemoteCallBody] ResendProviderPasswordRecoveryOtpRequest request);
}

public sealed class ProviderProvisionFromKeycloakRequest
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactPhone { get; set; }
    public string? TaxNo { get; set; }
    public bool? EmailVerified { get; set; }
}

public sealed class ProviderMarkPhoneVerifiedResult
{
    public long ProfileId { get; set; }
    public bool PhoneVerified { get; set; }
}

public sealed class ProviderSendOtpRequest
{
    public string PhoneNumber { get; set; } = default!;
}

public sealed class ProviderCheckOtpRequest
{
    public string PhoneNumber { get; set; } = default!;
    public int Otp { get; set; }
    public string ValidationGuid { get; set; } = default!;
}
