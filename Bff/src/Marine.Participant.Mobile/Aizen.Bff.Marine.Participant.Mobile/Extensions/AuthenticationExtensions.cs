using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace Aizen.Bff.Marine.Participant.Mobile.Extensions;

/// <summary>
/// Inbound authentication for the Marine Participant Mobile BFF: Keycloak (full IdP), RS256, audience
/// marine-mobile-bff. The legacy Identity HS256 token is NOT used as the human auth source.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddMarineMobileAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["MarineMobileKeycloak:BaseUrl"]?.TrimEnd('/');
        var realm = configuration["MarineMobileKeycloak:Realm"];
        var authority = configuration["MarineMobileKeycloak:Authority"] ?? $"{baseUrl}/realms/{realm}";
        var audience = configuration["MarineMobileKeycloak:Audience"]
                       ?? configuration["MarineMobileKeycloak:BffClientId"];
        var metadataAddress = configuration["MarineMobileKeycloak:MetadataAddress"];
        var requireHttps = bool.TryParse(configuration["MarineMobileKeycloak:RequireHttpsMetadata"], out var rh) && rh;

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                if (!string.IsNullOrWhiteSpace(metadataAddress))
                    options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = requireHttps;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = MapKeycloakRealmRoles,

                    // A browser WebSocket cannot send an Authorization header. SignalR therefore passes the token as
                    // `?access_token=…`, and it is only honoured for the hub path — never for the REST API, where a
                    // token in the URL would end up in logs, referrers and browser history. Harmless in Foundation
                    // (no hub is mapped yet); kept so realtime can be added later without touching auth.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }

    // Flatten Keycloak realm_access.roles (nested JSON) into ClaimTypes.Role so ASP.NET
    // RequireRole / policies can read mobile_user.
    private static Task MapKeycloakRealmRoles(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
            return Task.CompletedTask;

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var value = role.GetString();
                    if (!string.IsNullOrWhiteSpace(value) && !identity.HasClaim(ClaimTypes.Role, value))
                        identity.AddClaim(new Claim(ClaimTypes.Role, value));
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed realm_access; authorization policies still enforce access.
        }

        return Task.CompletedTask;
    }
}
