using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IMarinaReferenceService
{
    /// <summary>Returns the closest active marinas to the given position, ordered by ascending great-circle distance.</summary>
    Task<IReadOnlyList<MarinaNearbyDto>> GetNearbyAsync(double latitude, double longitude, int limit, CancellationToken cancellationToken = default);

    Task<MarinaDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    // ── Admin curation ──
    Task<MarinaAdminListResult> ListForAdminAsync(bool needsReviewOnly, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(long id, string name, string? cityCode, CancellationToken cancellationToken = default);
    Task<bool> MarkReviewedAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken = default);
}
