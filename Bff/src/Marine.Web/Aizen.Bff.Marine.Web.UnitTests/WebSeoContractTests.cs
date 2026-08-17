using Aizen.Bff.Marine.Web.Application.Common.Options;
using Aizen.Bff.Marine.Web.Application.Common.Seo;
using Aizen.Bff.Marine.Web.Application.Content;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;
using Aizen.Bff.Marine.Web.Application.Seo.Query.GetWebSeoSlugs;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — the W3 SEO contract end to end: <c>AvailableLangs</c> on content detail (from the translation set), the
/// <c>Seo</c> block populated by the Options-backed policy, and the slug feed carrying
/// <c>slug/availableLangs/lastModified/indexable</c>.
/// </summary>
public sealed class WebSeoContractTests
{
    private static ISeoIndexabilityPolicy Policy(MarineWebPublicOptions.SeoOptions? seo = null)
    {
        var options = new MarineWebPublicOptions { Seo = seo ?? new() };
        return new OptionsSeoIndexabilityPolicy(new StaticMonitor(options));
    }

    private sealed class StaticMonitor : IOptionsMonitor<MarineWebPublicOptions>
    {
        public StaticMonitor(MarineWebPublicOptions value) => CurrentValue = value;
        public MarineWebPublicOptions CurrentValue { get; }
        public MarineWebPublicOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<MarineWebPublicOptions, string?> listener) => null;
    }

    // ── W3.1 detail AvailableLangs (from the item's translation set) ──────────────

    [Fact]
    public void Detail_available_langs_come_from_the_translation_set()
    {
        var item = new ContentItemDto
        {
            Id = "c1", Type = ContentType.Blog, Slug = "anchoring", Status = ContentStatus.Published,
            DefaultLanguage = "tr",
            Translations = new()
            {
                new() { Lang = "tr", Title = "Demirleme", Body = "gövde" },
                new() { Lang = "en", Title = "Anchoring", Body = "body" },
            },
        };

        WebContentMapper.ToDetail(item, "en").AvailableLangs.Should().Equal("tr", "en");
    }

    // ── W3.2 detail Seo block from the policy ─────────────────────────────────────

    [Fact]
    public async Task Detail_handler_sets_seo_indexable_from_the_policy()
    {
        var body = new string('x', 500);
        var content = new FakeContentRemoteCall
        {
            BySlugResponse = new ContentItemDto
            {
                Id = "c1", Type = ContentType.Blog, Slug = "anchoring", Status = ContentStatus.Published,
                DefaultLanguage = "tr",
                Translations = new()
                {
                    new() { Lang = "tr", Title = "Demirleme", Summary = "özet", Body = body, SeoTitle = "SEO T", SeoDescription = "SEO D" },
                },
            },
        };

        var handler = new GetWebContentBySlugQueryHandler(
            content, Policy(), NullLogger<GetWebContentBySlugQueryHandler>.Instance);

        var detail = await handler.Handle(new GetWebContentBySlugQuery { Slug = "anchoring", Lang = "tr" }, default);

        detail!.Seo.Should().NotBeNull();
        detail.Seo!.Indexable.Should().BeTrue();
        detail.Seo.Reason.Should().Be("indexable");
        detail.Seo.Title.Should().Be("SEO T");
        detail.Seo.Description.Should().Be("SEO D");
    }

    [Fact]
    public async Task Detail_handler_reports_thin_content_when_options_demand_more_body()
    {
        var content = new FakeContentRemoteCall
        {
            BySlugResponse = new ContentItemDto
            {
                Id = "c1", Type = ContentType.Blog, Slug = "stub", Status = ContentStatus.Published,
                DefaultLanguage = "tr",
                Translations = new() { new() { Lang = "tr", Title = "Kısa", Body = "short" } },
            },
        };

        var handler = new GetWebContentBySlugQueryHandler(
            content, Policy(new() { MinBodyLength = 200 }), NullLogger<GetWebContentBySlugQueryHandler>.Instance);

        var detail = await handler.Handle(new GetWebContentBySlugQuery { Slug = "stub", Lang = "tr" }, default);

        detail!.Seo!.Indexable.Should().BeFalse();
        detail.Seo.Reason.Should().Contain("thin-content");
    }

    // ── W3.3 slug feed ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Slug_feed_returns_slug_langs_lastModified_and_indexable()
    {
        var published = DateTimeOffset.UtcNow.AddDays(-2);
        var content = new FakeContentRemoteCall
        {
            FeedResponse = new ContentFeedResponse
            {
                Items = new()
                {
                    new()
                    {
                        Id = "c1", Slug = "anchoring", Title = "Anchoring", Summary = "How to anchor",
                        Lang = "tr", AvailableLangs = new() { "tr", "en" }, PublishedAt = published,
                    },
                },
                Page = 1, PageSize = 100, Total = 1,
            },
        };

        var handler = new GetWebSeoSlugsQueryHandler(
            content, Policy(), NullLogger<GetWebSeoSlugsQueryHandler>.Instance);

        var rows = await handler.Handle(new GetWebSeoSlugsQuery { EntityType = "content", Lang = "tr" }, default);

        rows.Should().ContainSingle();
        rows![0].Slug.Should().Be("anchoring");
        rows[0].AvailableLangs.Should().Equal("tr", "en");
        rows[0].LastModified.Should().Be(published);
        rows[0].Indexable.Should().BeTrue();
    }

    [Fact]
    public async Task Slug_feed_rejects_unknown_entity_type_without_faking()
    {
        var handler = new GetWebSeoSlugsQueryHandler(
            new FakeContentRemoteCall(), Policy(), NullLogger<GetWebSeoSlugsQueryHandler>.Instance);

        var act = () => handler.Handle(new GetWebSeoSlugsQuery { EntityType = "services", Lang = "tr" }, default);

        await act.Should().ThrowAsync<Aizen.Core.Infrastructure.Exception.AizenBusinessException>();
    }
}
