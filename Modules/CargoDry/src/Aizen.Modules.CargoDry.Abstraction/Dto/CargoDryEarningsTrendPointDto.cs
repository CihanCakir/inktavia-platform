namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryEarningsTrendPointDto
{
    public string  Month        { get; init; } = default!;
    public decimal Commission   { get; init; }
    public string  CurrencyCode { get; init; } = "TRY";
}
