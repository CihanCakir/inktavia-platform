using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class ContentFeedProjectionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Excludes_expired_future_and_wrong_surface()
    {
        var live     = TestData.Item(publishAt: Now.AddHours(-1), surface: ContentSurface.MarineOsWeb);
        var expired  = TestData.Item(publishAt: Now.AddHours(-3), expireAt: Now.AddMinutes(-5), surface: ContentSurface.MarineOsWeb);
        var future   = TestData.Item(publishAt: Now.AddHours(1), surface: ContentSurface.MarineOsWeb);
        var noPublish = TestData.Item(publishAt: null, surface: ContentSurface.MarineOsWeb);
        var other    = TestData.Item(publishAt: Now.AddHours(-1), surface: ContentSurface.Provider);

        var res = ContentFeedProjection.Build(
            new[] { live, expired, future, noPublish, other }, ContentSurface.MarineOsWeb, "tr", 1, 20, Now);

        res.Total.Should().Be(1);
        res.Items.Single().Id.Should().Be(live.Id);
    }

    [Fact]
    public void Orders_pinned_then_position_then_publishAt_desc()
    {
        var pinned = TestData.Item(publishAt: Now.AddMinutes(-30), position: 3, pinnedUntil: Now.AddHours(1), slug: "pinned");
        var pos1   = TestData.Item(publishAt: Now.AddHours(-2), position: 1, slug: "pos1");
        var pos2   = TestData.Item(publishAt: Now.AddHours(-1), position: 2, slug: "pos2");

        var res = ContentFeedProjection.Build(
            new[] { pos2, pinned, pos1 }, ContentSurface.MarineOsWeb, "tr", 1, 20, Now);

        res.Items.Select(i => i.Slug).Should().ContainInOrder("pinned", "pos1", "pos2");
    }

    [Fact]
    public void Resolves_requested_language_then_falls_back_to_default()
    {
        var bilingual = TestData.Item(publishAt: Now.AddHours(-1), defaultLang: "tr",
            translations: new[] { ("tr", "TR"), ("en", "EN") }, slug: "bi");
        var trOnly = TestData.Item(publishAt: Now.AddHours(-2), defaultLang: "tr",
            translations: new[] { ("tr", "OnlyTR") }, slug: "tronly");

        var res = ContentFeedProjection.Build(new[] { bilingual, trOnly }, ContentSurface.MarineOsWeb, "en", 1, 20, Now);

        var bi = res.Items.Single(i => i.Slug == "bi");
        bi.Lang.Should().Be("en"); bi.Title.Should().Be("EN");
        var tr = res.Items.Single(i => i.Slug == "tronly");
        tr.Lang.Should().Be("tr"); tr.Title.Should().Be("OnlyTR"); // fallback to default language
    }

    [Fact]
    public void Paging_reports_total_and_slices()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => TestData.Item(publishAt: Now.AddHours(-i - 1), position: 1)).ToArray();

        var page2 = ContentFeedProjection.Build(items, ContentSurface.MarineOsWeb, "tr", 2, 2, Now);
        page2.Total.Should().Be(5);
        page2.Items.Should().HaveCount(2);
        page2.Page.Should().Be(2);
    }
}
