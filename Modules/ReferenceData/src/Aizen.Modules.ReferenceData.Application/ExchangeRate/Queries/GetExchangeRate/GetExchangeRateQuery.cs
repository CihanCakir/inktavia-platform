using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

public sealed class GetExchangeRateQuery : AizenQuery<ExchangeRateDto?>
{
    public string FromCurrencyCode { get; }
    public string ToCurrencyCode { get; }

    public GetExchangeRateQuery(string fromCurrencyCode, string toCurrencyCode)
    {
        FromCurrencyCode = fromCurrencyCode;
        ToCurrencyCode = toCurrencyCode;
    }
}
