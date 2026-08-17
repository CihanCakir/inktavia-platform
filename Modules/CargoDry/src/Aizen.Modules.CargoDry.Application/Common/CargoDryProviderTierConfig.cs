namespace Aizen.Modules.CargoDry.Application.Common;

/// <summary>
/// Provider tier thresholds by rolling 12-month cumulative realized commission.
/// PLACEHOLDER VALUES — owned by Finance; will move to SystemParameter post sign-off.
/// CE-6a-(b): BonusRate is applied to settlement as an additive rate bonus on ProviderShareAmount.
/// </summary>
public static class CargoDryProviderTierConfig
{
    public sealed record Tier(string Code, string Label, decimal LowerInclusive, decimal? UpperExclusive, decimal BonusRate);

    /// <summary>Hard cap on the effective provider commission rate after tier bonus. Finance-owned placeholder.</summary>
    public const decimal MaxEffectiveRate = 0.35m;

    public static readonly IReadOnlyList<Tier> Tiers = new[]
    {
        new Tier("BRONZE", "Bronze",     0m,      5000m,  0.00m),
        new Tier("SILVER", "Silver",  5000m,     15000m,  0.02m),
        new Tier("GOLD",   "Gold",   15000m,      null,   0.03m),
    };

    public static Tier Resolve(decimal cumulativeCommission)
        => Tiers.First(t => cumulativeCommission >= t.LowerInclusive &&
                            (t.UpperExclusive == null || cumulativeCommission < t.UpperExclusive));

    public static Tier? Next(Tier current)
    {
        var idx = Tiers.ToList().FindIndex(t => t.Code == current.Code);
        return idx >= 0 && idx < Tiers.Count - 1 ? Tiers[idx + 1] : null;
    }

    /// <summary>effectiveRate = min(baseRate + tierBonusRate, MaxEffectiveRate), clamped to [0,1].</summary>
    public static decimal EffectiveRate(decimal baseRate, decimal tierBonusRate)
        => Math.Clamp(Math.Min(baseRate + Math.Max(tierBonusRate, 0m), MaxEffectiveRate), 0m, 1m);
}
