using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Add Vessel Owner Command Handler", "Delegates owner addition to IVesselOwnershipService and invalidates owners and vessel detail caches.")]
public sealed class AddVesselOwnerCommandHandler : AizenCommandHandler<AddVesselOwnerCommand, VesselOwnerDto>
{
    private readonly IVesselOwnershipService _ownershipService;
    private readonly IVesselCacheInvalidationService _invalidation;

    public AddVesselOwnerCommandHandler(IVesselOwnershipService ownershipService, IVesselCacheInvalidationService invalidation)
    {
        _ownershipService = ownershipService;
        _invalidation = invalidation;
    }

    public override async Task<VesselOwnerDto?> Handle(AddVesselOwnerCommand request, CancellationToken cancellationToken)
    {
        var result = await _ownershipService.AddOwnerAsync(request.VesselId, request.Request, cancellationToken);

        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return result;
    }
}
