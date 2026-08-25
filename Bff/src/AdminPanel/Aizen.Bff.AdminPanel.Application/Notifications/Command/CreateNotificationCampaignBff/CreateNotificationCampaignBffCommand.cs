using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

public sealed class CreateNotificationCampaignBffCommand : AizenCommand<NotificationCampaignMutationResponse>
{
    public CreateNotificationCampaignRequest Request { get; init; } = default!;
}
