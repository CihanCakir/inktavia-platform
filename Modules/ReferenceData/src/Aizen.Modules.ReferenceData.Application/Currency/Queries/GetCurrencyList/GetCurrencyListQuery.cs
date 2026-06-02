using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

public sealed class GetCurrencyListQuery : AizenQuery<IReadOnlyList<CurrencyDto>>
{
    public bool OnlyActive { get; }

    public GetCurrencyListQuery(bool onlyActive = true)
    {
        OnlyActive = onlyActive;
    }
}
