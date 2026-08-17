using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// N-B per-user preference: one row per (UserId, Category, Channel) → Enabled. Absent rows resolve to the
/// documented defaults (see NotificationPreferencePolicy); locked cells (InApp, Account/security) are never stored.
/// </summary>
public sealed class NotificationPreferenceEntity : AizenEntity
{
    public long                 UserId    { get; private set; }
    public NotificationCategory Category  { get; private set; }
    public NotificationChannel  Channel   { get; private set; }
    public bool                 Enabled   { get; private set; }
    public DateTimeOffset       UpdatedAt { get; private set; }

    private NotificationPreferenceEntity() { }

    public static NotificationPreferenceEntity Create(
        long userId, NotificationCategory category, NotificationChannel channel, bool enabled)
        => new()
        {
            UserId    = userId,
            Category  = category,
            Channel   = channel,
            Enabled   = enabled,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public void SetEnabled(bool enabled)
    {
        Enabled   = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
