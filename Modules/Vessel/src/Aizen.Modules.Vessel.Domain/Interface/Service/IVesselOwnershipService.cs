using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Ownership;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel ownership service interface", "Manages vessel owner relationships, roles and primary owner protection.")]
public interface IVesselOwnershipService
{
    Task<VesselOwnerDto> AddOwnerAsync(long vesselId, AddVesselOwnerRequest request, CancellationToken cancellationToken = default);
    Task<VesselOwnerDto> UpdateOwnerRoleAsync(long vesselId, long ownerId, VesselOwnershipRole role, CancellationToken cancellationToken = default);
    Task RemoveOwnerAsync(long vesselId, long ownerId, CancellationToken cancellationToken = default);
    Task AcceptInvitationAsync(long vesselId, long userId, CancellationToken cancellationToken = default);
    Task RejectInvitationAsync(long vesselId, long userId, CancellationToken cancellationToken = default);
    Task SetPrimaryOwnerAsync(long vesselId, long ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselOwnerDto>> GetOwnersAsync(long vesselId, CancellationToken cancellationToken = default);
}
