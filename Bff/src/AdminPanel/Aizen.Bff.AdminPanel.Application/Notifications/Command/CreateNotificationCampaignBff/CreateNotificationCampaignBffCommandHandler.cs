using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Create notification campaign command handler",
    "Admin doğrudan/toplu bildirim kampanyasını Notification modülüne iletir (Queued + kuyruğa alınır).")]
public sealed class CreateNotificationCampaignBffCommandHandler
    : AizenCommandHandler<CreateNotificationCampaignBffCommand, NotificationCampaignMutationResponse>
{
    private readonly INotificationRemoteCall _notification;

    public CreateNotificationCampaignBffCommandHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationCampaignMutationResponse?> Handle(
        CreateNotificationCampaignBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _notification.CreateNotificationCampaignAsync(request.Request);
        return result?.Body;
    }
}
