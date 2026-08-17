using Aizen.Bff.Marine.Web.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — the W2.2 freshness contract emitted by <see cref="WebCacheAttribute"/>: a per-resource <c>Cache-Control</c>,
/// a weak body-derived <c>ETag</c>, and a <c>304</c> on a matching <c>If-None-Match</c> so conditional requests are
/// cheap. This is what lets the Next.js server run ISR.
/// </summary>
public sealed class WebCacheTests
{
    private static ResultExecutingContext ExecutingContext(HttpContext http, object? value)
    {
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new ObjectResult(value),
            controller: new object());
    }

    private static (string cacheControl, string etag) RunGet(object value, string? ifNoneMatch = null, HttpContext? shared = null)
    {
        var http = shared ?? new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        if (ifNoneMatch is not null)
            http.Request.Headers[HeaderNames.IfNoneMatch] = ifNoneMatch;

        var ctx = ExecutingContext(http, value);
        new WebCacheAttribute(300, 3600).OnResultExecuting(ctx);

        return (http.Response.Headers[HeaderNames.CacheControl].ToString(),
                http.Response.Headers[HeaderNames.ETag].ToString());
    }

    [Fact]
    public void Emits_cache_control_and_weak_etag_on_get()
    {
        var (cacheControl, etag) = RunGet(new { id = 1, name = "x" });

        cacheControl.Should().Be("public, max-age=300, stale-while-revalidate=3600");
        etag.Should().StartWith("W/\"").And.EndWith("\"");
    }

    [Fact]
    public void Same_payload_yields_stable_etag_and_different_payload_changes_it()
    {
        var a1 = RunGet(new { id = 1 }).etag;
        var a2 = RunGet(new { id = 1 }).etag;
        var b = RunGet(new { id = 2 }).etag;

        a1.Should().Be(a2, "the weak ETag is derived from the serialized body");
        a1.Should().NotBe(b);
    }

    [Fact]
    public void Matching_if_none_match_short_circuits_to_304()
    {
        var value = new { id = 42, name = "spring" };
        var etag = RunGet(value).etag;

        // Second request presents the ETag → the filter must replace the result with 304 Not Modified.
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Headers[HeaderNames.IfNoneMatch] = etag;
        var ctx = ExecutingContext(http, value);
        new WebCacheAttribute(300, 3600).OnResultExecuting(ctx);

        ctx.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact]
    public void Non_get_requests_are_not_cached()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Post;
        var ctx = ExecutingContext(http, new { id = 1 });
        new WebCacheAttribute(300, 3600).OnResultExecuting(ctx);

        http.Response.Headers.ContainsKey(HeaderNames.CacheControl).Should().BeFalse();
    }
}
