using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Add Vessel Owner Command Handler", "Delegates owner addition to IVesselOwnershipService and invalidates owners and vessel detail caches.")]
public sealed class AddVesselOwnerCommandHandler : AizenCommandHandler<AddVesselOwnerCommand, AddVesselOwnerResponse>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public AddVesselOwnerCommandHandler(
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

    public override async Task<AddVesselOwnerResponse?> Handle(AddVesselOwnerCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanManageOwnersAsync(request.VesselId, currentUserId, cancellationToken);

        var result = await _ownershipService.AddOwnerAsync(request.VesselId, request.Request, cancellationToken);

        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return new AddVesselOwnerResponse(result);
    }
}
