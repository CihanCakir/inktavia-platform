using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.SubscribeWebNewsletter;

/// <summary>
/// POST /api/v1/web/newsletter/subscribe — forwards an untrusted newsletter sign-up to the Notification module. Shape-
/// validated at the BFF; the module owns idempotency, spam scoring, persistence, and the double opt-in mail.
/// </summary>
public sealed class SubscribeWebNewsletterCommand : AizenCommand<NewsletterSubscribeResponse>
{
    public string  Email      { get; init; } = default!;
    public string  Lang       { get; init; } = default!;
    public string? SourcePage { get; init; }

    /// <summary>Caller IP the controller resolved (X-Forwarded-For / remote IP); forwarded to the module for hashing.</summary>
    public string? ClientIp   { get; init; }
}
