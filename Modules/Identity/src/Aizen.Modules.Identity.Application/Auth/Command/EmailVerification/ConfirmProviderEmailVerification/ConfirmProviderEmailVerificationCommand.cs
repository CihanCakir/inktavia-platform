using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConfirmProviderEmailVerification;

public sealed class ConfirmProviderEmailVerificationCommand
    : AizenCommand<ConfirmProviderEmailVerificationResponse>
{
    public string Token { get; set; } = default!;
}
