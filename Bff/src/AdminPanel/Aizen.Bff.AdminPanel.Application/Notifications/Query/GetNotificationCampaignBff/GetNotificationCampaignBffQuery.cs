using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNotificationCampaignBffQuery : AizenQuery<NotificationCampaignDto>
{
    public long Id { get; }

    public GetNotificationCampaignBffQuery(long id) => Id = id;
}
