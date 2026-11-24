using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Content.Command;
using Aizen.Bff.AdminPanel.Application.Content.Query;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Model;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Bff.AdminPanel.UnitTests.Content;

/// <summary>
/// Requirements #4 (list query forwards every filter to the module call — none silently dropped) and
/// #6 (handlers bind the AizenApiResponse&lt;T&gt; envelope — they read .Body and fall back safely on null).
/// Plus a couple of mapping guards (enum → string reshape).
/// </summary>
public sealed class ContentBffHandlerTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);
    private static IContentRemoteCall Remote() => Substitute.For<IContentRemoteCall>();

    // ── #4: filter mapping (no silent drop) ─────────────────────────────────────────────────────
    [Fact]
    public async Task List_forwards_every_filter_to_the_module_call()
    {
        var remote = Remote();
        remote.ListItems(Arg.Any<ContentType?>(), Arg.Any<ContentStatus?>(), Arg.Any<ContentSurface?>(),
                Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Ok(new ContentFeedResponse { Page = 3, PageSize = 7, Total = 0 }));

        var handler = new GetContentItemsBffQueryHandler(remote);
        await handler.Handle(new GetContentItemsBffQuery
        {
            Type = ContentType.Blog,
            Status = ContentStatus.Draft,
            Surface = ContentSurface.MarineOsWeb,
            Tag = "maintenance",
            Lang = "en",
            Page = 3,
            PageSize = 7,
        }, CancellationToken.None);

        // Every filter must reach the remote verbatim — a dropped filter fails this exact-match assertion.
        await remote.Received(1).ListItems(
            ContentType.Blog, ContentStatus.Draft, ContentSurface.MarineOsWeb, "maintenance", "en", 3, 7);
    }

    // ── #6: envelope binding ────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task List_binds_envelope_body_and_maps_rows()
    {
        var remote = Remote();
        remote.ListItems(Arg.Any<ContentType?>(), Arg.Any<ContentStatus?>(), Arg.Any<ContentSurface?>(),
                Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Ok(new ContentFeedResponse
            {
                Items = new List<ContentItemSummaryDto>
                {
                    new() { Id = "c1", Type = ContentType.Blog, Slug = "hello", Status = ContentStatus.Published,
                            Lang = "tr", Title = "Merhaba" },
                },
                Page = 2, PageSize = 5, Total = 42,
            }));

        var handler = new GetContentItemsBffQueryHandler(remote);
        var result = await handler.Handle(new GetContentItemsBffQuery { Page = 2, PageSize = 5 }, CancellationToken.None);

        result.Total.Should().Be(42);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be("c1");
        result.Items[0].Type.Should().Be("Blog");        // enum reshaped to string
        result.Items[0].Status.Should().Be("Published");
        result.Items[0].Title.Should().Be("Merhaba");
    }

    [Fact]
    public async Task List_null_envelope_falls_back_to_empty_result()
    {
        var remote = Remote();
        remote.ListItems(Arg.Any<ContentType?>(), Arg.Any<ContentStatus?>(), Arg.Any<ContentSurface?>(),
                Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((AizenApiResponse<ContentFeedResponse>)null!);

        var handler = new GetContentItemsBffQueryHandler(remote);
        var result = await handler.Handle(new GetContentItemsBffQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetItem_binds_envelope_body_and_maps_detail()
    {
        var remote = Remote();
        remote.GetItem("c1").Returns(Ok(new ContentItemDto
        {
            Id = "c1", Type = ContentType.Announcement, Slug = "x", Status = ContentStatus.Scheduled,
            DefaultLanguage = "tr",
            Translations = new() { new ContentTranslationDto { Lang = "tr", Title = "Duyuru" } },
            Media = new() { new ContentMediaDto { FileStorageId = "f1", Kind = ContentMediaKind.Cover, Position = 0 } },
        }));

        var handler = new GetContentItemBffQueryHandler(remote);
        var result = await handler.Handle(new GetContentItemBffQuery { Id = "c1" }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("c1");
        result.Type.Should().Be("Announcement");
        result.Status.Should().Be("Scheduled");
        result.Translations.Should().ContainSingle(t => t.Title == "Duyuru");
        result.Media.Should().ContainSingle(m => m.Kind == "Cover");
    }

    [Fact]
    public async Task GetItem_null_envelope_maps_to_null()
    {
        var remote = Remote();
        remote.GetItem("missing").Returns((AizenApiResponse<ContentItemDto>)null!);

        var handler = new GetContentItemBffQueryHandler(remote);
        var result = await handler.Handle(new GetContentItemBffQuery { Id = "missing" }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Create_forwards_typed_body_and_returns_created_item()
    {
        var remote = Remote();
        remote.CreateItem(Arg.Any<CreateContentItemRequest>())
            .Returns(Ok(new ContentItemDto { Id = "new1", Type = ContentType.Blog, Slug = "s", Status = ContentStatus.Draft }));

        var handler = new CreateContentItemBffCommandHandler(remote);
        var body = new CreateContentItemRequest { Type = ContentType.Blog, DefaultLanguage = "tr" };
        var result = await handler.Handle(new CreateContentItemBffCommand { Body = body }, CancellationToken.None);

        result!.Id.Should().Be("new1");
        result.Status.Should().Be("Draft");
        await remote.Received(1).CreateItem(Arg.Is<CreateContentItemRequest>(r => ReferenceEquals(r, body)));
    }

    [Fact]
    public async Task Moderate_forwards_body_and_maps_comment()
    {
        var remote = Remote();
        remote.ModerateComment("cm1", Arg.Any<ModerateContentCommentRequest>())
            .Returns(Ok(new ContentCommentDto
            {
                Id = "cm1", ContentId = "c1", Body = "hi", Status = ContentCommentStatus.Approved,
            }));

        var handler = new ModerateContentCommentBffCommandHandler(remote);
        var result = await handler.Handle(new ModerateContentCommentBffCommand
        {
            CommentId = "cm1",
            Body = new ModerateContentCommentRequest { Status = ContentCommentStatus.Approved, Reason = "ok" },
        }, CancellationToken.None);

        result!.Status.Should().Be("Approved");     // enum reshaped to string
        await remote.Received(1).ModerateComment("cm1",
            Arg.Is<ModerateContentCommentRequest>(r => r.Status == ContentCommentStatus.Approved && r.Reason == "ok"));
    }
}
