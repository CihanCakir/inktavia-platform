using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Client;

[DocumentationInfo("Vessel client contract", "Inter-module client interface for reading vessel data from other modules.")]
public interface IVesselClient
{
    Task<VesselDetailDto?> GetVesselDetailAsync(Guid vesselId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselListItemDto>> GetCurrentUserVesselsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UserHasAccessToVesselAsync(Guid vesselId, Guid userId, CancellationToken cancellationToken = default);
}
