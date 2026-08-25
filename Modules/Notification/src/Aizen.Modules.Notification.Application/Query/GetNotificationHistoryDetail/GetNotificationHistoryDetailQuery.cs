using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationHistoryDetail;

/// <summary>Tek bildirimin tam satırı (admin gönderim geçmişi detayı). Bulunamazsa null döner.</summary>
public sealed class GetNotificationHistoryDetailQuery : AizenQuery<NotificationHistoryDetailDto>
{
    public long Id { get; init; }
}
