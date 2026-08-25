using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNotificationCampaignsPagedBffQuery : AizenQuery<NotificationCampaignListResult>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
