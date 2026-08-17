namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;

// BE_MO9c — owner notification surface (inbox + read + preferences + FCM device-token). All passthroughs over the
// existing module endpoints, which scope to the caller's participant from the token. COST-FREE: the inbox carries the
// human message (Title/Body) + the deep-link ref only — enums cross as string NAMES (the AdminPanel numeric-enum
// gotcha) and the internal MetadataJson (transaction/context ids) is DROPPED.

/// <summary>One inbox item (cost-free). MetadataJson + Channel + Status are dropped; Type crosses as its name.</summary>
public sealed class MobileNotificationDto
{
    public long Id { get; set; }
    /// <summary>NotificationType name (e.g. "OfferAccepted", "PaymentCaptured", "MaintenanceReminderDue").</summary>
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    /// <summary>Deep-link ref (e.g. "ServiceRequest" / "Payment" / a conversation) — no economics.</summary>
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

/// <summary>The caller's inbox page + the unread badge count.</summary>
public sealed class MobileNotificationListDto
{
    public List<MobileNotificationDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int UnreadCount { get; set; }
}

/// <summary>One channel cell of the preference matrix.</summary>
public sealed class MobileNotificationPreferenceCellDto
{
    public bool Enabled { get; set; }
    /// <summary>True = the user cannot change this (baseline/security — e.g. InApp, or the Account category).</summary>
    public bool Locked { get; set; }
}

/// <summary>One category row of the preference matrix.</summary>
public sealed class MobileNotificationCategoryPreferenceDto
{
    /// <summary>NotificationCategory name (Messages/ServiceRequests/Disputes/Payments/CargoDry/Broadcast).</summary>
    public string Category { get; set; } = default!;
    public MobileNotificationPreferenceCellDto InApp { get; set; } = new();
    public MobileNotificationPreferenceCellDto Push { get; set; } = new();
    public MobileNotificationPreferenceCellDto Email { get; set; } = new();
}

/// <summary>The caller's per-category × channel preference matrix (already cost-free — categories + toggles only).</summary>
public sealed class MobileNotificationPreferencesDto
{
    public List<MobileNotificationCategoryPreferenceDto> Categories { get; set; } = new();
}

/// <summary>Register-device-token payload — only the FCM token; the Platform is forced to Fcm server-side.</summary>
public sealed class MobileRegisterDeviceTokenRequest
{
    public string? DeviceToken { get; set; }
}

/// <summary>Toggle one preference cell (only Push/Email are user-changeable; the module rejects locked cells).</summary>
public sealed class MobileUpdatePreferenceRequest
{
    public string? Category { get; set; }
    public string? Channel { get; set; }
    public bool Enabled { get; set; }
}

/// <summary>Result of marking one notification read.</summary>
public sealed class MobileMarkReadResultDto
{
    public long NotificationId { get; set; }
    public bool Updated { get; set; }
}

/// <summary>Result of marking all read.</summary>
public sealed class MobileMarkAllReadResultDto
{
    public int UpdatedCount { get; set; }
}

/// <summary>Result of registering an FCM device token.</summary>
public sealed class MobileDeviceTokenResultDto
{
    public bool Registered { get; set; }
}
