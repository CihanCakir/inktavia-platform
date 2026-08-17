using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Content;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using FluentAssertions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W4 — the security-critical field-stripping guarantees of <see cref="WebContentMapper"/>. The public website must
/// never receive internal author ids, targeting/placement, moderation trail, or lifecycle fields. Covered both
/// behaviourally (a populated module DTO maps to the intended web shape) and structurally (the web DTO type simply
/// has no property that could carry the forbidden field).
/// </summary>
public sealed class WebContentMapperTests
{
    private static string[] Props<T>() => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name).ToArray();

    private static ContentItemDto FullItem() => new()
    {
        Id = "c1",
        Type = ContentType.Blog,
        Slug = "spring-service",
        Status = ContentStatus.Published,              // internal lifecycle — must NOT surface
        PublishAt = DateTimeOffset.UtcNow.AddDays(-1),
        ExpireAt = DateTimeOffset.UtcNow.AddDays(30),  // internal lifecycle — must NOT surface
        DefaultLanguage = "tr",
        Translations = new()
        {
            new() { Lang = "tr", Title = "Bahar Bakımı", Summary = "TR özet", Body = "TR gövde" },
            new() { Lang = "en", Title = "Spring Service", Summary = "EN summary", Body = "EN body" },
        },
        Media = new()
        {
            new() { FileStorageId = "f-cover", Url = "https://cdn/cover.jpg", Kind = ContentMediaKind.Cover, Position = 0 },
            new() { FileStorageId = "f-g1", Url = "https://cdn/g1.jpg", Kind = ContentMediaKind.Gallery, Alt = "g1", Position = 1 },
        },
        Placements = new() { new() { Surface = ContentSurface.MarineOsWeb, Slot = ContentSlot.Feed, Position = 1 } },
        Audience = new() { Type = ContentAudienceType.Public, RegionCodes = { "TR-34" } },
        Tags = new() { "service", "spring" },
        CategorySlug = "maintenance",
        CommentCount = 3,
        FavoriteCount = 7,
        AuthorUserId = 999_001,                        // internal — must NOT surface
        PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
    };

    // ── by-slug detail ──────────────────────────────────────────────────────────

    [Fact]
    public void ToDetail_resolves_language_and_maps_public_fields()
    {
        var d = WebContentMapper.ToDetail(FullItem(), "en");

        d.Lang.Should().Be("en");
        d.Title.Should().Be("Spring Service");
        d.Body.Should().Be("EN body");
        d.CoverUrl.Should().Be("https://cdn/cover.jpg");
        d.Gallery.Should().ContainSingle().Which.Url.Should().Be("https://cdn/g1.jpg");
        d.Tags.Should().BeEquivalentTo("service", "spring");
        d.CommentCount.Should().Be(3);
        d.FavoriteCount.Should().Be(7);
    }

    [Fact]
    public void WebContentDetailDto_omits_internal_fields()
    {
        var names = Props<WebContentDetailDto>();
        names.Should().NotContain("AuthorUserId");
        names.Should().NotContain("Placements");
        names.Should().NotContain("Audience");
        names.Should().NotContain("Status");
        names.Should().NotContain("ExpireAt");
        names.Should().NotContain("Translations", "the full multi-language set never leaves the BFF; one language is resolved");
        names.Should().NotContain("UpdatedAt");
    }

    [Fact]
    public void WebContentMediaDto_omits_fileStorageId()
        => Props<WebContentMediaDto>().Should().NotContain("FileStorageId");

    // ── public approved comments ─────────────────────────────────────────────────

    [Fact]
    public void ToComments_keeps_display_name_and_strips_author_and_moderation()
    {
        var src = new ContentCommentsResponse
        {
            Items = new()
            {
                new()
                {
                    Id = "m1", ContentId = "c1", AuthorUserId = 500, AuthorProfileId = 900,
                    AuthorDisplayName = "Deniz K.", Body = "Harika!", Status = ContentCommentStatus.Approved,
                    ModeratedByUserId = 42, ModeratedAt = DateTimeOffset.UtcNow, LastModerationReason = "ok",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            },
            Page = 1, PageSize = 20, Total = 1,
        };

        var r = WebContentMapper.ToComments(src);

        r.Items.Should().ContainSingle();
        r.Items[0].AuthorDisplayName.Should().Be("Deniz K.");
        r.Items[0].Body.Should().Be("Harika!");
    }

    [Fact]
    public void WebContentCommentDto_omits_author_ids_status_and_moderation_trail()
    {
        var names = Props<WebContentCommentDto>();
        names.Should().NotContain("AuthorUserId");
        names.Should().NotContain("AuthorProfileId");
        names.Should().NotContain("Status", "the public must not see moderation status of others' comments");
        names.Should().NotContain("ModeratedByUserId");
        names.Should().NotContain("ModeratedAt");
        names.Should().NotContain("LastModerationReason");
    }

    // ── /me my-comments (keeps OWN status) ───────────────────────────────────────

    [Fact]
    public void ToMyComment_keeps_own_status_but_strips_author_and_moderation()
    {
        var src = new ContentCommentDto
        {
            Id = "m1", ContentId = "c1", AuthorUserId = 500, AuthorProfileId = 900, AuthorDisplayName = "Me",
            Body = "Benim yorumum", Status = ContentCommentStatus.Pending, ParentCommentId = null,
            ModeratedByUserId = 42, LastModerationReason = "queued", CreatedAt = DateTimeOffset.UtcNow,
        };

        var m = WebContentMapper.ToMyComment(src);

        m.Status.Should().Be(ContentCommentStatus.Pending, "the caller may see their own moderation status");
        m.Body.Should().Be("Benim yorumum");
    }

    [Fact]
    public void WebMyCommentDto_keeps_status_but_omits_author_ids_and_moderation_trail()
    {
        var names = Props<WebMyCommentDto>();
        names.Should().Contain("Status", "a participant sees their OWN comment's status");
        names.Should().NotContain("AuthorUserId");
        names.Should().NotContain("AuthorProfileId");
        names.Should().NotContain("AuthorDisplayName");
        names.Should().NotContain("ModeratedByUserId");
        names.Should().NotContain("ModeratedAt");
        names.Should().NotContain("LastModerationReason");
    }

    // ── /me my-favorites ─────────────────────────────────────────────────────────

    [Fact]
    public void ToMyFavorites_maps_and_strips_participant_ids()
    {
        var src = new ContentFavoritesResponse
        {
            Items = new()
            {
                new()
                {
                    Id = "fav1", ContentId = "c1", UserId = 500, ProfileId = 900,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Content = new ContentItemSummaryDto { Id = "c1", Slug = "spring-service", Title = "Spring" },
                },
            },
            Page = 1, PageSize = 20, Total = 1,
        };

        var r = WebContentMapper.ToMyFavorites(src);

        r.Items.Should().ContainSingle();
        r.Items[0].ContentId.Should().Be("c1");
        r.Items[0].Content!.Slug.Should().Be("spring-service");
    }

    [Fact]
    public void WebMyFavoriteDto_omits_participant_internal_ids()
    {
        var names = Props<WebMyFavoriteDto>();
        names.Should().NotContain("UserId");
        names.Should().NotContain("ProfileId");
    }
}
