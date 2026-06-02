using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class ActivateLookupGroupCommandHandler : AizenCommandHandler<ActivateLookupGroupCommand, bool>
{
    private readonly ILookupReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public ActivateLookupGroupCommandHandler(ILookupReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(ActivateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateGroupAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateLookupGroupAsync(request.Id, cancellationToken);
        return true;
    }
}
