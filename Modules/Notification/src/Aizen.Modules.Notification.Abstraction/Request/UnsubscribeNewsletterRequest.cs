namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>P1 — one-click opt-out via the stable, non-expiring unsubscribe token embedded in every mail.</summary>
public sealed class UnsubscribeNewsletterRequest
{
    public string Token { get; set; } = default!;
}
