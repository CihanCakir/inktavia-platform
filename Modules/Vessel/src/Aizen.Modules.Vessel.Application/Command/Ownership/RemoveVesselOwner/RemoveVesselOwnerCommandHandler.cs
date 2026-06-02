using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Remove Vessel Owner Command Handler", "Delegates owner removal to IVesselOwnershipService and invalidates owners and vessel detail caches.")]
public sealed class RemoveVesselOwnerCommandHandler : AizenCommandHandler<RemoveVesselOwnerCommand, bool>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;

    public RemoveVesselOwnerCommandHandler(IVesselOwnershipService ownershipService, IVesselCacheInvalidationService invalidation)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(RemoveVesselOwnerCommand request, CancellationToken cancellationToken)
    {
        await _ownershipService.RemoveOwnerAsync(request.VesselId, request.OwnerId, cancellationToken);

        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return true;
    }
}
