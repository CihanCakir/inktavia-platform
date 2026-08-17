using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Commands.DeleteContentComment;
using Aizen.Modules.Content.Application.Commands.ModerateContentComment;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class ModerationHandlerTests
{
    [Fact]
    public async Task CommentCount_follows_the_delta_matrix_across_the_full_path()
    {
        var items = new InMemoryItemRepo();
        var comments = new InMemoryCommentRepo();
        var info = new FakeInfo { UserId = 7, Roles = new[] { "ContentModerator" } };
        var item = TestData.Item(publishAt: DateTimeOffset.UtcNow.AddHours(-1));
        items.Store.Add(item);
        var comment = TestData.Comment(item.Id, ContentCommentStatus.Pending);
        comments.Store.Add(comment);

        var mod = new ModerateContentCommentCommandHandler(comments, items, info);
        var del = new DeleteContentCommentCommandHandler(comments, items);

        item.CommentCount.Should().Be(0);                                         // start (Pending)
        await mod.Handle(New(comment.Id, ContentCommentStatus.Approved), default);
        item.CommentCount.Should().Be(1);                                         // Pending→Approved +1
        await mod.Handle(New(comment.Id, ContentCommentStatus.Hidden), default);
        item.CommentCount.Should().Be(0);                                         // Approved→Hidden -1
        await mod.Handle(New(comment.Id, ContentCommentStatus.Approved), default);
        item.CommentCount.Should().Be(1);                                         // Hidden→Approved +1
        await mod.Handle(New(comment.Id, ContentCommentStatus.Approved), default);
        item.CommentCount.Should().Be(1);                                         // Approved→Approved 0 (no-op)
        await del.Handle(new DeleteContentCommentCommand { CommentId = comment.Id }, default);
        item.CommentCount.Should().Be(0);                                         // Delete (was Approved) -1

        static ModerateContentCommentCommand New(string id, ContentCommentStatus s)
            => new() { CommentId = id, Status = s, Reason = "r" };
    }

    [Fact]
    public async Task Moderation_writes_the_trail()
    {
        var items = new InMemoryItemRepo();
        var comments = new InMemoryCommentRepo();
        var info = new FakeInfo { UserId = 77, Roles = new[] { "ContentModerator" } };
        var item = TestData.Item(publishAt: DateTimeOffset.UtcNow.AddHours(-1));
        items.Store.Add(item);
        var comment = TestData.Comment(item.Id, ContentCommentStatus.Pending);
        comments.Store.Add(comment);

        var dto = await new ModerateContentCommentCommandHandler(comments, items, info)
            .Handle(new ModerateContentCommentCommand { CommentId = comment.Id, Status = ContentCommentStatus.Rejected, Reason = "spam" }, default);

        dto!.Status.Should().Be(ContentCommentStatus.Rejected);
        dto.ModeratedByUserId.Should().Be(77);
        dto.LastModerationReason.Should().Be("spam");
        dto.ModeratedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Pending_is_rejected_as_a_moderation_target()
    {
        var items = new InMemoryItemRepo();
        var comments = new InMemoryCommentRepo();
        var comment = TestData.Comment("cid", ContentCommentStatus.Approved);
        comments.Store.Add(comment);

        var mod = new ModerateContentCommentCommandHandler(comments, items, new FakeInfo { UserId = 7 });

        await FluentActions.Awaiting(() => mod.Handle(
                new ModerateContentCommentCommand { CommentId = comment.Id, Status = ContentCommentStatus.Pending }, default))
            .Should().ThrowAsync<AizenBusinessException>().WithMessage("*Pending*");
    }
}
