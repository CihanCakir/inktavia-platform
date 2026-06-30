using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetActiveProviderSubscription;

[DocumentationInfo("Get active provider subscription query handler",
    "Returns the active ProviderPlanSubscription for the given provider, or null if no active subscription exists.")]
public sealed class GetActiveProviderSubscriptionQueryHandler
    : AizenQueryHandler<GetActiveProviderSubscriptionQuery, ActiveProviderSubscriptionResult?>
{
    private readonly IProviderPlanRepository _plans;

    public GetActiveProviderSubscriptionQueryHandler(IProviderPlanRepository plans)
        => _plans = plans;

    public override async Task<ActiveProviderSubscriptionResult?> Handle(
        GetActiveProviderSubscriptionQuery request, CancellationToken ct)
    {
        var subscription = await _plans.GetActiveSubscriptionAsync(
            request.ProviderProfileId, DateTime.UtcNow, ct);

        if (subscription is null) return null;

        var plan = await _plans.GetByIdAsync(subscription.ProviderPlanId, ct);

        return new ActiveProviderSubscriptionResult(
            SubscriptionId:              subscription.Id,
            ProviderPlanId:              subscription.ProviderPlanId,
            PlanCode:                    plan?.PlanCode ?? string.Empty,
            Status:                      subscription.Status,
            PaidAmount:                  subscription.PaidAmount,
            CurrencyCode:                subscription.CurrencyCode,
            PeriodStart:                 subscription.SubscriptionPeriodStart,
            PeriodEnd:                   subscription.SubscriptionPeriodEnd,
            AutoRenew:                   subscription.AutoRenew,
            CommissionRateAtSubscription: subscription.CommissionRateAtSubscription);
    }
}
