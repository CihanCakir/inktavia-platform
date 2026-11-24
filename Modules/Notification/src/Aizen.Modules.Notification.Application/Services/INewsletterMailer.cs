namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// P1 — sends the two newsletter double opt-in mails to an ANONYMOUS address (no platform user id). It reuses the
/// module's existing template renderer + email sender (no new transport); it does NOT go through SendNotificationCommand
/// because there is no RecipientUserId, no preference gate, and no in-app row to file for a website visitor.
/// </summary>
public interface INewsletterMailer
{
    /// <summary>Double opt-in confirmation link. <paramref name="lang"/> selects the mail locale (falls back to en).</summary>
    Task SendConfirmationAsync(string email, string lang, string confirmUrl, string unsubscribeUrl, CancellationToken ct);

    /// <summary>Welcome mail sent once the subscriber confirms.</summary>
    Task SendWelcomeAsync(string email, string lang, string unsubscribeUrl, CancellationToken ct);
}
