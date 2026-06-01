using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class CreateLookupGroupCommandHandler : AizenCommandHandler<CreateLookupGroupCommand, LookupGroupDto>
{
    private readonly ILookupReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateLookupGroupCommandHandler(ILookupReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<LookupGroupDto?> Handle(CreateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateGroupAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateLookupGroupAsync(cancellationToken: cancellationToken);
        return result;
    }
}
