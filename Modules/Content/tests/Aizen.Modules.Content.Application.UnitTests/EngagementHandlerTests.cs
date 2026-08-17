using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Commands.AddContentFavorite;
using Aizen.Modules.Content.Application.Commands.RemoveContentFavorite;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class EngagementHandlerTests
{
    [Fact]
    public async Task Favorite_is_idempotent_and_re_addable_after_remove()
    {
        var items = new InMemoryItemRepo();
        var favs = new InMemoryFavoriteRepo();
        var info = new FakeInfo { UserId = 42 };
        var item = TestData.Item(publishAt: DateTimeOffset.UtcNow.AddHours(-1));
        items.Store.Add(item);

        var add = new AddContentFavoriteCommandHandler(items, favs, info);
        var remove = new RemoveContentFavoriteCommandHandler(items, favs, info);

        (await add.Handle(new AddContentFavoriteCommand { ContentId = item.Id }, default))!.FavoriteCount.Should().Be(1);
        item.FavoriteCount.Should().Be(1);

        // duplicate add → no-op
        (await add.Handle(new AddContentFavoriteCommand { ContentId = item.Id }, default))!.FavoriteCount.Should().Be(1);
        item.FavoriteCount.Should().Be(1);

        // remove → 0
        var r = await remove.Handle(new RemoveContentFavoriteCommand { ContentId = item.Id }, default);
        r!.Favorited.Should().BeFalse(); r.FavoriteCount.Should().Be(0);
        item.FavoriteCount.Should().Be(0);

        // re-add after remove succeeds (partial index releases the soft-deleted row)
        (await add.Handle(new AddContentFavoriteCommand { ContentId = item.Id }, default))!.FavoriteCount.Should().Be(1);
        item.FavoriteCount.Should().Be(1);
    }

    [Fact]
    public async Task Favorite_on_unpublished_is_rejected()
    {
        var items = new InMemoryItemRepo();
        var item = TestData.Item(status: ContentStatus.Draft, publishAt: null);
        items.Store.Add(item);
        var add = new AddContentFavoriteCommandHandler(items, new InMemoryFavoriteRepo(), new FakeInfo { UserId = 42 });

        await FluentActions.Awaiting(() => add.Handle(new AddContentFavoriteCommand { ContentId = item.Id }, default))
            .Should().ThrowAsync<AizenBusinessException>();
    }
}
