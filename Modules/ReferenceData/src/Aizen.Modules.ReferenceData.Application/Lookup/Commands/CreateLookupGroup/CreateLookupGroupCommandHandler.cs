using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class CreateLookupGroupCommandHandler : AizenCommandHandler<CreateLookupGroupCommand, LookupGroupDto>
{
    private readonly ILookupReferenceService _service;

    public CreateLookupGroupCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<LookupGroupDto?> Handle(CreateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateGroupAsync(request.Request, cancellationToken);
    }
}
