using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction;

/// <summary>
/// Single source of truth for grouping <see cref="NotificationType"/> into a
/// <see cref="NotificationCategory"/> (N-B). Used by preference resolution and the dispatch gate.
/// </summary>
public static class NotificationCategoryMap
{
    /// <summary>Resolve a notification type to its preference category (by the enum's numeric ranges).</summary>
    public static NotificationCategory Resolve(NotificationType type)
    {
        var code = (int)type;
        return code switch
        {
            200 or 201                 => NotificationCategory.Messages,        // NewMessage / MessageBlocked
            >= 100 and <= 139          => NotificationCategory.ServiceRequests, // SR / offer / assignment / completion (incl. 103 maintenance-reminder N2, 133 auto-approve-approaching N3-C)
            140 or 141                 => NotificationCategory.Disputes,
            >= 150 and <= 163          => NotificationCategory.Payments,   // incl. 157 PaymentAuthorized (N-C); 160 price-change, 161/162 boost, 163 budget-low (N1/N4)
            >= 300 and <= 309          => NotificationCategory.CargoDry,
            >= 400 and <= 412          => NotificationCategory.Account,          // profile + auth/OTP + e-posta doğrulama (412) — security, always-deliver
            >= 900 and <= 999          => NotificationCategory.Broadcast,   // incl. 910 SupportRequestOpened (N-D)
            _                          => NotificationCategory.Account,          // unknown → treat as always-deliver
        };
    }

    /// <summary>
    /// Security category — push/email always deliver and are not user-disableable (excluded from the toggle list).
    /// </summary>
    public static bool IsAlwaysDeliver(NotificationCategory category) => category == NotificationCategory.Account;

    /// <summary>Categories exposed as user-toggleable in the preferences matrix (Account/security excluded).</summary>
    public static readonly IReadOnlyList<NotificationCategory> ToggleableCategories = new[]
    {
        NotificationCategory.Messages,
        NotificationCategory.ServiceRequests,
        NotificationCategory.Disputes,
        NotificationCategory.Payments,
        NotificationCategory.CargoDry,
        NotificationCategory.Broadcast,
    };
}
