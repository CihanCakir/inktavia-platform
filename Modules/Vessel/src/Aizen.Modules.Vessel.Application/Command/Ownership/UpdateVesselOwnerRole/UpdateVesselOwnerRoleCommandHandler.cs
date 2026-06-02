using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Update Vessel Owner Role Command Handler", "Loads owner entity, applies role change and invalidates owners cache.")]
public sealed class UpdateVesselOwnerRoleCommandHandler : AizenCommandHandler<UpdateVesselOwnerRoleCommand, VesselOwnerDto>
{
    private readonly IVesselOwnerRepository _ownerRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpdateVesselOwnerRoleCommandHandler(IVesselOwnerRepository ownerRepository, IVesselCacheInvalidationService invalidation)
    {
        _ownerRepository = ownerRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselOwnerDto?> Handle(UpdateVesselOwnerRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = await _ownerRepository.GetByIdAsync(request.OwnerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel owner {request.OwnerId} not found.");

        owner.ChangeRole(request.Request.Role);
        _ownerRepository.Update(owner);

        await _invalidation.InvalidateOwnersAsync(request.VesselId, cancellationToken);

        return owner.ToDto();
    }
}
