using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.Options;
using Aizen.Bff.Marine.Web.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — the W2.1 trusted server-caller decision behind the <c>public-read-ip</c> policy. A valid secret routes the
/// caller to the shared high/unlimited trusted partition; missing/invalid credentials, and (critically) an unset
/// configured secret, keep the caller on the strict per-IP window (fail-closed). The compare is constant-time.
/// </summary>
public sealed class TrustedWebCallerTests
{
    private const string Secret = "s3rv3r-side-secret-value";

    private static HttpContext Context(string? header, string ip = "203.0.113.7")
    {
        var ctx = new DefaultHttpContext();
        if (header is not null)
            ctx.Request.Headers[MarineWebPublicOptions.TrustedCallerHeader] = header;
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return ctx;
    }

    [Fact]
    public void Valid_secret_is_trusted_and_uses_shared_partition()
    {
        var ctx = Context(Secret);
        TrustedWebCaller.IsTrusted(ctx, Secret).Should().BeTrue();
        TrustedWebCaller.PartitionKeyFor(ctx, Secret).Should().Be(TrustedWebCaller.TrustedPartitionKey);
    }

    [Fact]
    public void Missing_header_falls_back_to_per_ip_partition()
    {
        var ctx = Context(header: null, ip: "198.51.100.9");
        TrustedWebCaller.IsTrusted(ctx, Secret).Should().BeFalse();
        TrustedWebCaller.PartitionKeyFor(ctx, Secret).Should().Be("198.51.100.9");
    }

    [Fact]
    public void Wrong_secret_is_not_trusted()
    {
        var ctx = Context("not-the-secret");
        TrustedWebCaller.IsTrusted(ctx, Secret).Should().BeFalse();
        TrustedWebCaller.PartitionKeyFor(ctx, Secret).Should().Be("203.0.113.7");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unset_configured_secret_is_fail_closed_even_with_a_header(string? configured)
    {
        // Even when the caller presents a header value, an empty/unset configured secret must never trust anyone.
        var ctx = Context("anything-the-caller-sends");
        TrustedWebCaller.IsTrusted(ctx, configured).Should().BeFalse();
        TrustedWebCaller.PartitionKeyFor(ctx, configured).Should().Be("203.0.113.7");
    }

    [Fact]
    public void Length_mismatch_is_handled_by_constant_time_compare()
    {
        // FixedTimeEquals over differing-length inputs returns false without throwing (exercises the compare path).
        TrustedWebCaller.IsTrusted(Context("short"), Secret).Should().BeFalse();
        TrustedWebCaller.IsTrusted(Context(Secret + "-longer"), Secret).Should().BeFalse();
    }

    [Fact]
    public void Unknown_ip_uses_the_unknown_partition_key()
    {
        var ctx = new DefaultHttpContext();   // no RemoteIpAddress set
        TrustedWebCaller.PartitionKeyFor(ctx, Secret).Should().Be(TrustedWebCaller.UnknownIpPartitionKey);
    }
}
