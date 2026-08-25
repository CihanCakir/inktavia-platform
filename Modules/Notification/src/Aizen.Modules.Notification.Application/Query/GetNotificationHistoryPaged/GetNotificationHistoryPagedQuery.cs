using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationHistoryPaged;

/// <summary>Admin gönderim geçmişi sayfalı sorgusu. Tüm filtreler opsiyonel ve birleştirilebilir; en yeni önce.</summary>
public sealed class GetNotificationHistoryPagedQuery : AizenQuery<NotificationHistoryListResult>
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
