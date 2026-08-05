using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestParticipantPasswordRecovery;

public sealed class RequestParticipantPasswordRecoveryCommand
    : AizenCommand<RequestProviderPasswordRecoveryResponse>
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
