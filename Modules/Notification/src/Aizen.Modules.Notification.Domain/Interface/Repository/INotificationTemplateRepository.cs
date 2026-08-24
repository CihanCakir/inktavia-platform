using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct = default);
    Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(NotificationType type, NotificationChannel channel, CancellationToken ct = default);
    Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Admin sayfalı liste: en yeni önce değil, Type→Channel sırası. Filtreler: channel (template.Channel),
    /// enabled (template.IsActive), search (kod/ad), locale &amp; status ise içerik satırı EXISTS ile eşleşir.
    /// </summary>
    Task<(List<NotificationTemplateEntity> Items, int TotalCount)> GetPagedAsync(
        NotificationChannel? channel,
        string? locale,
        TemplateContentStatus? status,
        bool? enabled,
        string? search,
        int skip,
        int take,
        CancellationToken ct = default);

    Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct = default);
}
