using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.SubscribeParticipant;

public sealed class SubscribeParticipantBffCommand : AizenCommand<SubscribeParticipantBffCommandResponse>
{
    public required SubscribeParticipantPlanRequest Body { get; init; }
}

public sealed class SubscribeParticipantBffCommandResponse
{
    public SubscribeParticipantPlanResult Result { get; init; } = default!;
}
