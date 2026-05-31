using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;

public sealed class ExchangeRateHistoryDto
{
    public long Id { get; set; }
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
    public decimal Rate { get; set; }
    public CurrencyRateProviderType ProviderType { get; set; }
    public DateTime RateDate { get; set; }
}
