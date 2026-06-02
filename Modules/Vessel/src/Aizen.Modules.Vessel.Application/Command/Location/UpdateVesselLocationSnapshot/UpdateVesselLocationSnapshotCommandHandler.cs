using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Location;

[DocumentationInfo("Update Vessel Location Snapshot Command Handler", "Marks previous snapshot as historical, creates a new current snapshot and invalidates location cache.")]
public sealed class UpdateVesselLocationSnapshotCommandHandler : AizenCommandHandler<UpdateVesselLocationSnapshotCommand, VesselLocationSnapshotDto>
{
    private readonly IVesselLocationSnapshotRepository _locationRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public UpdateVesselLocationSnapshotCommandHandler(
        IVesselLocationSnapshotRepository locationRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _locationRepository = locationRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<VesselLocationSnapshotDto?> Handle(UpdateVesselLocationSnapshotCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var current = await _locationRepository.GetCurrentAsync(request.VesselId, cancellationToken);
        if (current is not null)
        {
            current.MarkAsHistorical();
            _locationRepository.Update(current);
        }

        var r = request.Request;
        var snapshot = VesselLocationSnapshotEntity.Create(
            request.VesselId,
            r.CountryCode,
            r.CityCode,
            r.DistrictCode,
            r.MarinaName,
            r.Latitude,
            r.Longitude,
            r.AccuracyMeters,
            r.Source,
            r.CapturedAt);

        await _locationRepository.AddAsync(snapshot, cancellationToken);

        await _invalidation.InvalidateLocationAsync(request.VesselId, cancellationToken);

        return snapshot.ToDto();
    }
}
