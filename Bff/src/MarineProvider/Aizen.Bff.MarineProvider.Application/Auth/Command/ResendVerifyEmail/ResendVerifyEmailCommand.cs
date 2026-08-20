using Aizen.Bff.MarineProvider.Application.Contracts.Auth.EmailVerification;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class ResendVerifyEmailCommand : AizenCommand<ResendVerifyEmailResponse>
{
    public string Email { get; set; } = default!;
}
