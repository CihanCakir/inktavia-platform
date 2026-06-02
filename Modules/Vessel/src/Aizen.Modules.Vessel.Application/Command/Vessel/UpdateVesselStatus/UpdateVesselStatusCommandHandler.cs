using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Status Command Handler", "Delegates status change to IVesselStatusService and invalidates vessel and status history caches.")]
public sealed class UpdateVesselStatusCommandHandler : AizenCommandHandler<UpdateVesselStatusCommand, bool>
{
    private readonly IVesselStatusService _statusService;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public UpdateVesselStatusCommandHandler(
        IVesselStatusService statusService,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _statusService = statusService;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<bool> Handle(UpdateVesselStatusCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        await _statusService.ChangeStatusAsync(request.VesselId, request.Request.Status, request.Request.Reason, currentUserId, cancellationToken);

        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateStatusHistoryAsync(request.VesselId, cancellationToken);

        return true;
    }
}
