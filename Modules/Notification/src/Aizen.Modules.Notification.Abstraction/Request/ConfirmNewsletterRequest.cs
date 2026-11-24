namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>P1 — redeem the opaque, single-use confirmation token emailed at sign-up (double opt-in).</summary>
public sealed class ConfirmNewsletterRequest
{
    public string Token { get; set; } = default!;
}
