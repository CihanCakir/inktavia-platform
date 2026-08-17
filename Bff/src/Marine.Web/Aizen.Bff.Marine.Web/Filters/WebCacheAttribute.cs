using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Aizen.Bff.Marine.Web.Filters;

/// <summary>
/// Emits an HTTP freshness contract on a public read so the server-rendered Next.js consumer can cache with ISR:
///   • <c>Cache-Control: public, max-age=&lt;m&gt;, stale-while-revalidate=&lt;s&gt;</c> (per-resource, from the attribute).
///   • A weak <c>ETag</c> derived from the response body, plus <c>304 Not Modified</c> on a matching <c>If-None-Match</c>
///     (conditional requests are cheap — no body re-sent).
/// The per-resource numbers are the SINGLE source of truth for the README freshness table (the consts below).
/// Applied per public GET endpoint; the authenticated /me surface is never cached.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WebCacheAttribute : Attribute, IResultFilter
{
    // Freshness presets (seconds) — keep in lockstep with the README "resource → staleness tolerance" table.
    public const int ReferenceMaxAge = 3600,  ReferenceSwr = 86400;   // countries / cities / lookups — change rarely
    public const int CategoriesMaxAge = 1800, CategoriesSwr = 86400;  // content categories — change rarely
    public const int DetailMaxAge = 300,      DetailSwr = 3600;       // content by-slug detail — moderate
    public const int FeedMaxAge = 60,         FeedSwr = 300;          // content feed / by-type — changes more often
    public const int CommentsMaxAge = 30,     CommentsSwr = 120;      // approved comments — most volatile
    public const int SlugFeedMaxAge = 300,    SlugFeedSwr = 3600;     // sitemap slug feed — moderate (tolerates lag)
    public const int ServicesMaxAge = 3600,   ServicesSwr = 86400;    // W4 service catalogue (lookup taxonomy) — change rarely
    public const int LocationsMaxAge = 3600,  LocationsSwr = 86400;   // W4 location detail (reference geography) — change rarely
    public const int PricingMaxAge = 1800,    PricingSwr = 86400;     // W4 public pricing (published plan terms) — change rarely
    public const int ServicePageMaxAge = 300, ServicePageSwr = 3600;  // M2 service×location landing (coarse availability) — moderate

    private readonly int _maxAge;
    private readonly int _staleWhileRevalidate;

    public WebCacheAttribute(int maxAgeSeconds, int staleWhileRevalidateSeconds)
    {
        _maxAge = maxAgeSeconds;
        _staleWhileRevalidate = staleWhileRevalidateSeconds;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method))
            return;

        var response = context.HttpContext.Response;

        // Cache-Control always (public, server-to-server ISR + browser dev).
        response.Headers[HeaderNames.CacheControl] =
            $"public, max-age={_maxAge}, stale-while-revalidate={_staleWhileRevalidate}";

        // ETag from the serialized payload (envelope included → stable per content).
        if (context.Result is not ObjectResult { Value: { } value })
            return;

        var etag = ComputeWeakETag(value);
        response.Headers[HeaderNames.ETag] = etag;

        var ifNoneMatch = request.Headers[HeaderNames.IfNoneMatch].ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch.Contains(etag, StringComparison.Ordinal))
            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // No post-processing needed.
    }

    private static string ComputeWeakETag(object value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var hash = SHA256.HashData(bytes);
        // First 16 bytes are ample for a cache validator; base64url, no padding.
        var token = Convert.ToBase64String(hash, 0, 16)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"W/\"{token}\"";
    }
}
