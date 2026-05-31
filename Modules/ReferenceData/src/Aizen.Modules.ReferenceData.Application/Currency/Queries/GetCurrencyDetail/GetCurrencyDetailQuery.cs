using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

public sealed class GetCurrencyDetailQuery : AizenQuery<CurrencyDto?>
{
    public long Id { get; }

    public GetCurrencyDetailQuery(long id)
    {
        Id = id;
    }
}
