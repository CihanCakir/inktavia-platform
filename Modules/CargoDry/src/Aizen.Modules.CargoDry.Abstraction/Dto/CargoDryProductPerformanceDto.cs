namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProductPerformanceDto
{
    public string  ProductCode      { get; init; } = default!;
    public decimal EarnedCommission { get; init; }
    public string  CurrencyCode     { get; init; } = "USD";
}
