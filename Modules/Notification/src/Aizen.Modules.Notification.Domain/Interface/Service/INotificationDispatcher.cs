using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Service;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationEntity notification, CancellationToken ct = default);
}
