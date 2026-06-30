
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Currency;

/// <summary>Seed model for an ExchangeRate entity, read from exchange-rates.json.</summary>
[DocumentationInfo("Seed model representing an exchange rate loaded from JSON.", "Maps to ExchangeRateEntity. Idempotency key: FromCurrencyCode + ToCurrencyCode.")]
public sealed class ExchangeRateSeedModel
{
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
    public decimal Rate { get; set; }
    public int ProviderType { get; set; }
    public DateTime RateDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
}
