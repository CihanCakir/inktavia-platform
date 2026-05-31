using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

public sealed class GetBaseCurrencyQueryHandler : AizenQueryHandler<GetBaseCurrencyQuery, CurrencyDto?>
{
    private readonly ICurrencyReferenceService _service;

    public GetBaseCurrencyQueryHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(GetBaseCurrencyQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetBaseCurrencyAsync(cancellationToken);
    }
}
