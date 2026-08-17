using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetActiveParticipantSubscription;

/// <summary>Returns the currently active participant subscription, or null if none exists.</summary>
public sealed class GetActiveParticipantSubscriptionQuery
    : AizenQuery<ActiveParticipantSubscriptionResult?>
{
    public required long ParticipantProfileId { get; init; }
}
