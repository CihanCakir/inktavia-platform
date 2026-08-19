using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ResendProviderEmailVerification;

public sealed class ResendProviderEmailVerificationCommand
    : AizenCommand<ResendProviderEmailVerificationResponse>
{
    public string Email { get; set; } = default!;
}
