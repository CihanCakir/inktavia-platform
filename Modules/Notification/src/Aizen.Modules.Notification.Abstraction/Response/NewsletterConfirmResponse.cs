namespace Aizen.Modules.Notification.Abstraction.Response;

/// <summary>P1 — result of redeeming a confirmation token. <c>Confirmed=false</c> for an unknown/expired/used token.</summary>
public sealed class NewsletterConfirmResponse
{
    public bool Confirmed { get; set; }
}
