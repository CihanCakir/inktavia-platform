namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Prior-period comparison for CargoDry dashboard KPI change badges.
/// Compares current 30-day window vs previous 30-day window.
/// Null values indicate no prior data available for that metric.
/// </summary>
public sealed class CargoDryStatsComparisonDto
{
    /// <summary>Percent change in active kits vs prior period. Positive = growth.</summary>
    public double? ActiveKitsChangePercent     { get; init; }
    /// <summary>Percent change in total kits vs prior period.</summary>
    public double? TotalKitsChangePercent      { get; init; }
    /// <summary>Percent change in today's activations vs same day 30d ago.</summary>
    public double? TodayActivationsChangePct   { get; init; }
    /// <summary>Renewal rate delta vs prior period (absolute, not percent of percent).</summary>
    public double? RenewalRateDelta            { get; init; }
    /// <summary>UTC window start for current period.</summary>
    public string  CurrentPeriodStart          { get; init; } = default!;
    /// <summary>UTC window start for prior period.</summary>
    public string  PriorPeriodStart            { get; init; } = default!;
}
