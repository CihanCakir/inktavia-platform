using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → Identity module calls. Auth (Keycloak service token) is injected by
/// <c>MarineMobileBffAuthDelegatingHandler</c>. All endpoints are service-token authorized
/// (Identity IdentityRead/IdentityWrite policies) — the marine-mobile-bff service account
/// must hold the identity_read / identity_write client roles on identity-api.
/// </summary>
public interface IIdentityRemoteCall : IAizenRemoteCall
{
    // Read the organizer (participant) profile by its profile id.
    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/{profileId}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileById(long profileId);

    // Resolve the organizer profile linked to a Keycloak subject (404/null when unlinked).
    [AizenRemoteCallGet("/api/v1/identity/organizers/profiles/by-subject/{keycloakSubject}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileByKeycloakSubject(string keycloakSubject);

    // Idempotently provision/link an organizer profile for a Keycloak-authenticated user.
    [AizenRemoteCallPost("/api/v1/identity/organizers/provision-from-keycloak")]
    Task<AizenApiResponse<ProvisionOrganizerFromKeycloakResult>> ProvisionFromKeycloak(
        [AizenRemoteCallBody] ParticipantProvisionFromKeycloakRequest request);

    // Persist phone verification on the organizer profile (called only after OTP success).
    [AizenRemoteCallPost("/api/v1/identity/organizers/profiles/{profileId}/phone/mark-verified")]
    Task<AizenApiResponse<ParticipantMarkPhoneVerifiedResult>> MarkPhoneVerified(long profileId);

    // Existing phone OTP contracts (optional post-registration verification; not a login method).
    [AizenRemoteCallPost("/api/v1/auth/otp/send")]
    Task<AizenApiResponse<SendOtpDto>> SendOtp([AizenRemoteCallBody] ParticipantSendOtpRequest request);

    [AizenRemoteCallPost("/api/v1/auth/otp/check")]
    Task<AizenApiResponse<CheckOtpDto>> CheckOtp([AizenRemoteCallBody] ParticipantCheckOtpRequest request);

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

    // ── Participant (mobile) OTP Login (M2a Identity routes; reuse the shared OTP-login DTOs) ─────

    [AizenRemoteCallPost("/api/v1/identity/auth/participant-otp-login/request")]
    Task<AizenApiResponse<RequestProviderOtpLoginResponse>> RequestParticipantOtpLogin(
        [AizenRemoteCallBody] RequestProviderOtpLoginRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/participant-otp-login/verify")]
    Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> VerifyParticipantOtpLogin(
        [AizenRemoteCallBody] VerifyProviderOtpLoginRequest request);

    [AizenRemoteCallPost("/api/v1/identity/auth/participant-otp-login/resend")]
    Task<AizenApiResponse<ResendProviderOtpLoginResponse>> ResendParticipantOtpLogin(
        [AizenRemoteCallBody] ResendProviderOtpLoginRequest request);

    // ── Participant provisioning + session-mint (M2e; reused by M2d social) ───────────────────────

    // Mint a single-use login_ticket for an already-authenticated participant subject (password/social).
    [AizenRemoteCallPost("/api/v1/identity/auth/participant-otp-login/mint-ticket")]
    Task<AizenApiResponse<Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.MintParticipantTicketResponse>> MintParticipantLoginTicket(
        [AizenRemoteCallBody] Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.MintParticipantTicketRequest request);

    // Idempotently provision/link a Participant profile for a Keycloak-authenticated user.
    [AizenRemoteCallPost("/api/v1/identity/participant/provision-from-keycloak")]
    Task<AizenApiResponse<Aizen.Modules.Identity.Abstraction.Dto.Participant.ProvisionParticipantFromKeycloakResult>> ProvisionParticipantFromKeycloak(
        [AizenRemoteCallBody] MobileParticipantProvisionRequest request);

    // Resolve the Participant profile linked to a Keycloak subject (404/null when unlinked).
    [AizenRemoteCallGet("/api/v1/identity/participant/profiles/by-subject/{keycloakSubject}")]
    Task<AizenApiResponse<OrganizerProfileDetailDto>> GetParticipantProfileByKeycloakSubject(string keycloakSubject);

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
    Task<AizenApiResponse<AttachParticipantDocumentResponse>> AttachParticipantDocument(
        long profileId,
        [AizenRemoteCallBody] AttachParticipantDocumentRequest request);

    [AizenRemoteCallDelete("/api/v1/identity/organizers/profiles/{profileId}/documents/{fileId}")]
    Task<AizenApiResponse<RemoveParticipantDocumentResponse>> RemoveParticipantDocument(
        long profileId,
        Guid fileId);
}

public sealed class ParticipantProvisionFromKeycloakRequest
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

/// <summary>Body for POST /api/v1/identity/participant/provision-from-keycloak (matches the Identity command).</summary>
public sealed class MobileParticipantProvisionRequest
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ContactPhone { get; set; }
    public bool? EmailVerified { get; set; }
}

public sealed class ParticipantMarkPhoneVerifiedResult
{
    public long ProfileId { get; set; }
    public bool PhoneVerified { get; set; }
}

public sealed class ParticipantSendOtpRequest
{
    public string PhoneNumber { get; set; } = default!;
}

public sealed class ParticipantCheckOtpRequest
{
    public string PhoneNumber { get; set; } = default!;
    public int Otp { get; set; }
    public string ValidationGuid { get; set; } = default!;
}

public sealed class AttachParticipantDocumentRequest
{
    public Guid FileId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string? Issuer { get; set; }
    public long UserId { get; set; }
}

public sealed class AttachParticipantDocumentResponse
{
    public long DocumentId { get; set; }
    public Guid FileId { get; set; }
    public bool Success { get; set; }
    public Aizen.Modules.Identity.Abstraction.Dto.Onboarding.ProviderDocumentDto? Document { get; set; }
}

public sealed class RemoveParticipantDocumentResponse
{
    public bool Success { get; set; }
}
