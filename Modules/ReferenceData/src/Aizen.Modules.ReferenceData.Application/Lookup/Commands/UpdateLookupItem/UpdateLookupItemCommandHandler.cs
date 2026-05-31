using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class UpdateLookupItemCommandHandler : AizenCommandHandler<UpdateLookupItemCommand, LookupItemDto>
{
    private readonly ILookupReferenceService _service;

    public UpdateLookupItemCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<LookupItemDto?> Handle(UpdateLookupItemCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateItemAsync(request.Id, request.Name, request.Description, request.IconKey, request.ColorCode, request.SortOrder, request.IsDefault, request.IsActive, cancellationToken);
    }
}
