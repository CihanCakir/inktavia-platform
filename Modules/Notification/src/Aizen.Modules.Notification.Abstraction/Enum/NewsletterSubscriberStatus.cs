namespace Aizen.Modules.Notification.Abstraction.Enum;

/// <summary>
/// P1 — lifecycle of a website newsletter subscriber. Double opt-in: a subscriber is only a real subscriber once
/// <see cref="Confirmed"/> (the emailed token was redeemed). A <see cref="Pending"/> row is NOT a subscriber.
/// </summary>
public enum NewsletterSubscriberStatus
{
    /// <summary>Signed up, confirmation mail sent, token not yet redeemed. NOT a subscriber.</summary>
    Pending      = 1,
    /// <summary>Double opt-in complete — the confirmation token was redeemed. The only "subscribed" state.</summary>
    Confirmed    = 2,
    /// <summary>Opted out via the one-click unsubscribe link. Can resubscribe.</summary>
    Unsubscribed = 3,
    /// <summary>The confirmation/welcome mail hard-bounced. Can resubscribe (fresh confirm).</summary>
    Bounced      = 4,
    /// <summary>Manually suppressed by an admin (abuse/complaint). Sticky — never auto-resurrected by resubscribe.</summary>
    Suppressed   = 5,
}
