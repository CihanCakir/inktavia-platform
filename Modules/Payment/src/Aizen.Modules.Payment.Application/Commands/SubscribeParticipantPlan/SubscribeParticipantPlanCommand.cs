using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.SubscribeParticipantPlan;

/// <summary>
/// Subscribes a participant to a plan for one billing period.
/// Creates a ParticipantPlanSubscriptionEntity with Active status.
///
/// Discount and multiplier snapshots are read from the plan at subscription time
/// and stored immutably in the subscription record.
/// </summary>
public sealed class SubscribeParticipantPlanCommand : AizenCommand<SubscribeParticipantPlanResult>
{
    public required long     ParticipantProfileId  { get; init; }
    public required long     ParticipantPlanId     { get; init; }
    public required decimal  PaidAmount            { get; init; }
    public required string   CurrencyCode          { get; init; }
    public required DateTime PeriodStart           { get; init; }
    public required DateTime PeriodEnd             { get; init; }
    public bool              AutoRenew             { get; init; } = false;
    public long?             PaymentTransactionId  { get; init; }
}
