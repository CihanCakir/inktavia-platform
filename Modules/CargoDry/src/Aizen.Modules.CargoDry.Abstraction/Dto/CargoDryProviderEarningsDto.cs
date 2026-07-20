namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProviderEarningsDto
{
    public decimal ThisMonthCommission { get; init; }
    public decimal YtdCommission       { get; init; }
    public decimal PendingPayout       { get; init; }
    public decimal PaidPayout          { get; init; }
    public decimal InHandPotential     { get; init; }
    public decimal RenewalPotential    { get; init; }
    public decimal AvgEarningPerKit    { get; init; }
    public decimal SellThroughPct      { get; init; }
    public int     SoldKits            { get; init; }
    public int     InHandKits          { get; init; }
    public decimal MonthlyTarget       { get; init; }
    public decimal TargetAchieved      { get; init; }
    public decimal RemainingToTarget   { get; init; }
    public decimal ProgressPct         { get; init; }
    public string  CurrencyCode        { get; init; } = "USD";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
