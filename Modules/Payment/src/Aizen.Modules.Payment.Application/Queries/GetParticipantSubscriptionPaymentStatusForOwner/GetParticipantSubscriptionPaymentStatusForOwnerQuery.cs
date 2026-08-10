using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantSubscriptionPaymentStatusForOwner;

/// <summary>
/// BE-MO7 — the owner-facing payment status of a paid participant-subscription checkout. Owner-scoped: the transaction
/// must be a Subscription-context transaction whose payer is the resolved participant, else a clean not-found (no
/// existence leak). Reuses the MO3 owner payment lifecycle so the FE polls it exactly like the accept→pay flow.
/// </summary>
public sealed class GetParticipantSubscriptionPaymentStatusForOwnerQuery : AizenQuery<ParticipantSubscriptionPaymentStatusResult>
{
    public required long ParticipantProfileId { get; init; }
    public required long TransactionId        { get; init; }
}
