using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNotificationHistoryBffQuery : AizenQuery<NotificationHistoryListResult>
{
    public DateTimeOffset?      From            { get; init; }
    public DateTimeOffset?      To              { get; init; }
    public NotificationChannel? Channel         { get; init; }
    public NotificationStatus?  Status          { get; init; }
    public string?              TemplateCode    { get; init; }
    public long?                RecipientUserId { get; init; }
    public long?                CampaignId      { get; init; }
    public int                  Page            { get; init; } = 1;
    public int                  PageSize        { get; init; } = 20;
}
