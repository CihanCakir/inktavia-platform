using Aizen.Bff.MarineProvider.Application.Contracts.Auth.EmailVerification;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class VerifyEmailCommand : AizenCommand<VerifyEmailResponse>
{
    public string Token { get; set; } = default!;
}
