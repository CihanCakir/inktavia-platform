using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Set Primary Vessel Owner Command Handler", "Delegates primary owner assignment to IVesselOwnershipService and invalidates owners cache.")]
public sealed class SetPrimaryVesselOwnerCommandHandler : AizenCommandHandler<SetPrimaryVesselOwnerCommand, bool>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;

    public SetPrimaryVesselOwnerCommandHandler(IVesselOwnershipService ownershipService, IVesselCacheInvalidationService invalidation)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(SetPrimaryVesselOwnerCommand request, CancellationToken cancellationToken)
    {
        await _ownershipService.SetPrimaryOwnerAsync(request.VesselId, request.OwnerId, cancellationToken);
        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        return true;
    }
}
