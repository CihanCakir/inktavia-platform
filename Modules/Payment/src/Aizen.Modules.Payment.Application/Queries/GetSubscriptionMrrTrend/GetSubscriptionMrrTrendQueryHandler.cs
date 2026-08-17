using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionMrrTrend;

[DocumentationInfo("GetSubscriptionMrrTrendQueryHandler",
    "Returns monthly MRR totals for the last N months by merging provider and participant subscription revenue. " +
    "Used for the Revenue Velocity chart on the admin subscriptions dashboard.")]
public sealed class GetSubscriptionMrrTrendQueryHandler
    : AizenQueryHandler<GetSubscriptionMrrTrendQuery, SubscriptionMrrTrendResult>
{
    private readonly IProviderPlanRepository    _providerPlans;
    private readonly IParticipantPlanRepository _participantPlans;

    private static readonly string[] TurkishMonthLabels =
        ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"];

    public GetSubscriptionMrrTrendQueryHandler(
        IProviderPlanRepository    providerPlans,
        IParticipantPlanRepository participantPlans)
    {
        _providerPlans    = providerPlans;
        _participantPlans = participantPlans;
    }

    public override async Task<SubscriptionMrrTrendResult?> Handle(
        GetSubscriptionMrrTrendQuery request, CancellationToken ct)
    {
        var months = Math.Clamp(request.Months, 1, 24);
        var utcNow = DateTime.UtcNow;

        // Start from the first day of (months-1) months ago so current month is included
        var fromUtc = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(months - 1));

        var providerRows     = await _providerPlans.GetMonthlyPaidAmountAsync(fromUtc, ct);
        var participantRows  = await _participantPlans.GetMonthlyPaidAmountAsync(fromUtc, ct);

        // Merge into a single dictionary keyed by (Year, Month)
        var merged = new Dictionary<(int, int), decimal>();
        foreach (var (year, month, total) in providerRows)
        {
            merged[(year, month)] = merged.GetValueOrDefault((year, month)) + total;
        }
        foreach (var (year, month, total) in participantRows)
        {
            merged[(year, month)] = merged.GetValueOrDefault((year, month)) + total;
        }

        // Build the ordered result for the requested month range
        var result = new List<MrrMonthDto>();
        for (var i = 0; i < months; i++)
        {
            var date  = fromUtc.AddMonths(i);
            var key   = (date.Year, date.Month);
            var total = merged.GetValueOrDefault(key, 0m);
            var label = TurkishMonthLabels[date.Month - 1];
            if (i == months - 1 && date.Year == utcNow.Year && date.Month == utcNow.Month)
                label += "*"; // mark current month as projection
            result.Add(new MrrMonthDto(date.Year, date.Month, label, total));
        }

        return new SubscriptionMrrTrendResult(result);
    }
}
