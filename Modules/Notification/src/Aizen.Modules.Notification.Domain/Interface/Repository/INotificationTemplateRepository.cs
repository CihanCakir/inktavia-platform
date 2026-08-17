using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct = default);
    Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(NotificationType type, NotificationChannel channel, CancellationToken ct = default);
    Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct = default);
}
