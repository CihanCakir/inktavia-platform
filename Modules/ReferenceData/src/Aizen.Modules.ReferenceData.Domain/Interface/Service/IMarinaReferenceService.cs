using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IMarinaReferenceService
{
    /// <summary>Returns the closest active marinas to the given position, ordered by ascending great-circle distance.</summary>
    Task<IReadOnlyList<MarinaNearbyDto>> GetNearbyAsync(double latitude, double longitude, int limit, CancellationToken cancellationToken = default);

    Task<MarinaDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
