using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class MoveLookupGroupCommandHandler : AizenCommandHandler<MoveLookupGroupCommand, LookupGroupTreeDto>
{
    private readonly ILookupTreeService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public MoveLookupGroupCommandHandler(ILookupTreeService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<LookupGroupTreeDto?> Handle(MoveLookupGroupCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.MoveGroupAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateLookupGroupAsync(cancellationToken: cancellationToken);
        return result;
    }
}
