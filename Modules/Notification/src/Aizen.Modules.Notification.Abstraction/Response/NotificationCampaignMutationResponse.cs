using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Response;

/// <summary>Kampanya oluşturma sonucu: yeni kampanya id'si + kuyruğa alınan durum.</summary>
public sealed class NotificationCampaignMutationResponse
{
    public long           CampaignId { get; set; }
    public CampaignStatus Status     { get; set; }
}
