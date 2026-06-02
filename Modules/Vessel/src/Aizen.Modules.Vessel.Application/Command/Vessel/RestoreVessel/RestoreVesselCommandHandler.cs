using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Restore Vessel Command Handler", "Restores an archived vessel and invalidates vessel and list caches.")]
public sealed class RestoreVesselCommandHandler : AizenCommandHandler<RestoreVesselCommand, bool>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public RestoreVesselCommandHandler(
        IVesselRepository vesselRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _vesselRepository = vesselRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<bool> Handle(RestoreVesselCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel {request.VesselId} not found.");

        if (!vessel.IsArchived)
            throw new InvalidOperationException("Vessel is not archived.");

        vessel.Restore();
        _vesselRepository.Update(vessel);

        await _invalidation.InvalidateVesselAsync(vessel.Id, cancellationToken);
        await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);

        return true;
    }
}
