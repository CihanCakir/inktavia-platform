using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetActiveParticipantSubscription;

[DocumentationInfo("Get active participant subscription query handler",
    "Returns the active ParticipantPlanSubscription for the given participant, or null if no active subscription exists.")]
public sealed class GetActiveParticipantSubscriptionQueryHandler
    : AizenQueryHandler<GetActiveParticipantSubscriptionQuery, ActiveParticipantSubscriptionResult?>
{
    private readonly IParticipantPlanRepository _plans;

    public GetActiveParticipantSubscriptionQueryHandler(IParticipantPlanRepository plans)
        => _plans = plans;

    public override async Task<ActiveParticipantSubscriptionResult?> Handle(
        GetActiveParticipantSubscriptionQuery request, CancellationToken ct)
    {
        var subscription = await _plans.GetActiveSubscriptionAsync(
            request.ParticipantProfileId, DateTime.UtcNow, ct);

        if (subscription is null) return null;

        var plan = await _plans.GetByIdAsync(subscription.ParticipantPlanId, ct);

        return new ActiveParticipantSubscriptionResult(
            SubscriptionId:               subscription.Id,
            ParticipantPlanId:            subscription.ParticipantPlanId,
            PlanCode:                     plan?.PlanCode ?? string.Empty,
            Status:                       subscription.Status,
            PaidAmount:                   subscription.PaidAmount,
            CurrencyCode:                 subscription.CurrencyCode,
            PeriodStart:                  subscription.SubscriptionPeriodStart,
            PeriodEnd:                    subscription.SubscriptionPeriodEnd,
            AutoRenew:                    subscription.AutoRenew,
            ServiceDiscountAtSubscription: subscription.ServiceDiscountAtSubscription,
            EarnMultiplierAtSubscription:  subscription.EarnMultiplierAtSubscription);
    }
}
