using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class ActivateLookupGroupCommandHandler : AizenCommandHandler<ActivateLookupGroupCommand, bool>
{
    private readonly ILookupReferenceService _service;

    public ActivateLookupGroupCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(ActivateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateGroupAsync(request.Id, cancellationToken);
        return true;
    }
}
