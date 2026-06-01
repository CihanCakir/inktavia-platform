using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class UpdateCurrencyCommandHandler : AizenCommandHandler<UpdateCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateCurrencyCommandHandler(ICurrencyReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<CurrencyDto?> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request.Id, request.Name, request.Symbol, request.DecimalPlaces, request.IsActive, cancellationToken);
        await _invalidation.InvalidateCurrencyAsync(request.Id, cancellationToken);
        return result;
    }
}
