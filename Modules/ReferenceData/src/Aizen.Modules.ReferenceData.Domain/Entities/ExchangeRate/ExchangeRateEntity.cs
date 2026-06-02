using Aizen.Core.Domain;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

public sealed class ExchangeRateEntity : AizenEntityWithAudit
{
    public string FromCurrencyCode { get; private set; } = default!;
    public string ToCurrencyCode { get; private set; } = default!;
    public decimal Rate { get; private set; }
    public CurrencyRateProviderType ProviderType { get; private set; }
    public DateTime RateDate { get; private set; }
    public DateTime ValidUntil { get; private set; }

    public ExchangeRateEntity() { }

    public static ExchangeRateEntity Create(string fromCurrencyCode, string toCurrencyCode, decimal rate, CurrencyRateProviderType providerType, DateTime rateDate, DateTime validUntil)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate));
        if (validUntil <= rateDate) throw new ArgumentException("ValidUntil must be greater than RateDate.", nameof(validUntil));

        return new ExchangeRateEntity
        {
            FromCurrencyCode = fromCurrencyCode.Trim().ToUpperInvariant(),
            ToCurrencyCode = toCurrencyCode.Trim().ToUpperInvariant(),
            Rate = rate,
            ProviderType = providerType,
            RateDate = rateDate,
            ValidUntil = validUntil,
            IsActive = true
        };
    }

    public void UpdateRate(decimal rate, CurrencyRateProviderType providerType, DateTime rateDate, DateTime validUntil)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate));
        if (validUntil <= rateDate) throw new ArgumentException("ValidUntil must be greater than RateDate.", nameof(validUntil));

        Rate = rate;
        ProviderType = providerType;
        RateDate = rateDate;
        ValidUntil = validUntil;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
}
