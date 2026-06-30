
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Currency;

/// <summary>Seed model for a Currency entity, read from currencies.json.</summary>
[DocumentationInfo("Seed model representing a currency loaded from JSON.", "Maps to CurrencyEntity. Idempotency key: Code.")]
public sealed class CurrencySeedModel
{
    public string Code { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; } = true;
}
