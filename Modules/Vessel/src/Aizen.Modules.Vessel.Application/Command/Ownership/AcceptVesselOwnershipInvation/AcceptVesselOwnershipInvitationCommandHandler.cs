using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Accept Vessel Ownership Invitation Command Handler", "Delegates invitation acceptance to IVesselOwnershipService and invalidates owners cache.")]
public sealed class AcceptVesselOwnershipInvitationCommandHandler : AizenCommandHandler<AcceptVesselOwnershipInvitationCommand, bool>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IAizenInfoAccessor _info;

    public AcceptVesselOwnershipInvitationCommandHandler(
        IVesselOwnershipService ownershipService,
        IVesselCacheInvalidationService invalidation,
        IAizenInfoAccessor info)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
        _info = info;
    }

    public override async Task<bool> Handle(AcceptVesselOwnershipInvitationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        await _ownershipService.AcceptInvitationAsync(request.VesselId, currentUserId, cancellationToken);
        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        return true;
    }
}
