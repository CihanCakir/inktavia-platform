using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConsumeProviderEmailVerification;

public sealed class ConsumeProviderEmailVerificationCommand
    : AizenCommand<ConsumeProviderEmailVerificationResponse>
{
    public string Token { get; set; } = default!;
}
