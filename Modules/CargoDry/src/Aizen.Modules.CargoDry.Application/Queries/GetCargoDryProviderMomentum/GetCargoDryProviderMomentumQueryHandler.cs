using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderMomentum;

public sealed class GetCargoDryProviderMomentumQueryHandler
    : AizenQueryHandler<GetCargoDryProviderMomentumQuery, CargoDryProviderMomentumDto>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;

    public GetCargoDryProviderMomentumQueryHandler(ICargoDrySalesAttributionRepository attributions)
        => _attributions = attributions;

    public override async Task<CargoDryProviderMomentumDto?> Handle(
        GetCargoDryProviderMomentumQuery request, CancellationToken ct)
    {
        var months = Math.Clamp(request.LookbackMonths, 3, 60);
        var now = DateTimeOffset.UtcNow;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var windowStart = currentMonthStart.AddMonths(-(months - 1));
        var windowEnd   = currentMonthStart.AddMonths(1);

        var active = await _attributions.GetProviderActiveSalesMonthsAsync(
            request.ProviderProfileId, windowStart, windowEnd, ct);

        // Build ordered month keys (oldest → newest)
        var keys = new string[months];
        for (var i = 0; i < months; i++)
        {
            var m = currentMonthStart.AddMonths(-(months - 1) + i);
            keys[i] = m.ToString("yyyy-MM");
        }

        var currentKey = currentMonthStart.ToString("yyyy-MM");
        var activeThisMonth = active.Contains(currentKey);

        // ── currentStreak: count backwards from starting point ───────────────
        // If this month is active, start from this month; otherwise start from previous month
        // (in-progress month doesn't break the streak until it ends).
        var startIdx = activeThisMonth ? months - 1 : months - 2;
        var currentStreak = 0;
        if (startIdx >= 0 && active.Contains(keys[startIdx]))
        {
            for (var i = startIdx; i >= 0; i--)
            {
                if (!active.Contains(keys[i])) break;
                currentStreak++;
            }
        }

        // ── bestStreak: longest consecutive active run in the window ─────────
        var bestStreak = 0;
        var run = 0;
        for (var i = 0; i < months; i++)
        {
            if (active.Contains(keys[i]))
            {
                run++;
                if (run > bestStreak) bestStreak = run;
            }
            else
            {
                run = 0;
            }
        }

        return new CargoDryProviderMomentumDto
        {
            CurrentStreakMonths = currentStreak,
            BestStreakMonths    = bestStreak,
            ActiveThisMonth    = activeThisMonth,
        };
    }
}
