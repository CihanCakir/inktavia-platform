using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Visibility Command Handler", "Updates vessel visibility and invalidates vessel cache.")]
public sealed class UpdateVesselVisibilityCommandHandler : AizenCommandHandler<UpdateVesselVisibilityCommand, bool>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpdateVesselVisibilityCommandHandler(IVesselRepository vesselRepository, IVesselCacheInvalidationService invalidation)
    {
        _vesselRepository = vesselRepository;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(UpdateVesselVisibilityCommand request, CancellationToken cancellationToken)
    {
        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel {request.VesselId} not found.");

        vessel.UpdateVisibility(request.Visibility);
        _vesselRepository.Update(vessel);

        await _invalidation.InvalidateVesselAsync(vessel.Id, cancellationToken);

        return true;
    }
}
