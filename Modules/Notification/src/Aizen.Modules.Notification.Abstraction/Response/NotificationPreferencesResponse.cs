namespace Aizen.Modules.Notification.Abstraction.Response;

/// <summary>
/// N-B preferences matrix for the current user — one row per user-toggleable category, each with the
/// per-channel effective state. Category/channel names are the enum names (stable string contract for the FE).
/// The security Account category is excluded (always-deliver); Sms is out of scope (greyed on the FE).
/// </summary>
public sealed class NotificationPreferencesResponse
{
    public List<NotificationCategoryPreferenceDto> Categories { get; set; } = new();
}

public sealed class NotificationCategoryPreferenceDto
{
    /// <summary>The <c>NotificationCategory</c> enum name (e.g. "Messages", "Payments").</summary>
    public string Category { get; set; } = default!;

    public ChannelPreferenceDto InApp { get; set; } = default!;
    public ChannelPreferenceDto Push  { get; set; } = default!;
    public ChannelPreferenceDto Email { get; set; } = default!;
}

public sealed class ChannelPreferenceDto
{
    public bool Enabled { get; set; }
    /// <summary>True = the user cannot change this cell (baseline/security); the FE renders it locked/on.</summary>
    public bool Locked  { get; set; }
}
