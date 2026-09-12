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
            NotificationChannel.Email => EmailDefault(category),
            _                         => false,  // Sms (out of scope)
        };
    }

    /// <summary>
    /// Email default is split TRANSACTIONAL vs COMMERCIAL — do NOT collapse this back to a blanket false.
    /// The rule: a category is on-by-default only when the recipient is already a party to the job the email is
    /// about (ServiceRequests/Payments/Disputes — offer received, payment captured, dispute resolved), so muting it
    /// silently drops mail the user actually needs (the owner-gets-no-email bug). Categories that are commercial or
    /// high-volume (Messages, CargoDry, Broadcast) stay opt-in so we don't spam an address the user never consented to.
    /// Account is never reached here — IsAlwaysDeliver short-circuits above.
    /// </summary>
    private static bool EmailDefault(NotificationCategory category) => category switch
    {
        NotificationCategory.ServiceRequests => true,   // transactional
        NotificationCategory.Payments        => true,   // transactional
        NotificationCategory.Disputes        => true,   // transactional
        NotificationCategory.Messages        => false,  // opt-in (high-volume chat)
        NotificationCategory.CargoDry        => false,  // opt-in (commercial)
        NotificationCategory.Broadcast       => false,  // opt-in (marketing/announcements)
        _                                    => false,  // unknown → opt-in (safe default)
    };

    /// <summary>Effective enabled state: locked cells always deliver; otherwise the stored value or the default.</summary>
    public static bool Resolve(NotificationCategory category, NotificationChannel channel, bool? stored)
    {
        if (IsLocked(category, channel)) return true;
        return stored ?? DefaultEnabled(category, channel);
    }
}
