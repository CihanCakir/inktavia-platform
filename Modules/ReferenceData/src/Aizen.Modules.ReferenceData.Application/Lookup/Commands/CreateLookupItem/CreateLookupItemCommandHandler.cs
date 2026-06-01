using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class CreateLookupItemCommandHandler : AizenCommandHandler<CreateLookupItemCommand, LookupItemDto>
{
    private readonly ILookupReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateLookupItemCommandHandler(ILookupReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<LookupItemDto?> Handle(CreateLookupItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateItemAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateLookupItemAsync(cancellationToken: cancellationToken);
        return result;
    }
}
