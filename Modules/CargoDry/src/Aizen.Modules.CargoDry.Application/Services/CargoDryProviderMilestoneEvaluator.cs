using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

public sealed class CargoDryProviderMilestoneEvaluator : ICargoDryProviderMilestoneEvaluator
{
    private static readonly int[] StreakThresholds = [3, 6, 12];

    private readonly ICargoDrySalesAttributionRepository       _attributions;
    private readonly ICargoDryProviderMilestoneAwardRepository _awards;
    private readonly IAizenMessagePublisher                    _publisher;
    private readonly ILogger<CargoDryProviderMilestoneEvaluator> _logger;

    public CargoDryProviderMilestoneEvaluator(
        ICargoDrySalesAttributionRepository       attributions,
        ICargoDryProviderMilestoneAwardRepository awards,
        IAizenMessagePublisher                    publisher,
        ILogger<CargoDryProviderMilestoneEvaluator> logger)
    {
        _attributions = attributions;
        _awards       = awards;
        _publisher    = publisher;
        _logger       = logger;
    }

    public async Task EvaluateAfterSaleAsync(long providerProfileId, DateTimeOffset occurredAtUtc, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var currentMonthStart = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var endOfTime = DateTimeOffset.UtcNow.AddDays(1);

        // ── 1. FirstSale ─────────────────────────────────────────────────────────
        await TryAwardAsync(providerProfileId, "FirstSale", "ALL", null, nowUtc, ct);

        // ── 2. MonthlyTargetReached ──────────────────────────────────────────────
        var monthEnd = currentMonthStart.AddMonths(1);
        var thisMonthCommission = await _attributions.SumProviderCommissionAsync(
            providerProfileId, currentMonthStart, endOfTime, ct);

        // Compute target (same as CE-4 handler)
        var m1Start = currentMonthStart.AddMonths(-1);
        var m2Start = currentMonthStart.AddMonths(-2);
        var m3Start = currentMonthStart.AddMonths(-3);
        var m1 = await _attributions.SumProviderCommissionAsync(providerProfileId, m3Start, m2Start, ct);
        var m2 = await _attributions.SumProviderCommissionAsync(providerProfileId, m2Start, m1Start, ct);
        var m3 = await _attributions.SumProviderCommissionAsync(providerProfileId, m1Start, currentMonthStart, ct);
        var avg3 = (m1 + m2 + m3) / 3m;
        var monthlyTarget = Math.Max(Math.Round(avg3 * 1.10m, 0), 150m);

        if (thisMonthCommission >= monthlyTarget)
        {
            var periodKey = currentMonthStart.ToString("yyyy-MM");
            await TryAwardAsync(providerProfileId, "MonthlyTargetReached", periodKey,
                $"{thisMonthCommission:F0}", nowUtc, ct);
        }

        // ── 3. TierUp ────────────────────────────────────────────────────────────
        var windowStart = currentMonthStart.AddMonths(-11);
        var cumulative = await _attributions.SumProviderCommissionAsync(
            providerProfileId, windowStart, endOfTime, ct);
        var tier = CargoDryProviderTierConfig.Resolve(cumulative);
        if (tier.Code != "BRONZE")
        {
            await TryAwardAsync(providerProfileId, "TierUp", tier.Code, tier.Label, nowUtc, ct);
        }

        // ── 4. StreakMilestone ────────────────────────────────────────────────────
        var streakWindowStart = currentMonthStart.AddMonths(-23);
        var streakWindowEnd = currentMonthStart.AddMonths(1);
        var activeMonths = await _attributions.GetProviderActiveSalesMonthsAsync(
            providerProfileId, streakWindowStart, streakWindowEnd, ct);

        var currentStreak = ComputeCurrentStreak(currentMonthStart, activeMonths, 24);

        foreach (var threshold in StreakThresholds)
        {
            if (currentStreak >= threshold)
            {
                await TryAwardAsync(providerProfileId, "StreakMilestone", threshold.ToString(),
                    $"{threshold}", nowUtc, ct);
            }
        }
    }

    private async Task TryAwardAsync(
        long providerProfileId, string milestoneType, string periodKey,
        string? displayValue, DateTime nowUtc, CancellationToken ct)
    {
        if (await _awards.ExistsAsync(providerProfileId, milestoneType, periodKey, ct))
            return;

        var award = CargoDryProviderMilestoneAwardEntity.Create(
            providerProfileId, milestoneType, periodKey, displayValue, nowUtc);

        try
        {
            await _awards.AddAsync(award, ct);
            await _awards.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation — already awarded (race condition). Swallow.
            _logger.LogInformation(
                "Milestone {Type}/{PeriodKey} already awarded for provider {Pid} (race).",
                milestoneType, periodKey, providerProfileId);
            return;
        }

        await _publisher.PublishAsync(new CargoDryProviderMilestoneReachedMessage
        {
            ProviderProfileId = providerProfileId,
            MilestoneType     = milestoneType,
            PeriodKey         = periodKey,
            DisplayValue      = displayValue,
            OccurredAtUtc     = nowUtc,
        }, ct);

        award.MarkPublished();
        await _awards.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Milestone awarded: {Type}/{PeriodKey} for provider {Pid}.",
            milestoneType, periodKey, providerProfileId);
    }

    private static int ComputeCurrentStreak(
        DateTimeOffset currentMonthStart, HashSet<string> activeMonths, int lookback)
    {
        var currentKey = currentMonthStart.ToString("yyyy-MM");
        var activeThisMonth = activeMonths.Contains(currentKey);

        var startIdx = activeThisMonth ? 0 : 1; // 0=this month, 1=previous
        var streak = 0;

        for (var i = startIdx; i < lookback; i++)
        {
            var m = currentMonthStart.AddMonths(-i);
            var key = m.ToString("yyyy-MM");
            if (!activeMonths.Contains(key)) break;
            streak++;
        }

        return streak;
    }
}
