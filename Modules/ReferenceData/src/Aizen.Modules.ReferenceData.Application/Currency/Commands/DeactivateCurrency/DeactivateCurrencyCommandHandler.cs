using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class DeactivateCurrencyCommandHandler : AizenCommandHandler<DeactivateCurrencyCommand, bool>
{
    private readonly ICurrencyReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public DeactivateCurrencyCommandHandler(ICurrencyReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(DeactivateCurrencyCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateCurrencyAsync(request.Id, cancellationToken);
        return true;
    }
}
