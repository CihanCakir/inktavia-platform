
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Currency;

/// <summary>Seed model for an ExchangeRateHistory entity, read from exchange-rate-histories.json.</summary>
[DocumentationInfo("Seed model representing an exchange rate history entry loaded from JSON.", "Maps to ExchangeRateHistoryEntity. Idempotency key: FromCurrencyCode + ToCurrencyCode + RateDate.")]
public sealed class ExchangeRateHistorySeedModel
{
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
    public decimal Rate { get; set; }
    public int ProviderType { get; set; }
    public DateTime RateDate { get; set; }
}
