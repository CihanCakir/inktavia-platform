namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>
/// P1 — the untrusted newsletter sign-up payload the website forwards. Shape-validated server-side; NEVER trusted.
/// The caller IP is forwarded out-of-band (X-Forwarded-For) and hashed server-side — never carried in the body.
/// </summary>
public sealed class SubscribeNewsletterRequest
{
    public string  Email      { get; set; } = default!;
    /// <summary>Locale the reader subscribed in (<c>tr</c>/<c>en</c>); used to pick the confirmation-mail language.</summary>
    public string  Lang       { get; set; } = default!;
    /// <summary>Path only (no query string) of the page the sign-up came from. Optional.</summary>
    public string? SourcePage { get; set; }
}
