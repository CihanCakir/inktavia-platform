using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.VerifyProviderEmailVerification;

public sealed class VerifyProviderEmailVerificationCommand
    : AizenCommand<VerifyProviderEmailVerificationResponse>
{
    public string Token { get; set; } = default!;
}
