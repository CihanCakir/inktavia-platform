using Aizen.Modules.Identity.Domain.Model.PasswordRecovery;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

/// <summary>
/// Owns the provider password recovery domain: request/verify/reset/resend. Result models live in
/// <c>Aizen.Modules.Identity.Domain.Model.PasswordRecovery</c> (Application handlers map them to the Abstraction
/// response DTOs).
/// </summary>
public interface IProviderPasswordRecoveryDomainService
{
    Task<PasswordRecoveryRequestResult> RequestAsync(string channel, string identifier, CancellationToken ct);
    Task<PasswordRecoveryVerifyResult> VerifyOtpAsync(string resetRequestId, string otpCode, CancellationToken ct);
    Task<PasswordRecoveryResetResult> ResetAsync(string resetToken, string newPassword, CancellationToken ct);
    Task<PasswordRecoveryResendResult> ResendAsync(string resetRequestId, CancellationToken ct);
}
