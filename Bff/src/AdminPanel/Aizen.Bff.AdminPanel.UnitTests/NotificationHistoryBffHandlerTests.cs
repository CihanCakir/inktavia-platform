using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Notifications.Query;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// Faz 28.5 — Admin gönderim geçmişi BFF handler'ları: Notification modülüne ince proxy (result?.Body döner; paged
/// gövde null gelirse sözleşmeyi koruyan boş sayfaya düşer). Refit arayüzü NSubstitute ile taklit edilir.
/// </summary>
public sealed class NotificationHistoryBffHandlerTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);

    private static INotificationRemoteCall Remote() => Substitute.For<INotificationRemoteCall>();

    // ── Paged ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task History_paged_maps_module_body()
    {
        var remote = Remote();
        remote.GetNotificationHistoryAsync(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<NotificationChannel?>(), Arg.Any<NotificationStatus?>(), Arg.Any<string?>(),
                Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new NotificationHistoryListResult
            {
                Items = new List<NotificationHistoryListItemDto>
                {
                    new() { Id = 5, RecipientUserId = 42, Channel = NotificationChannel.Email, TemplateCode = "SR_CREATED_INAPP", Title = "Hi", Locale = "tr", Status = NotificationStatus.Sent },
                },
                TotalCount = 17, Page = 2, PageSize = 10,
            }));

        var handler = new GetNotificationHistoryBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationHistoryBffQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(17);
        result.Page.Should().Be(2);
        result.Items[0].Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public async Task History_paged_null_body_falls_back_to_empty_page_preserving_paging()
    {
        var remote = Remote();
        remote.GetNotificationHistoryAsync(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<NotificationChannel?>(), Arg.Any<NotificationStatus?>(), Arg.Any<string?>(),
                Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((AizenApiResponse<NotificationHistoryListResult>)null!);

        var handler = new GetNotificationHistoryBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationHistoryBffQuery { Page = 4, PageSize = 25 }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(4);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task History_paged_forwards_filters_to_remote()
    {
        var remote = Remote();
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        remote.GetNotificationHistoryAsync(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<NotificationChannel?>(), Arg.Any<NotificationStatus?>(), Arg.Any<string?>(),
                Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new NotificationHistoryListResult()));

        var handler = new GetNotificationHistoryBffQueryHandler(remote);
        await handler.Handle(new GetNotificationHistoryBffQuery
        {
            From = from, Channel = NotificationChannel.Push, Status = NotificationStatus.Failed,
            TemplateCode = "PAYMENT_CAPTURED_INAPP", RecipientUserId = 99, Page = 1, PageSize = 20,
        }, CancellationToken.None);

        await remote.Received(1).GetNotificationHistoryAsync(from, null,
            NotificationChannel.Push, NotificationStatus.Failed, "PAYMENT_CAPTURED_INAPP", 99, null, 1, 20, Arg.Any<CancellationToken>());
    }

    // ── Detail ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task History_detail_returns_module_body()
    {
        var remote = Remote();
        remote.GetNotificationHistoryByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(Ok(new NotificationHistoryDetailDto
            {
                Id = 5, RecipientUserId = 42, Channel = NotificationChannel.Email, TemplateCode = "SR_CREATED_INAPP",
                Title = "Hi", Locale = "tr", Status = NotificationStatus.Sent, Body = "b", MetadataJson = "{}",
            }));

        var handler = new GetNotificationHistoryDetailBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationHistoryDetailBffQuery(5), CancellationToken.None);

        result!.Id.Should().Be(5);
        result.Body.Should().Be("b");
        result.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public async Task History_detail_null_body_returns_null()
    {
        var remote = Remote();
        remote.GetNotificationHistoryByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns((AizenApiResponse<NotificationHistoryDetailDto>)null!);

        var handler = new GetNotificationHistoryDetailBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationHistoryDetailBffQuery(999), CancellationToken.None);

        result.Should().BeNull();
    }
}
