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
}
