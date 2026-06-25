using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationRepository
{
    Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task AddAsync(NotificationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default);
    Task BulkMarkAsReadAsync(long userId, CancellationToken ct = default);
}
