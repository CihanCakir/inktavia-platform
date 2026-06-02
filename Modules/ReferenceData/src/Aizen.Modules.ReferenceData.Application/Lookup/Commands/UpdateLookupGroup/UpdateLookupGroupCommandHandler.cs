using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class UpdateLookupGroupCommandHandler : AizenCommandHandler<UpdateLookupGroupCommand, LookupGroupDto>
{
    private readonly ILookupReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateLookupGroupCommandHandler(ILookupReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<LookupGroupDto?> Handle(UpdateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateGroupAsync(request.Id, request.Request, cancellationToken);
        await _invalidation.InvalidateLookupGroupAsync(request.Id, cancellationToken);
        return result;
    }
}
