using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace Aizen.Bff.AdminPanel.Extensions;

/// <summary>
/// Inbound authentication for the AdminPanel BFF: Keycloak (full IdP), RS256, audience admin-panel-bff.
/// A byte-for-byte mirror of the MarineProvider BFF's <c>AddMarineProviderAuthentication</c> (RS256 via JWKS — no
/// SymmetricSecurityKey / HS256): the human Keycloak token is read from the default <c>Authorization: Bearer</c> header,
/// exactly like the provider. The BFF→module Keycloak service token is injected outbound by the delegating handler, so
/// it never collides with the inbound human token. The legacy Identity HS256 token and X-Aizen-User-Token are gone.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddAdminPanelAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["AdminPanelKeycloak:BaseUrl"]?.TrimEnd('/');
        var realm = configuration["AdminPanelKeycloak:Realm"];
        var authority = configuration["AdminPanelKeycloak:Authority"] ?? $"{baseUrl}/realms/{realm}";
        var audience = configuration["AdminPanelKeycloak:Audience"]
                       ?? configuration["AdminPanelKeycloak:AdminPanelBffClientId"];
        var metadataAddress = configuration["AdminPanelKeycloak:MetadataAddress"];
        var requireHttps = bool.TryParse(configuration["AdminPanelKeycloak:RequireHttpsMetadata"], out var rh) && rh;

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

                    // The admin SPA connects to the BFF-hosted SignalR hub (/hubs/admin-messaging). A browser
                    // WebSocket cannot send an Authorization header, so SignalR passes the token as `?access_token=…`.
                    // Honour it ONLY for hub paths (/hubs/*) — never for the REST API, where a token in the URL would
                    // leak into logs, referrers and history. Mirrors the MarineProvider BFF.
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
    // RequireRole / policies (e.g. "AdminPanelAccess" → "Admin") can read them.
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
