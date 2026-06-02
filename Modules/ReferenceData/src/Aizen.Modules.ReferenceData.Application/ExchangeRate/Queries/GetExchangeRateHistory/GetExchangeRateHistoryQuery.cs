using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

public sealed class GetExchangeRateHistoryQuery : AizenQuery<IReadOnlyList<ExchangeRateHistoryDto>>
{
    public string FromCurrencyCode { get; }
    public string ToCurrencyCode { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }

    public GetExchangeRateHistoryQuery(string fromCurrencyCode, string toCurrencyCode, DateTime? startDate = null, DateTime? endDate = null)
    {
        FromCurrencyCode = fromCurrencyCode;
        ToCurrencyCode = toCurrencyCode;
        StartDate = startDate;
        EndDate = endDate;
    }
}
