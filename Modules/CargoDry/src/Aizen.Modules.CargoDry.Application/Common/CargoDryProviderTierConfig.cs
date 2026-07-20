namespace Aizen.Modules.CargoDry.Application.Common;

/// <summary>
/// Provider tier thresholds by rolling 12-month cumulative realized commission.
/// PLACEHOLDER VALUES — owned by Finance; will move to SystemParameter in CE-6a-(b).
/// This slice is display-only; BonusRate is surfaced to the UI but NOT applied to any settlement math yet.
/// </summary>
public static class CargoDryProviderTierConfig
{
    public sealed record Tier(string Code, string Label, decimal LowerInclusive, decimal? UpperExclusive, decimal BonusRate);

    // BonusRate is an additive rate point (e.g. 0.02m = +2%), display-only in this slice.
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
}
