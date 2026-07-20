namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProviderMomentumDto
{
    /// <summary>Consecutive months with ≥1 sale, ending at the current month (or the previous month if the
    /// current in-progress month has no sale yet — the streak is preserved until the month ends).</summary>
    public int  CurrentStreakMonths { get; init; }
    /// <summary>Longest run of consecutive active months observed within the lookback window.</summary>
    public int  BestStreakMonths    { get; init; }
    /// <summary>True if the current calendar month (UTC) already has ≥1 sale.</summary>
    public bool ActiveThisMonth     { get; init; }
}
