namespace Aizen.Modules.Notification.Application.Command.SubmitContactMessage;

/// <summary>
/// M4 — server-side contact-intake spam thresholds (config-driven, not compiled-in). Bound from <c>ContactIntake</c>.
/// <see cref="IpHashSalt"/> salts the IP hash (never stored raw) and MUST be overridden per environment via secret.
/// </summary>
public sealed class ContactIntakeOptions
{
    public const string SectionName = "ContactIntake";

    /// <summary>Score at/above which the ticket is persisted but admins are NOT notified (silent accept).</summary>
    public int SpamThreshold { get; set; } = 50;

    /// <summary>Sliding window (minutes) for the per-IP submission-rate heuristic.</summary>
    public int RateWindowMinutes { get; set; } = 10;

    /// <summary>Submissions per IP within the window above which a rate penalty is added.</summary>
    public int RateMaxPerWindow { get; set; } = 3;

    /// <summary>Link count in subject+message above which a strong penalty is added.</summary>
    public int MaxLinks { get; set; } = 2;

    /// <summary>Salt for the IP hash. Server-side only; override per environment.</summary>
    public string IpHashSalt { get; set; } = "__FROM_SECRET__";
}
