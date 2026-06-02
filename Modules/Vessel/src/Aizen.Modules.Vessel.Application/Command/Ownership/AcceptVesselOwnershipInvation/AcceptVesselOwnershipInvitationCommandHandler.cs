using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Accept Vessel Ownership Invitation Command Handler", "Delegates invitation acceptance to IVesselOwnershipService and invalidates owners cache.")]
public sealed class AcceptVesselOwnershipInvitationCommandHandler : AizenCommandHandler<AcceptVesselOwnershipInvitationCommand, bool>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;

    public AcceptVesselOwnershipInvitationCommandHandler(IVesselOwnershipService ownershipService, IVesselCacheInvalidationService invalidation)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(AcceptVesselOwnershipInvitationCommand request, CancellationToken cancellationToken)
    {
        await _ownershipService.AcceptInvitationAsync(request.VesselId, request.UserId, cancellationToken);
        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        return true;
    }
}
