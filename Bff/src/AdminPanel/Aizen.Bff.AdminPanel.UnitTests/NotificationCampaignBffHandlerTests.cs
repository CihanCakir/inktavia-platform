using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Notifications.Command;
using Aizen.Bff.AdminPanel.Application.Notifications.Query;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>Faz 28.6 — admin kampanya BFF handler'ları: Notification modülüne ince proxy (result?.Body; paged null→boş sayfa).</summary>
public sealed class NotificationCampaignBffHandlerTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);

    private static INotificationRemoteCall Remote() => Substitute.For<INotificationRemoteCall>();

    [Fact]
    public async Task Create_forwards_body_and_returns_response()
    {
        var remote = Remote();
        remote.CreateNotificationCampaignAsync(Arg.Any<CreateNotificationCampaignRequest>())
            .Returns(Ok(new NotificationCampaignMutationResponse { CampaignId = 7, Status = CampaignStatus.Queued }));

        var handler = new CreateNotificationCampaignBffCommandHandler(remote);
        var result = await handler.Handle(new CreateNotificationCampaignBffCommand
        {
            Request = new CreateNotificationCampaignRequest
            {
                Audience = CampaignAudience.Provider, TargetMode = CampaignTargetMode.All, TemplateCode = "TPL",
                Channels = new List<NotificationChannel> { NotificationChannel.InApp },
            },
        }, CancellationToken.None);

        result!.CampaignId.Should().Be(7);
        result.Status.Should().Be(CampaignStatus.Queued);
    }

    [Fact]
    public async Task GetById_returns_module_body()
    {
        var remote = Remote();
        remote.GetNotificationCampaignByIdAsync(7)
            .Returns(Ok(new NotificationCampaignDto { Id = 7, Status = CampaignStatus.Completed, SentCount = 3, FailedCount = 1 }));

        var handler = new GetNotificationCampaignBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationCampaignBffQuery(7), CancellationToken.None);

        result!.Id.Should().Be(7);
        result.SentCount.Should().Be(3);
        result.Status.Should().Be(CampaignStatus.Completed);
    }

    [Fact]
    public async Task Paged_maps_body()
    {
        var remote = Remote();
        remote.GetNotificationCampaignsPagedAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Ok(new NotificationCampaignListResult
            {
                Items = new List<NotificationCampaignListItemDto> { new() { Id = 1, Status = CampaignStatus.Processing } },
                TotalCount = 5, Page = 1, PageSize = 20,
            }));

        var handler = new GetNotificationCampaignsPagedBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationCampaignsPagedBffQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Paged_null_body_falls_back_to_empty_page()
    {
        var remote = Remote();
        remote.GetNotificationCampaignsPagedAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns((AizenApiResponse<NotificationCampaignListResult>)null!);

        var handler = new GetNotificationCampaignsPagedBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationCampaignsPagedBffQuery { Page = 2, PageSize = 15 }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(15);
    }
}
