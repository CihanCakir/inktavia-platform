using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Queries.GetPublicContentFeed;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Content.Application.UnitTests;

/// <summary>
/// Minimal integration-style test of the public feed handler over the in-memory repo + cache:
/// exercises the Mongo predicate (status/audience/surface) → projection (window/order) → cache path.
/// </summary>
public sealed class PublicFeedHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Returns_only_in_window_public_items_for_the_surface_then_serves_from_cache()
    {
        var repo = new InMemoryItemRepo();
        repo.Store.Add(TestData.Item(publishAt: Now.AddHours(-1), surface: ContentSurface.MarineOsWeb, slug: "live"));
        repo.Store.Add(TestData.Item(publishAt: Now.AddHours(-3), expireAt: Now.AddMinutes(-1), surface: ContentSurface.MarineOsWeb)); // expired
        repo.Store.Add(TestData.Item(publishAt: Now.AddHours(1), surface: ContentSurface.MarineOsWeb));  // future
        repo.Store.Add(TestData.Item(publishAt: Now.AddHours(-1), surface: ContentSurface.Provider));    // other surface
        repo.Store.Add(TestData.Item(publishAt: Now.AddHours(-1), surface: ContentSurface.MarineOsWeb, audience: ContentAudienceType.Providers)); // non-public

        var cache = new DictCache();
        var handler = new GetPublicContentFeedQueryHandler(repo, cache, NullLogger<GetPublicContentFeedQueryHandler>.Instance);

        var first = await handler.Handle(new GetPublicContentFeedQuery { Surface = ContentSurface.MarineOsWeb, Lang = "tr" }, default);
        first.Total.Should().Be(1);
        first.Items.Single().Slug.Should().Be("live");
        cache.Sets.Should().Be(1); // computed + stored

        var second = await handler.Handle(new GetPublicContentFeedQuery { Surface = ContentSurface.MarineOsWeb, Lang = "tr" }, default);
        second.Total.Should().Be(1);
        cache.Sets.Should().Be(1); // served from cache — no recompute/store
    }
}
