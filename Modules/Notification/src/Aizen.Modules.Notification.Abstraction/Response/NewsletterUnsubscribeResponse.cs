namespace Aizen.Modules.Notification.Abstraction.Response;

/// <summary>P1 — result of a one-click unsubscribe. Idempotent: always <c>Unsubscribed=true</c> (even for an unknown
/// token) so unsubscribing twice succeeds and a caller learns nothing.</summary>
public sealed class NewsletterUnsubscribeResponse
{
    public bool Unsubscribed { get; set; }
}
