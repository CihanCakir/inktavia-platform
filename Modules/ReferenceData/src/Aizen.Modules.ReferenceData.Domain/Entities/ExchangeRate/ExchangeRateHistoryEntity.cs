using Aizen.Core.Domain;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

public sealed class ExchangeRateHistoryEntity : AizenEntityWithAudit
{
    public string FromCurrencyCode { get; private set; } = default!;
    public string ToCurrencyCode { get; private set; } = default!;
    public decimal Rate { get; private set; }
    public CurrencyRateProviderType ProviderType { get; private set; }
    public DateTime RateDate { get; private set; }
    public string? RawProviderPayload { get; private set; }

    private ExchangeRateHistoryEntity() { }

    public static ExchangeRateHistoryEntity Create(string fromCurrencyCode, string toCurrencyCode, decimal rate, CurrencyRateProviderType providerType, DateTime rateDate, string? rawProviderPayload)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate));

        return new ExchangeRateHistoryEntity
        {
            FromCurrencyCode = fromCurrencyCode.Trim().ToUpperInvariant(),
            ToCurrencyCode = toCurrencyCode.Trim().ToUpperInvariant(),
            Rate = rate,
            ProviderType = providerType,
            RateDate = rateDate,
            RawProviderPayload = rawProviderPayload
        };
    }
}
