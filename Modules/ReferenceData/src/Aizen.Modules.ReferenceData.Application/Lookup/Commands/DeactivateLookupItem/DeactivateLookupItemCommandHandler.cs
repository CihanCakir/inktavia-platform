using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class DeactivateLookupItemCommandHandler : AizenCommandHandler<DeactivateLookupItemCommand, bool>
{
    private readonly ILookupReferenceService _service;

    public DeactivateLookupItemCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(DeactivateLookupItemCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateItemAsync(request.Id, cancellationToken);
        return true;
    }
}
