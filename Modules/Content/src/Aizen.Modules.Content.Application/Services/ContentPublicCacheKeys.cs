using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Cache-key composition and TTLs for the public read-side. Keys fold in the current content
/// generation values (bumped by the C4 authoring handlers via <see cref="IContentCacheInvalidator"/>),
/// so a publish/unpublish transparently invalidates the affected entries — no explicit key deletion.
///
/// Composition:
///   feed  : content:feed:v1:{surface}:{lang}:{page}:{pageSize}:g{globalGen}:s{surfaceGen}
///   type  : content:type:v1:{surface}:{type}:{lang}:{page}:{pageSize}:g{globalGen}:s{surfaceGen}
///   slug  : content:slug:v1:{slug}:g{globalGen}
///   cats  : content:categories:v1:{lang}:g{globalGen}
/// Both the global generation and (for surface-scoped reads) the per-surface generation are included,
/// so any publish/unpublish (which bumps both) invalidates the read.
/// </summary>
public static class ContentPublicCacheKeys
{
    public static readonly TimeSpan FeedTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan CategoryTtl = TimeSpan.FromMinutes(30);

    public static async Task<long> ReadGenerationAsync(IAizenDistributedCache cache, string key, CancellationToken ct)
    {
        var (hit, value) = await cache.TryGetAsync<long>(key, ct);
        return hit ? value : 0L;
    }

    public static async Task<(long Global, long Surface)> ReadGenerationsAsync(
        IAizenDistributedCache cache, ContentSurface surface, CancellationToken ct)
    {
        var global = await ReadGenerationAsync(cache, ContentCacheInvalidator.GlobalGenerationKey, ct);
        var surfaceGen = await ReadGenerationAsync(cache, ContentCacheInvalidator.SurfaceGenerationKey(surface), ct);
        return (global, surfaceGen);
    }

    public static string Feed(ContentSurface surface, string lang, int page, int pageSize, long global, long surfaceGen)
        => $"content:feed:v1:{surface}:{lang}:{page}:{pageSize}:g{global}:s{surfaceGen}";

    public static string ByType(ContentSurface surface, ContentType type, string lang, int page, int pageSize, long global, long surfaceGen)
        => $"content:type:v1:{surface}:{type}:{lang}:{page}:{pageSize}:g{global}:s{surfaceGen}";

    public static string BySlug(string slug, string lang, long global)
        => $"content:slug:v1:{slug}:{lang}:g{global}";

    public static string CategoryTree(string lang, long global)
        => $"content:categories:v1:{lang}:g{global}";
}
