using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionStats;

[DocumentationInfo("GetSubscriptionStatsQueryHandler",
    "Computes subscription management KPI strip: active provider + participant counts, " +
    "combined MRR (sum of PaidAmount for active subscriptions), " +
    "failed renewals this month (PastDue created this month), " +
    "expiring within 7 days (Active subscriptions nearing period end). " +
    "Queries both IProviderPlanRepository and IParticipantPlanRepository.")]
public sealed class GetSubscriptionStatsQueryHandler
    : AizenQueryHandler<GetSubscriptionStatsQuery, SubscriptionStatsDto>
{
    private readonly IProviderPlanRepository     _providerPlans;
    private readonly IParticipantPlanRepository  _participantPlans;

    public GetSubscriptionStatsQueryHandler(
        IProviderPlanRepository    providerPlans,
        IParticipantPlanRepository participantPlans)
    {
        _providerPlans    = providerPlans;
        _participantPlans = participantPlans;
    }

    public override async Task<SubscriptionStatsDto?> Handle(
        GetSubscriptionStatsQuery request, CancellationToken ct)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var providerActive      = await _providerPlans.CountActiveSubscriptionsAsync(now, ct);
        var participantActive   = await _participantPlans.CountActiveSubscriptionsAsync(now, ct);

        var providerMrr         = await _providerPlans.SumActiveMrrAsync(now, ct);
        var participantMrr      = await _participantPlans.SumActiveMrrAsync(now, ct);

        var providerFailed      = await _providerPlans.CountPastDueThisMonthAsync(monthStart, monthEnd, ct);
        var participantFailed   = await _participantPlans.CountPastDueThisMonthAsync(monthStart, monthEnd, ct);

        var providerExpiring    = await _providerPlans.CountExpiringSoonAsync(now, 7, ct);
        var participantExpiring = await _participantPlans.CountExpiringSoonAsync(now, 7, ct);

        return new SubscriptionStatsDto(
            ActiveProviderSubscriptions:     providerActive,
            ActiveParticipantSubscriptions:  participantActive,
            MonthlyRecurringRevenue:         providerMrr + participantMrr,
            FailedRenewalsThisMonth:         providerFailed + participantFailed,
            ExpiringSoonCount:               providerExpiring + participantExpiring
        );
    }
}
