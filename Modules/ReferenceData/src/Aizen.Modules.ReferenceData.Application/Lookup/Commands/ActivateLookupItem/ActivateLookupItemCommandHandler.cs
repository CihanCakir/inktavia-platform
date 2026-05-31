using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class ActivateLookupItemCommandHandler : AizenCommandHandler<ActivateLookupItemCommand, bool>
{
    private readonly ILookupReferenceService _service;

    public ActivateLookupItemCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(ActivateLookupItemCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateItemAsync(request.Id, cancellationToken);
        return true;
    }
}
