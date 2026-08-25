using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationCampaign;

/// <summary>Tek kampanya detayı (durum + sayaçlar). Bulunamazsa null.</summary>
public sealed class GetNotificationCampaignQuery : AizenQuery<NotificationCampaignDto>
{
    public long Id { get; init; }
}
