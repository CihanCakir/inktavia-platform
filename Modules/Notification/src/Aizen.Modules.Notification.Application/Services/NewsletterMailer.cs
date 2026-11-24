using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Interface.Service;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// P1 — renders the newsletter templates (NEWSLETTER_CONFIRM_EMAIL / NEWSLETTER_WELCOME_EMAIL) via the module's single
/// strict <see cref="ITemplateRenderer"/> and hands them to the existing <see cref="IEmailSender"/> transport.
/// </summary>
public sealed class NewsletterMailer : INewsletterMailer
{
    public const string ConfirmTemplateCode = "NEWSLETTER_CONFIRM_EMAIL";
    public const string WelcomeTemplateCode = "NEWSLETTER_WELCOME_EMAIL";

    private readonly ITemplateRenderer _renderer;
    private readonly IEmailSender _emailSender;

    public NewsletterMailer(ITemplateRenderer renderer, IEmailSender emailSender)
    {
        _renderer = renderer;
        _emailSender = emailSender;
    }

    public async Task SendConfirmationAsync(
        string email, string lang, string confirmUrl, string unsubscribeUrl, CancellationToken ct)
    {
        var rendered = await _renderer.RenderAsync(
            ConfirmTemplateCode, NotificationChannel.Email, lang,
            new Dictionary<string, string>
            {
                ["confirmUrl"]     = confirmUrl,
                ["unsubscribeUrl"] = unsubscribeUrl,
            }, ct);

        await _emailSender.SendAsync(email, rendered.Title, rendered.Body, ct);
    }

    public async Task SendWelcomeAsync(string email, string lang, string unsubscribeUrl, CancellationToken ct)
    {
        var rendered = await _renderer.RenderAsync(
            WelcomeTemplateCode, NotificationChannel.Email, lang,
            new Dictionary<string, string>
            {
                ["unsubscribeUrl"] = unsubscribeUrl,
            }, ct);

        await _emailSender.SendAsync(email, rendered.Title, rendered.Body, ct);
    }
}
