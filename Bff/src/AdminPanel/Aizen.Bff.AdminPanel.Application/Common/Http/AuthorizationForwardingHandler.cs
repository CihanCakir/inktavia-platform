using Microsoft.AspNetCore.Http;

namespace Aizen.Bff.AdminPanel.Application.Common.Http;

[DocumentationInfo("Authorization forwarding handler", "Propagates the Authorization header from the incoming BFF HTTP request to outgoing downstream microservice calls. Skipped if no Authorization header is present in the inbound context or if the outgoing request already carries one.")]
public sealed class AuthorizationForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incoming = httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(incoming)
            && !request.Headers.Contains("Authorization"))
        {
            request.Headers.TryAddWithoutValidation("Authorization", incoming);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
