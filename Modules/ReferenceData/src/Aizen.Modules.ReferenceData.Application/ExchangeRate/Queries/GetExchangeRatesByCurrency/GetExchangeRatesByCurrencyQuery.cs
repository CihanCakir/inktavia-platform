using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

public sealed class GetExchangeRatesByCurrencyQuery : AizenQuery<IReadOnlyList<ExchangeRateDto>>
{
    public string CurrencyCode { get; }

    public GetExchangeRatesByCurrencyQuery(string currencyCode)
    {
        CurrencyCode = currencyCode;
    }
}
