using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class CreateLookupItemCommandHandler : AizenCommandHandler<CreateLookupItemCommand, LookupItemDto>
{
    private readonly ILookupReferenceService _service;

    public CreateLookupItemCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<LookupItemDto?> Handle(CreateLookupItemCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateItemAsync(request.Request, cancellationToken);
    }
}
