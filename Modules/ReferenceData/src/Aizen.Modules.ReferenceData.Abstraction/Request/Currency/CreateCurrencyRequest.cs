namespace Aizen.Modules.ReferenceData.Abstraction.Request.Currency;

public sealed class CreateCurrencyRequest
{
    public string Code { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
}
