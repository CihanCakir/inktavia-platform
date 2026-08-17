using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction;

/// <summary>
/// N-B preference defaults + lock rules — the single source of truth for resolving an effective
/// (category, channel) → enabled from an optional stored row. Used by preference resolution (GetPreferences)
/// and by both dispatch gates (push consumer + email send).
/// </summary>
public static class NotificationPreferencePolicy
{
    /// <summary>
    /// A locked cell cannot be changed by the user and always delivers:
    /// InApp (baseline inbox) for every category, and the security Account category on all channels.
    /// </summary>
    public static bool IsLocked(NotificationCategory category, NotificationChannel channel)
        => channel == NotificationChannel.InApp || NotificationCategoryMap.IsAlwaysDeliver(category);

    /// <summary>Default enabled state when no stored preference row exists.</summary>
    public static bool DefaultEnabled(NotificationCategory category, NotificationChannel channel)
    {
        if (channel == NotificationChannel.InApp) return true;                 // baseline inbox, always on
        if (NotificationCategoryMap.IsAlwaysDeliver(category)) return true;    // Account/security, always on
        return channel switch
        {
            NotificationChannel.Push  => true,   // N-A behaviour: on for subscribed users until muted
            NotificationChannel.Email => false,  // opt-in
            _                         => false,  // Sms (out of scope)
        };
    }

    /// <summary>Effective enabled state: locked cells always deliver; otherwise the stored value or the default.</summary>
    public static bool Resolve(NotificationCategory category, NotificationChannel channel, bool? stored)
    {
        if (IsLocked(category, channel)) return true;
        return stored ?? DefaultEnabled(category, channel);
    }
}
