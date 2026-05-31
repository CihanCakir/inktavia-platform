namespace Aizen.Modules.ReferenceData.Abstraction.Request.Currency;

public sealed class UpdateCurrencyRequest
{
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
}
