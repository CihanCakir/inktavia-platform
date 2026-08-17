using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CancelParticipantSubscription;

public sealed class CancelParticipantSubscriptionBffCommand : AizenCommand<CancelParticipantSubscriptionBffCommandResponse>
{
    public long    ParticipantProfileId { get; init; }
    public string? Reason               { get; init; }
}

public sealed class CancelParticipantSubscriptionBffCommandResponse
{
    public CancelSubscriptionResult Result { get; init; } = default!;
}
