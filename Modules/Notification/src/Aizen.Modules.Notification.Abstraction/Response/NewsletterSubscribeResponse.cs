namespace Aizen.Modules.Notification.Abstraction.Response;

/// <summary>
/// P1 — the ONLY thing an anonymous sign-up caller sees. Always <c>Accepted=true</c> for a well-formed request,
/// regardless of whether the address was new, already pending, already confirmed, or resubscribed — a caller can
/// never enumerate who is subscribed (see the handler). Carries no id, status, or token.
/// </summary>
public sealed class NewsletterSubscribeResponse
{
    public bool Accepted { get; set; }
}
