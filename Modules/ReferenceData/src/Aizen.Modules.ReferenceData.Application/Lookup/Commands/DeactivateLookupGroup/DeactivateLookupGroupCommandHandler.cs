using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class DeactivateLookupGroupCommandHandler : AizenCommandHandler<DeactivateLookupGroupCommand, bool>
{
    private readonly ILookupReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public DeactivateLookupGroupCommandHandler(ILookupReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(DeactivateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateGroupAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateLookupGroupAsync(request.Id, cancellationToken);
        return true;
    }
}
