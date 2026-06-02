using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class CreateExchangeRateHistoryCommand : AizenCommand<ExchangeRateHistoryDto>
{
    public string FromCurrencyCode { get; }
    public string ToCurrencyCode { get; }
    public decimal Rate { get; }
    public CurrencyRateProviderType ProviderType { get; }
    public DateTime RateDate { get; }
    public string? RawProviderPayload { get; }

    public CreateExchangeRateHistoryCommand(string fromCurrencyCode, string toCurrencyCode, decimal rate, CurrencyRateProviderType providerType, DateTime rateDate, string? rawProviderPayload)
    {
        FromCurrencyCode = fromCurrencyCode;
        ToCurrencyCode = toCurrencyCode;
        Rate = rate;
        ProviderType = providerType;
        RateDate = rateDate;
        RawProviderPayload = rawProviderPayload;
    }
}
