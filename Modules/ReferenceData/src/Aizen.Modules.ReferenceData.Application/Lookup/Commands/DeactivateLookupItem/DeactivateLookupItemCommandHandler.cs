using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class DeactivateLookupItemCommandHandler : AizenCommandHandler<DeactivateLookupItemCommand, bool>
{
    private readonly ILookupReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public DeactivateLookupItemCommandHandler(ILookupReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(DeactivateLookupItemCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateItemAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateLookupItemAsync(cancellationToken: cancellationToken);
        return true;
    }
}
