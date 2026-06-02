using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Status Command Handler", "Delegates status change to IVesselStatusService and invalidates vessel and status history caches.")]
public sealed class UpdateVesselStatusCommandHandler : AizenCommandHandler<UpdateVesselStatusCommand, bool>
{
    private readonly IVesselStatusService _statusService;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpdateVesselStatusCommandHandler(IVesselStatusService statusService, IVesselCacheInvalidationService invalidation)
    {
        _statusService = statusService;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(UpdateVesselStatusCommand request, CancellationToken cancellationToken)
    {
        await _statusService.ChangeStatusAsync(request.VesselId, request.Request.Status, request.Request.Reason, request.RequestingUserId, cancellationToken);

        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateStatusHistoryAsync(request.VesselId, cancellationToken);

        return true;
    }
}
