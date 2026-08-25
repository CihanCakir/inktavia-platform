using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationCampaignsPaged;

/// <summary>Sayfalı kampanya listesi, en yeni önce.</summary>
public sealed class GetNotificationCampaignsPagedQuery : AizenQuery<NotificationCampaignListResult>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
