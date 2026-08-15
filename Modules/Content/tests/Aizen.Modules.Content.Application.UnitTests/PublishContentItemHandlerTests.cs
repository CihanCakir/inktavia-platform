using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Message;
using Aizen.Modules.Content.Application.Commands.PublishContentItem;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class PublishContentItemHandlerTests
{
    private static (PublishContentItemCommandHandler h, InMemoryItemRepo repo, RecordingPublisher pub, RecordingCacheInvalidator cache)
        Build(string[] roles)
    {
        var repo = new InMemoryItemRepo();
        var pub = new RecordingPublisher();
        var cache = new RecordingCacheInvalidator();
        var h = new PublishContentItemCommandHandler(
            repo, new FakeInfo { UserId = 9, Roles = roles }, pub, cache,
            NullLogger<PublishContentItemCommandHandler>.Instance);
        return (h, repo, pub, cache);
    }

    private static readonly string[] Admin = { "ContentAdmin" };
    private static readonly string[] Editor = { "ContentEditor" };

    [Fact]
    public async Task Editor_is_blocked_from_publish()
    {
        var (h, repo, _, _) = Build(Editor);
        var item = TestData.Item(status: ContentStatus.Draft);
        repo.Store.Add(item);

        await FluentActions.Awaiting(() => h.Handle(new PublishContentItemCommand { ContentId = item.Id }, default))
            .Should().ThrowAsync<AizenBusinessException>().WithMessage("*elevated*");
    }

    [Fact]
    public async Task Publish_requires_a_placement()
    {
        var (h, repo, _, _) = Build(Admin);
        var item = TestData.Item(status: ContentStatus.Draft);
        item.Placements.Clear();
        repo.Store.Add(item);

        await FluentActions.Awaiting(() => h.Handle(new PublishContentItemCommand { ContentId = item.Id }, default))
            .Should().ThrowAsync<AizenBusinessException>().WithMessage("*placement*");
    }

    [Fact]
    public async Task Publish_requires_a_default_language_title()
    {
        var (h, repo, _, _) = Build(Admin);
        var item = new ContentItemDocument
        {
            Type = ContentType.Blog, Slug = "x", Status = ContentStatus.Draft, DefaultLanguage = "tr",
            Translations = { new ContentTranslation { Lang = "en", Title = "only en" } },
            Placements = { new ContentPlacement { Surface = ContentSurface.MarineOsWeb, Slot = ContentSlot.Feed } },
        };
        repo.Store.Add(item);

        await FluentActions.Awaiting(() => h.Handle(new PublishContentItemCommand { ContentId = item.Id }, default))
            .Should().ThrowAsync<AizenBusinessException>().WithMessage("*default language*");
    }

    [Fact]
    public async Task Publish_success_sets_state_emits_event_and_bumps_cache()
    {
        var (h, repo, pub, cache) = Build(Admin);
        var item = TestData.Item(status: ContentStatus.Draft, surface: ContentSurface.MarineOsWeb);
        repo.Store.Add(item);

        var dto = await h.Handle(new PublishContentItemCommand { ContentId = item.Id }, default);

        dto!.Status.Should().Be(ContentStatus.Published);
        dto.PublishedAt.Should().NotBeNull();
        pub.Published.Should().ContainSingle().Which.Should().BeOfType<ContentPublishedMessage>();
        ((ContentPublishedMessage)pub.Published[0]).Surfaces.Should().Contain("MarineOsWeb");
        cache.Bumps.Should().Be(1);
        cache.LastSurfaces.Should().Contain("MarineOsWeb");
    }
}
