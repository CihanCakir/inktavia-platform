using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ResetProviderPassword;

/// <summary>Public: set a new password using a valid reset token (obtained from OTP verification).</summary>
public sealed class ResetProviderPasswordCommand : AizenCommand<ResetProviderPasswordResponse>
{
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}
