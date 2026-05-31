using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Queries;

public sealed class GetCurrencyDetailQueryHandler : AizenQueryHandler<GetCurrencyDetailQuery, CurrencyDto?>
{
    private readonly ICurrencyReferenceService _service;

    public GetCurrencyDetailQueryHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(GetCurrencyDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
