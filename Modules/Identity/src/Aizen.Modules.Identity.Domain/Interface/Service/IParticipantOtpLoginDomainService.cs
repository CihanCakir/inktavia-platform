using Aizen.Modules.Identity.Domain.Model.OtpLogin;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IParticipantOtpLoginDomainService
{
    Task<OtpLoginRequestResult> RequestAsync(string channel, string identifier, CancellationToken ct);

    /// <summary>
    /// Resolve a login identifier (email|phone) to the canonical participant (Keycloak username/email + subject)
    /// WITHOUT sending an OTP — reuses the exact same lookup + participant gate as <see cref="RequestAsync"/>.
    /// Used by phone password-login to turn a phone into the email the Keycloak ROPC needs. Null when unresolved.
    /// </summary>
    Task<ParticipantIdentifierResolution?> ResolveByIdentifierAsync(string channel, string identifier, CancellationToken ct);
    Task<OtpLoginVerifyResult> VerifyOtpAsync(string loginRequestId, string otpCode, CancellationToken ct);
    Task<OtpLoginResendResult> ResendAsync(string loginRequestId, CancellationToken ct);
}
