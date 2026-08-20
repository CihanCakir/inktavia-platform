using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.GenerateProviderEmailVerification;

public sealed class GenerateProviderEmailVerificationCommand
    : AizenCommand<GenerateProviderEmailVerificationResponse>
{
    public string Email { get; set; } = default!;
}
