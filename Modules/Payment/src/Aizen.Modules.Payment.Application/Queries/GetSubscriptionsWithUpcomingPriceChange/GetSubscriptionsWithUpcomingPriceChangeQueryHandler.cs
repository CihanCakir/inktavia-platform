using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionsWithUpcomingPriceChange;

[DocumentationInfo("GetSubscriptionsWithUpcomingPriceChangeQueryHandler",
    "Returns active auto-renewing subscriptions whose renewal-date price differs from the current snapshot, " +
    "renewing within N days. Data source for Notification N1 — sends nothing.")]
public sealed class GetSubscriptionsWithUpcomingPriceChangeQueryHandler
    : AizenQueryHandler<GetSubscriptionsWithUpcomingPriceChangeQuery, List<UpcomingPriceChangeItem>>
{
    private readonly IProviderPlanRepository      _plans;
    private readonly IProviderPlanPriceRepository _prices;

    public GetSubscriptionsWithUpcomingPriceChangeQueryHandler(
        IProviderPlanRepository plans, IProviderPlanPriceRepository prices)
    {
        _plans  = plans;
        _prices = prices;
    }

    public override async Task<List<UpcomingPriceChangeItem>?> Handle(
        GetSubscriptionsWithUpcomingPriceChangeQuery request, CancellationToken ct)
    {
        var now   = DateTime.UtcNow;
        var until = now.AddDays(Math.Max(0, request.WithinDays));

        var subs = await _plans.GetActiveSubscriptionsRenewingBetweenAsync(now, until, ct);

        var items = new List<UpcomingPriceChangeItem>();
        foreach (var sub in subs)
        {
            // Price active at the renewal instant (§13.2) — resolved, not the original snapshot.
            var upcoming = await _prices.ResolveRenewalPriceAsync(sub, sub.SubscriptionPeriodEnd, ct);
            if (upcoming is null) continue;

            if (upcoming.PriceAmount != sub.PaidAmount)
                items.Add(new UpcomingPriceChangeItem(
                    SubscriptionId:      sub.Id,
                    ProviderProfileId:   sub.ProviderProfileId,
                    ProviderPlanId:      sub.ProviderPlanId,
                    CurrencyCode:        sub.CurrencyCode,
                    CurrentPaidAmount:   sub.PaidAmount,
                    UpcomingPriceAmount: upcoming.PriceAmount,
                    RenewalDateUtc:      sub.SubscriptionPeriodEnd));
        }

        return items;
    }
}
