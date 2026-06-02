using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Reject Vessel Ownership Invitation Command Handler", "Delegates invitation rejection to IVesselOwnershipService and invalidates owners cache.")]
public sealed class RejectVesselOwnershipInvitationCommandHandler : AizenCommandHandler<RejectVesselOwnershipInvitationCommand, bool>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;

    public RejectVesselOwnershipInvitationCommandHandler(IVesselOwnershipService ownershipService, IVesselCacheInvalidationService invalidation)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(RejectVesselOwnershipInvitationCommand request, CancellationToken cancellationToken)
    {
        await _ownershipService.RejectInvitationAsync(request.VesselId, request.UserId, cancellationToken);
        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        return true;
    }
}
