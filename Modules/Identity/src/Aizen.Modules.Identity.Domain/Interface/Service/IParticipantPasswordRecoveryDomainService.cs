using Aizen.Modules.Identity.Domain.Model.PasswordRecovery;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

/// <summary>
/// Owns the participant (mobile) password recovery domain: request/verify/reset/resend. Mirrors
/// <see cref="IProviderPasswordRecoveryDomainService"/> — only the profile gate (Participant) and the
/// persisted reset-request entity differ. Result models live in
/// <c>Aizen.Modules.Identity.Domain.Model.PasswordRecovery</c> (Application handlers map them to the
/// Abstraction response DTOs) and are shared with the provider vertical.
/// </summary>
public interface IParticipantPasswordRecoveryDomainService
{
    Task<PasswordRecoveryRequestResult> RequestAsync(string channel, string identifier, CancellationToken ct);
    Task<PasswordRecoveryVerifyResult> VerifyOtpAsync(string resetRequestId, string otpCode, CancellationToken ct);
    Task<PasswordRecoveryResetResult> ResetAsync(string resetToken, string newPassword, CancellationToken ct);
    Task<PasswordRecoveryResendResult> ResendAsync(string resetRequestId, CancellationToken ct);
}
