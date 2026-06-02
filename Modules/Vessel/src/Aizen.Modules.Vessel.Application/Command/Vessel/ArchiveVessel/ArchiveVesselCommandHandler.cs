using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Archive Vessel Command Handler", "Archives a vessel and invalidates vessel and list caches.")]
public sealed class ArchiveVesselCommandHandler : AizenCommandHandler<ArchiveVesselCommand, bool>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public ArchiveVesselCommandHandler(IVesselRepository vesselRepository, IVesselCacheInvalidationService invalidation)
    {
        _vesselRepository = vesselRepository;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(ArchiveVesselCommand request, CancellationToken cancellationToken)
    {
        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel {request.VesselId} not found.");

        vessel.Archive(request.Request.Reason);
        _vesselRepository.Update(vessel);

        await _invalidation.InvalidateVesselAsync(vessel.Id, cancellationToken);
        await _invalidation.InvalidateUserVesselListAsync(request.RequestingUserId, cancellationToken);

        return true;
    }
}
