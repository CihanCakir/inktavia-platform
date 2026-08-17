using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestProviderPasswordRecovery;

public sealed class RequestProviderPasswordRecoveryCommand
    : AizenCommand<RequestProviderPasswordRecoveryResponse>
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
