using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Modules.Content.Abstraction.Enum;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Redis-backed generation-bump implementation of <see cref="IContentCacheInvalidator"/>.
/// Keys: <c>content:gen</c> (global) and <c>content:gen:{surface}</c> (per surface). Best-effort —
/// a cache failure must never fail the write path (mirrors SendNotificationCommandHandler).
/// </summary>
public sealed class ContentCacheInvalidator : IContentCacheInvalidator
{
    public const string GlobalGenerationKey = "content:gen";

    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<ContentCacheInvalidator> _logger;

    public ContentCacheInvalidator(IAizenDistributedCache cache, ILogger<ContentCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public static string SurfaceGenerationKey(ContentSurface surface) => $"content:gen:{surface}";

    public async Task BumpAsync(IEnumerable<ContentSurface> surfaces, CancellationToken ct = default)
    {
        var generation = DateTimeOffset.UtcNow.Ticks;
        var options = new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) };

        try
        {
            await _cache.SetAsync(generation, GlobalGenerationKey, options, ct);
            foreach (var surface in surfaces.Distinct())
                await _cache.SetAsync(generation, SurfaceGenerationKey(surface), options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Content cache generation bump failed; feeds will refresh on TTL expiry.");
        }
    }
}
