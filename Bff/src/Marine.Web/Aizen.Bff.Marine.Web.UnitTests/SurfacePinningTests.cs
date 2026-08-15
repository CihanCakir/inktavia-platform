using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Content;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentByType;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentFeed;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W4 — surface pinning. The BFF always sends <see cref="ContentSurface.MarineOsWeb"/> to Content on the public
/// feed/by-type calls, and the web query types structurally have no <c>Surface</c> property — a caller cannot
/// choose another surface (e.g. Provider content) and leak it onto the website.
/// </summary>
public sealed class SurfacePinningTests
{
    [Fact]
    public async Task Feed_handler_pins_MarineOsWeb()
    {
        var content = new FakeContentRemoteCall { FeedResponse = new ContentFeedResponse() };
        var handler = new GetWebContentFeedQueryHandler(content, NullLogger<GetWebContentFeedQueryHandler>.Instance);

        await handler.Handle(new GetWebContentFeedQuery { Lang = "tr" }, default);

        content.LastFeedSurface.Should().Be(ContentSurface.MarineOsWeb);
        WebContentSurface.Pinned.Should().Be(ContentSurface.MarineOsWeb);
    }

    [Fact]
    public async Task ByType_handler_pins_MarineOsWeb()
    {
        var content = new FakeContentRemoteCall { FeedResponse = new ContentFeedResponse() };
        var handler = new GetWebContentByTypeQueryHandler(content, NullLogger<GetWebContentByTypeQueryHandler>.Instance);

        await handler.Handle(new GetWebContentByTypeQuery { Type = ContentType.Campaign, Lang = "tr" }, default);

        content.LastByTypeSurface.Should().Be(ContentSurface.MarineOsWeb);
    }

    [Fact]
    public void Web_feed_queries_have_no_Surface_property()
    {
        static string[] Props<T>() => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).ToArray();

        Props<GetWebContentFeedQuery>().Should().NotContain("Surface");
        Props<GetWebContentByTypeQuery>().Should().NotContain("Surface");
    }
}
