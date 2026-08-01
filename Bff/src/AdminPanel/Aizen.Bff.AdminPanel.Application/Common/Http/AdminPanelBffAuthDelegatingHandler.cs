using Aizen.Bff.AdminPanel.Application.Common.Options;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.AdminPanel.Application.Common.Http;

/// <summary>
/// Outgoing auth handler for the AdminPanel BFF → internal module calls. A byte-for-byte mirror of the MarineProvider
/// BFF's <c>MarineProviderBffAuthDelegatingHandler</c> (minus the provider profile id — an admin is not a provider):
///
///   • Authorization: Bearer {keycloak_service_token} — machine-to-machine client_credentials token (per-module
///       audiences baked into the admin-panel-bff client), when no Authorization is already present.
///   • X-Aizen-Bff-Assertion + X-Aizen-User-Id — a trusted-BFF identity assertion, sent ONLY when a module-assertion
///       secret is configured AND the admin identity has been resolved to a numeric user id this request. Audit only:
///       admin module endpoints authorize on the service token's Admin role, not on this header.
///
/// It never forwards an Identity X-Aizen-User-Token and never fabricates an Identity JWT — the human Keycloak token
/// stays inbound-only.
///
/// Register as Transient; resolves scoped services via IHttpContextAccessor.RequestServices.
/// </summary>
[DocumentationInfo("Admin panel BFF auth delegating handler",
    "Injects Authorization (Keycloak service token) and, when configured, the trusted-BFF identity assertion " +
    "(X-Aizen-Bff-Assertion + X-Aizen-User-Id) into all outgoing downstream HTTP calls. Never forwards the user JWT.")]
public sealed class AdminPanelBffAuthDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;

        if (context is not null && !request.Headers.Contains("Authorization"))
        {
            var tokenProvider = context.RequestServices
                .GetRequiredService<IAdminPanelBffKeycloakServiceTokenProvider>();

            var serviceToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {serviceToken}");
        }

        if (context is not null && !request.Headers.Contains(AizenAuthHeaders.BffAssertion))
        {
            var secret = context.RequestServices
                .GetRequiredService<IOptions<AdminPanelKeycloakOptions>>().Value.ModuleAssertionSecret;
            var resolver = context.RequestServices.GetRequiredService<IAdminIdentityResolver>();
            var holder = context.RequestServices.GetRequiredService<IAdminIdentityHolder>();

            await resolver.ResolveAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(secret) && holder.Resolved && holder.UserId is > 0)
            {
                request.Headers.TryAddWithoutValidation(AizenAuthHeaders.BffAssertion, secret);
                request.Headers.TryAddWithoutValidation(AizenAuthHeaders.AssertedUserId, holder.UserId!.Value.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
