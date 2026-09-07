using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

/// <summary>
/// Catalog-side of a duplicate merge: validate (same type; same brand for models; source ≠ target; both exist),
/// then deactivate the SOURCE with a merge audit (MergedIntoId = target). Idempotent — re-merging an already-merged
/// source is a safe no-op. The Vessel FK repoint is a separate Vessel-module concern (this module doesn't own it).
/// </summary>
public interface ICatalogMergeService
{
    Task<CatalogMergeResultDto> MergeAsync(CatalogMergeType type, long sourceId, long targetId, CancellationToken cancellationToken = default);
}
