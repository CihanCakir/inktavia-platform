using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Archive Vessel Command Handler", "Archives a vessel and invalidates vessel and list caches.")]
public sealed class ArchiveVesselCommandHandler : AizenCommandHandler<ArchiveVesselCommand, ArchiveVesselResponse>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public ArchiveVesselCommandHandler(
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

    public override async Task<ArchiveVesselResponse?> Handle(ArchiveVesselCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel {request.VesselId} not found.");

        if (vessel.IsArchived)
            throw new InvalidOperationException("Vessel is already archived.");

        vessel.Archive(request.Request.Reason);
        _vesselRepository.Update(vessel);

        await _invalidation.InvalidateVesselAsync(vessel.Id, cancellationToken);
        await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);

        return new ArchiveVesselResponse(vessel.Id, vessel.IsArchived, vessel.ArchivedAt, vessel.ArchiveReason);
    }
}
