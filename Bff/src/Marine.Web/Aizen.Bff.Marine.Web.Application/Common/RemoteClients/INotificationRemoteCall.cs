using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients;

/// <summary>
/// BFF → Notification module PUBLIC contact intake (M4). Anonymous on the module (in-cluster); the stricter
/// <c>contact-submit</c> rate limit lives on the BFF. The BFF forwards the caller's IP as <c>X-Forwarded-For</c> so
/// the module can hash it (salted) — the browser never sends its own IP. Enveloped response (<c>.Body</c>).
/// </summary>
public interface INotificationRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/api/v1/notification/public/contact")]
    Task<AizenApiResponse<SubmitContactResponse>> SubmitContact(
        [AizenRemoteCallBody] SubmitContactRequest request,
        [Refit.Header("X-Forwarded-For")] string? forwardedFor);

    // ── P1 — public newsletter double opt-in (anonymous on the module; newsletter-submit rate limit on the BFF). ──
    // Subscribe forwards the caller IP as X-Forwarded-For (the module hashes it, salted). Confirm/unsubscribe carry only
    // an opaque token. Enveloped responses (.Body).
    [AizenRemoteCallPost("/api/v1/notification/public/newsletter/subscribe")]
    Task<AizenApiResponse<NewsletterSubscribeResponse>> SubscribeNewsletter(
        [AizenRemoteCallBody] SubscribeNewsletterRequest request,
        [Refit.Header("X-Forwarded-For")] string? forwardedFor);

    [AizenRemoteCallPost("/api/v1/notification/public/newsletter/confirm")]
    Task<AizenApiResponse<NewsletterConfirmResponse>> ConfirmNewsletter(
        [AizenRemoteCallBody] ConfirmNewsletterRequest request);

    [AizenRemoteCallPost("/api/v1/notification/public/newsletter/unsubscribe")]
    Task<AizenApiResponse<NewsletterUnsubscribeResponse>> UnsubscribeNewsletter(
        [AizenRemoteCallBody] UnsubscribeNewsletterRequest request);
}
