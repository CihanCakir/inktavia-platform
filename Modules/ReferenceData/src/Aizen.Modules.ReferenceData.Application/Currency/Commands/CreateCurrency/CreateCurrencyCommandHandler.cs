using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class CreateCurrencyCommandHandler : AizenCommandHandler<CreateCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateCurrencyCommandHandler(ICurrencyReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<CurrencyDto?> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateCurrencyAsync(cancellationToken: cancellationToken);
        return result;
    }
}
