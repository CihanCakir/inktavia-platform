using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Remove Vessel Owner Command Handler", "Delegates owner removal to IVesselOwnershipService and invalidates owners and vessel detail caches.")]
public sealed class RemoveVesselOwnerCommandHandler : AizenCommandHandler<RemoveVesselOwnerCommand, RemoveVesselOwnerResponse>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public RemoveVesselOwnerCommandHandler(
        IVesselOwnershipService ownershipService,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<RemoveVesselOwnerResponse?> Handle(RemoveVesselOwnerCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanManageOwnersAsync(request.VesselId, currentUserId, cancellationToken);

        await _ownershipService.RemoveOwnerAsync(request.VesselId, request.OwnerId, cancellationToken);

        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return new RemoveVesselOwnerResponse(request.VesselId, request.OwnerId);
    }
}
