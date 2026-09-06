using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Response.Location;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Location;

[DocumentationInfo(
    "Set Vessel Selected Location Command Handler",
    "Loads the vessel, sets (or clears, when the body is all-null) the owner-chosen location on the aggregate, and invalidates vessel + list caches.")]
public sealed class SetVesselSelectedLocationCommandHandler : AizenCommandHandler<SetVesselSelectedLocationCommand, SetVesselSelectedLocationResponse>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public SetVesselSelectedLocationCommandHandler(
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

    public override async Task<SetVesselSelectedLocationResponse?> Handle(SetVesselSelectedLocationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel {request.VesselId} not found.");

        var r = request.Request;
        var hasSelection = r.MarinaId.HasValue
            || !string.IsNullOrWhiteSpace(r.MarinaName)
            || !string.IsNullOrWhiteSpace(r.CustomLabel)
            || r.Latitude.HasValue
            || r.Longitude.HasValue;

        if (hasSelection)
            vessel.SetSelectedLocation(r.MarinaId, r.MarinaName, r.CustomLabel, r.Latitude, r.Longitude);
        else
            vessel.ClearSelectedLocation();

        _vesselRepository.Update(vessel);

        await _invalidation.InvalidateVesselAsync(vessel.Id, cancellationToken);
        await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);

        return new SetVesselSelectedLocationResponse(vessel.ToSelectedLocationDto());
    }
}
