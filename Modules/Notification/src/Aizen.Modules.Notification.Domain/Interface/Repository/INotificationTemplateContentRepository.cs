using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationTemplateContentRepository
{
    /// <summary>
    /// Bir template kodu + kanal için TÜM Published içerikleri (tüm locale/version) döner. Locale fallback ve
    /// version seçimi renderer'da yapılır. Join: notification_templates.TemplateCode → TemplateId.
    /// </summary>
    Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(
        string templateCode, NotificationChannel channel, CancellationToken ct = default);

    Task AddAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default);
}
