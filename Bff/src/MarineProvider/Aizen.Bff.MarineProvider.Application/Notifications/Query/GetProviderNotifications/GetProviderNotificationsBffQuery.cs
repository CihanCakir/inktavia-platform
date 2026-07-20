using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class GetProviderNotificationsBffQuery : AizenQuery<NotificationListResponse>
{
    public int Skip { get; init; }
    public int Take { get; init; } = 20;
}
