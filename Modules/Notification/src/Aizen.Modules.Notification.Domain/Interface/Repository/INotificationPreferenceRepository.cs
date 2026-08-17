using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationPreferenceRepository
{
    /// <summary>All stored preference rows for a user (absent cells resolve to defaults at the policy layer).</summary>
    Task<List<NotificationPreferenceEntity>> GetByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>Upsert one (user, category, channel) → enabled row.</summary>
    Task UpsertAsync(long userId, NotificationCategory category, NotificationChannel channel, bool enabled, CancellationToken ct = default);
}
