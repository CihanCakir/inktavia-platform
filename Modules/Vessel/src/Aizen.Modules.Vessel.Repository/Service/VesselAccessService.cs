using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel access service", "Checks user access rights and visibility rules for a vessel.")]
public sealed class VesselAccessService : IVesselAccessService
{
    private readonly IVesselOwnerRepository _ownerRepository;

    public VesselAccessService(IVesselOwnerRepository ownerRepository)
    {
        _ownerRepository = ownerRepository;
    }

    public async Task<bool> UserHasAccessAsync(long vesselId, long userId, CancellationToken ct = default)
    {
        var owner = await _ownerRepository.GetByVesselAndUserAsync(vesselId, userId, ct);
        return owner is { IsActive: true };
    }

    public Task<bool> UserHasRoleAsync(long vesselId, long userId, VesselOwnershipRole role, CancellationToken ct = default)
        => _ownerRepository.UserHasRoleAsync(vesselId, userId, role, ct);

    public async Task<bool> CanViewVesselAsync(long vesselId, long? requestingUserId, CancellationToken ct = default)
    {
        // Non-authenticated users can only see public vessels
        if (!requestingUserId.HasValue)
            return true; // filtered by Visibility in the query layer

        return await UserHasAccessAsync(vesselId, requestingUserId.Value, ct);
    }

    public async Task EnsureCanEditAsync(long vesselId, long userId, CancellationToken ct = default)
    {
        if (!await UserHasAccessAsync(vesselId, userId, ct))
            throw new UnauthorizedAccessException("You do not have permission to edit this vessel.");
    }

    public async Task EnsureCanManageOwnersAsync(long vesselId, long userId, CancellationToken ct = default)
    {
        if (!await UserHasRoleAsync(vesselId, userId, VesselOwnershipRole.PrimaryOwner, ct))
            throw new UnauthorizedAccessException("You do not have permission to manage owners of this vessel.");
    }
}
