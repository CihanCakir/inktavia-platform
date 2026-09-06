using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// BFF → Identity module calls. Auth (Keycloak service token) is injected by
/// <c>MarineProviderBffAuthDelegatingHandler</c>. All endpoints are service-token authorized
/// (Identity IdentityRead/IdentityWrite policies) — the provider-portal-bff service account
/// must hold the identity_read / identity_write client roles on identity-api.
/// </summary>
public interface IIdentityRemoteCall : IAizenRemoteCall
{
    // Read the organizer (provider) profile by its profile id.
    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileById(long profileId);

    // Resolve the organizer profile linked to a Keycloak subject (404/null when unlinked).
    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/by-subject/{keycloakSubject}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileByKeycloakSubject(string keycloakSubject);

    // Update the caller's organizer (provider) profile — the module resolves the profile from the asserted
    // UserInfo.UserId (never the body), so the BFF must resolve the provider first so the assertion is stamped.
    [AizenRemoteCallPut("/api/v1/identity/organizers/profile")]
    Task<AizenApiResponse<ProfileUpdateResult>> UpdateOrganizerProfile([AizenRemoteCallBody] UpdateOrganizerProfileRequest request);

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

    // ── OTP Login (delegated to Identity) ───────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-otp-login/request")]
    Task<AizenApiResponse<RequestProviderOtpLoginResponse>> RequestProviderOtpLogin(
        [AizenRemoteCallBody] RequestProviderOtpLoginRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-otp-login/verify")]
    Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> VerifyProviderOtpLogin(
        [AizenRemoteCallBody] VerifyProviderOtpLoginRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-otp-login/resend")]
    Task<AizenApiResponse<ResendProviderOtpLoginResponse>> ResendProviderOtpLogin(
        [AizenRemoteCallBody] ResendProviderOtpLoginRequest request);

    // ── E-posta doğrulama (delegated to Identity — ASP.NET Identity yerleşik onay token'ı) ──────

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-email-verification/generate")]
    Task<AizenApiResponse<GenerateProviderEmailVerificationResponse>> GenerateProviderEmailVerification(
        [AizenRemoteCallBody] GenerateProviderEmailVerificationRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-email-verification/confirm")]
    Task<AizenApiResponse<ConfirmProviderEmailVerificationResponse>> ConfirmProviderEmailVerification(
        [AizenRemoteCallBody] ConfirmProviderEmailVerificationRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/provider-email-verification/resend")]
    Task<AizenApiResponse<ResendProviderEmailVerificationResponse>> ResendProviderEmailVerification(
        [AizenRemoteCallBody] ResendProviderEmailVerificationRequest request);

    // ── Onboarding (delegated to Identity) ──────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/identity/provider-onboarding/{profileId}")]
    Task<AizenApiResponse<ProviderOnboardingResponse>> GetProviderOnboarding(long profileId);

    [AizenRemoteCallPut("/api/v1/identity/provider-onboarding/{profileId}/steps/{step}")]
    Task<AizenApiResponse<SaveProviderOnboardingStepResponse>> SaveProviderOnboardingStep(
        long profileId,
        string step,
        [AizenRemoteCallBody] SaveProviderOnboardingStepRequest request);

    [AizenRemoteCallPost("/api/v1/identity/provider-onboarding/{profileId}/submit")]
    Task<AizenApiResponse<SubmitProviderOnboardingResponse>> SubmitProviderOnboarding(long profileId);

    // ── Onboarding Documents ────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/identity/organizers/profiles/{profileId}/documents")]
    Task<AizenApiResponse<AttachProviderDocumentResponse>> AttachProviderDocument(
        long profileId,
        [AizenRemoteCallBody] AttachProviderDocumentRequest request);

    [AizenRemoteCallDelete("/api/v1/identity/organizers/profiles/{profileId}/documents/{fileId}")]
    Task<AizenApiResponse<RemoveProviderDocumentResponse>> RemoveProviderDocument(
        long profileId,
        Guid fileId);
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

public sealed class AttachProviderDocumentRequest
{
    public Guid FileId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string? Issuer { get; set; }
    public long UserId { get; set; }
}

public sealed class AttachProviderDocumentResponse
{
    public long DocumentId { get; set; }
    public Guid FileId { get; set; }
    public bool Success { get; set; }
    public Aizen.Modules.Identity.Abstraction.Dto.Onboarding.ProviderDocumentDto? Document { get; set; }
}

public sealed class RemoveProviderDocumentResponse
{
    public bool Success { get; set; }
}
