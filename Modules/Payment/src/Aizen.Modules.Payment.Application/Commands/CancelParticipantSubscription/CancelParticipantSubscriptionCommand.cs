using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CancelParticipantSubscription;

/// <summary>
/// Cancels an active participant subscription. Status set to Cancelled immediately;
/// access to plan benefits remains until PeriodEnd (MVP — no pro-rata refund).
/// </summary>
public sealed class CancelParticipantSubscriptionCommand : AizenCommand<CancelSubscriptionResult>
{
    public required long    ParticipantProfileId  { get; init; }
    public          string? CancellationReason    { get; init; }
}
