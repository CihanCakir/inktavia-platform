using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class UpdateLookupGroupCommandHandler : AizenCommandHandler<UpdateLookupGroupCommand, LookupGroupDto>
{
    private readonly ILookupReferenceService _service;

    public UpdateLookupGroupCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<LookupGroupDto?> Handle(UpdateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateGroupAsync(request.Id, request.Request, cancellationToken);
    }
}
