using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Bff.AdminPanel.Application.Common.Http;

/// <summary>
/// Centralized outgoing auth header handler for all BFF → downstream microservice calls.
///
/// Sets two headers automatically on every outgoing Refit request:
///   • Authorization: Bearer {keycloak_service_token}   — machine-to-machine service account token
///   • X-Aizen-User-Token: {identity_user_access_token} — caller's identity JWT (populated by
///       AizenUserInfoMiddleware from the incoming X-Aizen-User-Token header).
///
/// Headers are skipped if already present on the outgoing request or if the value is empty
/// (e.g. unauthenticated/anonymous calls where UserInfo.AccessToken is not set).
///
/// Register as Transient. Resolves scoped services via IHttpContextAccessor.RequestServices
/// to avoid captive dependency issues — same pattern as AuthorizationForwardingHandler.
/// </summary>
[DocumentationInfo("Admin panel BFF auth delegating handler",
    "Injects Authorization (Keycloak service token) and X-Aizen-User-Token (identity JWT) " +
    "into all outgoing downstream HTTP calls. Replaces explicit per-method auth header " +
    "parameters across all IAizenRemoteCall-derived interfaces.")]
public sealed class AdminPanelBffAuthDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;

        if (context is not null)
        {
            // ── 1. Service-to-service token (Keycloak client_credentials) ──────────
            if (!request.Headers.Contains("Authorization"))
            {
                var tokenProvider = context.RequestServices
                    .GetRequiredService<IAdminPanelBffKeycloakServiceTokenProvider>();

                var serviceToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {serviceToken}");
            }

            // ── 2. Identity user token (from IAizenUserInfoAccessor, set by AizenUserInfoMiddleware) ──
            if (!request.Headers.Contains("X-Aizen-User-Token"))
            {
                var userInfoAccessor = context.RequestServices
                    .GetRequiredService<IAizenUserInfoAccessor>();

                var userToken = userInfoAccessor.UserInfo?.AccessToken;
                if (!string.IsNullOrWhiteSpace(userToken))
                    request.Headers.TryAddWithoutValidation("X-Aizen-User-Token", userToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
