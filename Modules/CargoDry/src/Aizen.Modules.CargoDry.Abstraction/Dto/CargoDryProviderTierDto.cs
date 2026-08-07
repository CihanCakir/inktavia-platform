namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProviderTierDto
{
    public string   CurrentTierCode      { get; init; } = default!;
    public string   CurrentTierLabel     { get; init; } = default!;
    public decimal  CurrentBonusRate     { get; init; }
    public decimal  CumulativeCommission { get; init; }
    public string?  NextTierCode         { get; init; }
    public string?  NextTierLabel        { get; init; }
    public decimal? NextTierThreshold    { get; init; }
    public decimal? NextTierBonusRate    { get; init; }
    public decimal  RemainingToNextTier  { get; init; }
    public decimal  ProgressPct          { get; init; }
    public string   CurrencyCode         { get; init; } = "TRY";
    public DateTimeOffset ComputedAtUtc  { get; init; }
}
