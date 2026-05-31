using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class DeactivateLookupGroupCommandHandler : AizenCommandHandler<DeactivateLookupGroupCommand, bool>
{
    private readonly ILookupReferenceService _service;

    public DeactivateLookupGroupCommandHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(DeactivateLookupGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateGroupAsync(request.Id, cancellationToken);
        return true;
    }
}
