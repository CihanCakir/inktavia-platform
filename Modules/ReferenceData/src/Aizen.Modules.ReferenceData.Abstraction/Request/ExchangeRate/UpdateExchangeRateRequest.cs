using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Request.ExchangeRate;

public sealed class UpdateExchangeRateRequest
{
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
    public decimal Rate { get; set; }
    public CurrencyRateProviderType ProviderType { get; set; }
    public DateTime RateDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? RawProviderPayload { get; set; }
}
