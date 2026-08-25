using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNotificationHistoryDetailBffQuery : AizenQuery<NotificationHistoryDetailDto>
{
    public long Id { get; }

    public GetNotificationHistoryDetailBffQuery(long id) => Id = id;
}
