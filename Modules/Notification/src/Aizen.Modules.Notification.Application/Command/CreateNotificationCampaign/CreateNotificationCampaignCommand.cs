using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.CreateNotificationCampaign;

public sealed class CreateNotificationCampaignCommand : AizenCommand<NotificationCampaignMutationResponse>
{
    public CampaignAudience   Audience             { get; set; }
    public CampaignTargetMode TargetMode           { get; set; }
    public List<long>?        SelectedRecipientIds { get; set; }
    public string?            TemplateCode         { get; set; }
    public Dictionary<string, CampaignLocaleContent>? CustomContent { get; set; }
    public List<NotificationChannel> Channels      { get; set; } = new();
    public DateTimeOffset?    ScheduledAt          { get; set; }
}
