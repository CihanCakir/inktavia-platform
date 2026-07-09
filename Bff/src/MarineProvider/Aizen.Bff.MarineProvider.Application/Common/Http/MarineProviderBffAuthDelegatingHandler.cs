using Aizen.Bff.MarineProvider.Application.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Bff.MarineProvider.Application.Common.Http;

/// <summary>
/// Outgoing auth handler for MarineProvider BFF → internal module calls.
///
/// Injects only the Keycloak service-account token (client_credentials) in the Authorization header.
/// It deliberately does NOT forward an Identity X-Aizen-User-Token: Provider Web authenticates via
/// Keycloak and there is no legacy Identity user token in this flow. Provider identity is passed to
/// modules explicitly (as endpoint parameters) on provider-safe endpoints, authorized by the service token.
///
/// Register as Transient; resolves scoped services via IHttpContextAccessor.RequestServices.
/// </summary>
public sealed class MarineProviderBffAuthDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;

        if (context is not null && !request.Headers.Contains("Authorization"))
        {
            var tokenProvider = context.RequestServices
                .GetRequiredService<IProviderKeycloakServiceTokenProvider>();

            var serviceToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {serviceToken}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
