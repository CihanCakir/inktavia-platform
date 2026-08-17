using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Common.Http;

/// <summary>
/// Outgoing auth handler for MarineProvider BFF → internal module calls.
///
/// Injects the Keycloak service-account token (client_credentials) in the Authorization header, and — when a
/// module-assertion secret is configured and the provider identity has already been resolved this request —
/// a trusted-BFF identity assertion (X-Aizen-Bff-Assertion + X-Aizen-User-Id + X-Aizen-Provider-Profile-Id) so
/// existing module handlers that read UserInfo.UserId work for provider calls.
///
/// It never forwards an Identity X-Aizen-User-Token and never fabricates an Identity JWT. Because the identity
/// holder is only populated AFTER the resolver's own Identity calls complete, those calls carry no assertion
/// headers → no recursion.
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

        if (context is not null && !request.Headers.Contains(AizenAuthHeaders.BffAssertion))
        {
            var secret = context.RequestServices
                .GetRequiredService<IOptions<MarineProviderKeycloakOptions>>().Value.ModuleAssertionSecret;
            var holder = context.RequestServices.GetRequiredService<IProviderIdentityHolder>();

            if (!string.IsNullOrWhiteSpace(secret) && holder.Resolved && holder.UserId is > 0)
            {
                request.Headers.TryAddWithoutValidation(AizenAuthHeaders.BffAssertion, secret);
                request.Headers.TryAddWithoutValidation(AizenAuthHeaders.AssertedUserId, holder.UserId!.Value.ToString());
                if (holder.ProfileId is > 0)
                    request.Headers.TryAddWithoutValidation(AizenAuthHeaders.AssertedProfileId, holder.ProfileId!.Value.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
