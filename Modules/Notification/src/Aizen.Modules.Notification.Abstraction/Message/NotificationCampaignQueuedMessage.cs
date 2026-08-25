using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Notification.Abstraction.Message;

/// <summary>Faz 28.6 — bir admin kampanyası kuyruğa alındı; CampaignDispatchConsumer bu id ile dağıtımı yapar.</summary>
public sealed class NotificationCampaignQueuedMessage : AizenBaseMessage
{
    public long CampaignId { get; set; }
}
