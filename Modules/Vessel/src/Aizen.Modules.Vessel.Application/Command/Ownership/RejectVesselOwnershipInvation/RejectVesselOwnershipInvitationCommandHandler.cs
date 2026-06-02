using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Reject Vessel Ownership Invitation Command Handler", "Delegates invitation rejection to IVesselOwnershipService and invalidates owners cache.")]
public sealed class RejectVesselOwnershipInvitationCommandHandler : AizenCommandHandler<RejectVesselOwnershipInvitationCommand, RejectVesselOwnershipInvitationResponse>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IAizenInfoAccessor _info;

    public RejectVesselOwnershipInvitationCommandHandler(
        IVesselOwnershipService ownershipService,
        IVesselCacheInvalidationService invalidation,
        IAizenInfoAccessor info)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
        _info = info;
    }

    public override async Task<RejectVesselOwnershipInvitationResponse?> Handle(RejectVesselOwnershipInvitationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        await _ownershipService.RejectInvitationAsync(request.VesselId, currentUserId, cancellationToken);
        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        return new RejectVesselOwnershipInvitationResponse(request.VesselId, currentUserId);
    }
}
