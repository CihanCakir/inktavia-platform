namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;

public sealed class CurrencyDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; }
}
