using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Commands.CreateContentItem;
using Aizen.Modules.Content.Application.Commands.UpdateContentItem;
using Aizen.Modules.Content.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

/// <summary>
/// W3.4 — reserved slugs are rejected at authoring time so an entity can never take a static route segment the site
/// resolves first (which would make it permanently, silently unreachable). Single source of truth is
/// <see cref="ReservedSlugs"/>; enforced by the create/update validators and honoured by <see cref="SlugService"/>.
/// </summary>
public sealed class ReservedSlugsTests
{
    [Theory]
    // Technical / framework / SEO segments (never localized).
    [InlineData("search")] [InlineData("request")] [InlineData("new")] [InlineData("all")] [InlineData("compare")]
    [InlineData("index")] [InlineData("api")] [InlineData("admin")] [InlineData("sitemap")] [InlineData("robots")]
    [InlineData("assets")] [InlineData("static")] [InlineData("_next")]
    // Turkish equivalents of the user-facing segments.
    [InlineData("ara")] [InlineData("talep")] [InlineData("yeni")] [InlineData("tumu")] [InlineData("karsilastir")]
    // Case-insensitive.
    [InlineData("SEARCH")] [InlineData("Api")] [InlineData("_Next")]
    public void Reserved_slugs_are_recognized(string slug)
        => ReservedSlugs.IsReserved(slug).Should().BeTrue($"'{slug}' collides with a static route segment");

    [Theory]
    [InlineData("anchoring-guide")]
    [InlineData("winter-haul-out")]
    [InlineData("searchlight")]     // contains "search" but is not equal to it
    [InlineData(null)]
    [InlineData("")]
    public void Non_reserved_slugs_are_allowed(string? slug)
        => ReservedSlugs.IsReserved(slug).Should().BeFalse();

    // ── create validator ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("sitemap")]
    [InlineData("ADMIN")]
    [InlineData("ara")]
    public void Create_validator_rejects_a_reserved_slug(string slug)
    {
        var result = new CreateContentItemCommandValidator().Validate(NewCreate(slug));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateContentItemCommand.Slug));
    }

    [Fact]
    public void Create_validator_allows_a_normal_or_auto_slug()
    {
        var validator = new CreateContentItemCommandValidator();
        validator.Validate(NewCreate("anchoring-guide")).Errors
            .Should().NotContain(e => e.PropertyName == nameof(CreateContentItemCommand.Slug));
        // Null slug (module auto-generates later) must not trip the reserved rule.
        validator.Validate(NewCreate(null)).Errors
            .Should().NotContain(e => e.PropertyName == nameof(CreateContentItemCommand.Slug));
    }

    // ── update validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Update_validator_rejects_a_reserved_reslug_but_allows_a_normal_one()
    {
        var validator = new UpdateContentItemCommandValidator();
        validator.Validate(new UpdateContentItemCommand { ContentId = "c1", Slug = "robots" }).Errors
            .Should().Contain(e => e.PropertyName == nameof(UpdateContentItemCommand.Slug));
        validator.Validate(new UpdateContentItemCommand { ContentId = "c1", Slug = "winter-tips" }).Errors
            .Should().NotContain(e => e.PropertyName == nameof(UpdateContentItemCommand.Slug));
    }

    // ── auto-generation honours the reserved list ─────────────────────────────────

    [Fact]
    public async Task SlugService_suffixes_an_auto_slug_that_lands_on_a_reserved_segment()
    {
        var svc = new SlugService(new InMemoryItemRepo());
        // A title "Search" slugifies to the reserved "search" → must be suffixed, never emitted bare.
        (await svc.GenerateUniqueSlugAsync("Search")).Should().Be("search-2");
    }

    private static CreateContentItemCommand NewCreate(string? slug) => new()
    {
        Type = ContentType.Blog,
        Slug = slug,
        DefaultLanguage = "tr",
        AuthorUserId = 1,
        Translations = new() { new ContentTranslationDto { Lang = "tr", Title = "Başlık" } },
    };
}
