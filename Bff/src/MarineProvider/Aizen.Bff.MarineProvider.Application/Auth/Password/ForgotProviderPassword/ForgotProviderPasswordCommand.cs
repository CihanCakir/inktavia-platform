using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ForgotProviderPassword;

/// <summary>Public: request a password recovery code by email or phone. Generic (non-enumerating) response.</summary>
public sealed class ForgotProviderPasswordCommand : AizenCommand<ForgotProviderPasswordResponse>
{
    /// <summary>"email" or "phone".</summary>
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
