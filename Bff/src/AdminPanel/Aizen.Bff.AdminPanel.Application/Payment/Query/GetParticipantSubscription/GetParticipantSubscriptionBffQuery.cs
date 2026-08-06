using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetParticipantSubscription;

public sealed class GetParticipantSubscriptionBffQuery : AizenQuery<GetParticipantSubscriptionBffResponse>
{
    public long ParticipantProfileId { get; init; }
}

public sealed class GetParticipantSubscriptionBffResponse
{
    public ActiveParticipantSubscriptionResult? Subscription             { get; init; }
    /// <summary>Participant full name — resolved from Identity module by BFF in parallel with subscription fetch.</summary>
    public string?                              ParticipantDisplayName   { get; init; }
    /// <summary>Participant avatar URL — resolved from Identity module by BFF.</summary>
    public string?                              ParticipantAvatarUrl     { get; init; }
}
