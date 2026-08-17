using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionChurnRisk;

[DocumentationInfo("GetSubscriptionChurnRiskQueryHandler",
    "Returns churn risk signals across all subscription types. " +
    "Signals: (1) total PastDue subscriptions — payment failure risk; " +
    "(2) Active subscriptions expiring within 7 days — renewal risk. " +
    "Used for the Churn Risk panel on the admin subscriptions dashboard.")]
public sealed class GetSubscriptionChurnRiskQueryHandler
    : AizenQueryHandler<GetSubscriptionChurnRiskQuery, SubscriptionChurnRiskResult>
{
    private readonly IProviderPlanRepository    _providerPlans;
    private readonly IParticipantPlanRepository _participantPlans;

    public GetSubscriptionChurnRiskQueryHandler(
        IProviderPlanRepository    providerPlans,
        IParticipantPlanRepository participantPlans)
    {
        _providerPlans    = providerPlans;
        _participantPlans = participantPlans;
    }

    public override async Task<SubscriptionChurnRiskResult?> Handle(
        GetSubscriptionChurnRiskQuery request, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;

        var pastDueProvider      = await _providerPlans.CountTotalPastDueAsync(ct);
        var pastDueParticipant   = await _participantPlans.CountTotalPastDueAsync(ct);
        var expiringProvider     = await _providerPlans.CountExpiringSoonAsync(utcNow, 7, ct);
        var expiringParticipant  = await _participantPlans.CountExpiringSoonAsync(utcNow, 7, ct);

        var paymentFailureRisk = pastDueProvider + pastDueParticipant;
        var expiringIn7Days    = expiringProvider + expiringParticipant;

        return new SubscriptionChurnRiskResult(
            PaymentFailureRiskCount: paymentFailureRisk,
            ExpiringIn7DaysCount:    expiringIn7Days,
            TotalAtRiskCount:        paymentFailureRisk + expiringIn7Days
        );
    }
}
