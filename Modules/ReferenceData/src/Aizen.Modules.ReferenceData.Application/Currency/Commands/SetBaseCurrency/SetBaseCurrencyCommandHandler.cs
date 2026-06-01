using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class SetBaseCurrencyCommandHandler : AizenCommandHandler<SetBaseCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public SetBaseCurrencyCommandHandler(ICurrencyReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<CurrencyDto?> Handle(SetBaseCurrencyCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SetBaseCurrencyAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateCurrencyAsync(cancellationToken: cancellationToken);
        return result;
    }
}
