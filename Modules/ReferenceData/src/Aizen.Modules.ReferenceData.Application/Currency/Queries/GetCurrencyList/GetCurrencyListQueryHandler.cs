using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

public sealed class GetCurrencyListQueryHandler : AizenQueryHandler<GetCurrencyListQuery, IReadOnlyList<CurrencyDto>>
{
    private readonly ICurrencyReferenceService _service;

    public GetCurrencyListQueryHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<CurrencyDto>> Handle(GetCurrencyListQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetListAsync(request.OnlyActive, cancellationToken);
    }
}
