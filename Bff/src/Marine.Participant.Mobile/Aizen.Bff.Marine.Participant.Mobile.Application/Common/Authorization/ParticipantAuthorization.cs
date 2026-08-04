using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;

public static class ParticipantAuthorizationPolicies
{
    public const string ParticipantAuthenticated = "ParticipantAuthenticated";
    public const string ParticipantActive = "ParticipantActive";
}

public static class ParticipantAuthorizationExtensions
{
    public static IServiceCollection AddMarineMobileAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Any verified Keycloak participant token.
            options.AddPolicy(ParticipantAuthorizationPolicies.ParticipantAuthenticated,
                p => p.RequireAuthenticatedUser());

            // Foundation: "active participant" == authenticated user holding the mobile_user realm role
            // (claims-based only). A runtime Identity status re-check (suspend/restrict taking effect while a
            // Keycloak token is still valid) is deferred to a later phase, once the participant profile
            // resolver + Identity lookup are wired in.
            options.AddPolicy(ParticipantAuthorizationPolicies.ParticipantActive, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("mobile_user");
            });
        });

        return services;
    }
}
