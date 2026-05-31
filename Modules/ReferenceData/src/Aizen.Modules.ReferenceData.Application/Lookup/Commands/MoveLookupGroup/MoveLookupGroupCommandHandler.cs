using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class MoveLookupGroupCommandHandler : AizenCommandHandler<MoveLookupGroupCommand, LookupGroupTreeDto>
{
    private readonly ILookupTreeService _service;

    public MoveLookupGroupCommandHandler(ILookupTreeService service)
    {
        _service = service;
    }

    public override async Task<LookupGroupTreeDto?> Handle(MoveLookupGroupCommand request, CancellationToken cancellationToken)
    {
        return await _service.MoveGroupAsync(request.Request, cancellationToken);
    }
}
