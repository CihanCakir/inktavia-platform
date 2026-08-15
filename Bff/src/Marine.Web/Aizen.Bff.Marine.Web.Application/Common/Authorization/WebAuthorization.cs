using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Bff.Marine.Web.Application.Common.Authorization;

public static class WebAuthorizationPolicies
{
    public const string WebAuthenticated = "WebAuthenticated";
    public const string WebActive = "WebActive";
}

public static class WebAuthorizationExtensions
{
    public static IServiceCollection AddMarineWebAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Any verified Keycloak participant token.
            options.AddPolicy(WebAuthorizationPolicies.WebAuthenticated,
                p => p.RequireAuthenticatedUser());

            // Foundation: "active web participant" == authenticated user holding the web_user realm role
            // (claims-based only). A runtime Identity status re-check (suspend/restrict taking effect while a
            // Keycloak token is still valid) is deferred to a later phase, once the participant profile
            // resolver + Identity lookup are wired in.
            options.AddPolicy(WebAuthorizationPolicies.WebActive, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("web_user");
            });
        });

        return services;
    }
}
