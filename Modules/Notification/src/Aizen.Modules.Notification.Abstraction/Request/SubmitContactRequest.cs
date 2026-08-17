namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>
/// M4 — the untrusted contact-form payload the website forwards. Shape-validated server-side; NEVER trusted.
/// <see cref="Honeypot"/> is a hidden field a real user leaves empty; <see cref="CaptchaToken"/> is accepted and
/// ignored until a provider is wired (see the FUTURE seam in the handler).
/// </summary>
public sealed class SubmitContactRequest
{
    public string  Name         { get; set; } = default!;
    public string  Email        { get; set; } = default!;
    public string  Subject      { get; set; } = default!;
    public string  Message      { get; set; } = default!;
    public string? SourcePage   { get; set; }
    public string? CaptchaToken { get; set; }
    public string? Honeypot     { get; set; }
}
