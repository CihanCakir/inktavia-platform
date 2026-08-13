using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Public-read cache invalidation seam. Publish/unpublish/edit bump a monotonic generation value;
/// the C5 public feed folds the current generation into its cache key, so a bump transparently
/// invalidates cached feeds without deleting individual entries.
/// </summary>
public interface IContentCacheInvalidator
{
    /// <summary>Bump the global generation and the generation for each affected surface. Best-effort.</summary>
    Task BumpAsync(IEnumerable<ContentSurface> surfaces, CancellationToken ct = default);
}
