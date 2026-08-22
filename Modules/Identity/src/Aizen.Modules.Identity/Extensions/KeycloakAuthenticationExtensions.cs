using System.Security.Claims;
using System.Text.Json;
using Aizen.Core.Common.Abstraction.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Aizen.Modules.Identity.Extensions;

public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Adds Keycloak JWT Bearer authentication using KEYCLOAK_* environment variables
    /// or Keycloak:* configuration section. Reads realm_access.roles and maps them to
    /// ClaimTypes.Role so authorization policies work with realm roles.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // FAZ14 (#30): doldurulmamış "__FROM_ENV__" yer-tutucusu = AYARLANMAMIŞ. Aksi hâlde options.Authority /
        // MetadataAddress yer-tutucuyla dolar ve JWKS keşfi çöker (herkes için 401; borç #53 ile aynı sınıf).
        var authority = AizenConfigPlaceholders.NullIfUnset(
            configuration["Keycloak:Authority"] ?? configuration["KEYCLOAK_AUTHORITY"]);

        var metadataAddress = AizenConfigPlaceholders.NullIfUnset(
            configuration["Keycloak:MetadataAddress"] ?? configuration["KEYCLOAK_METADATA_ADDRESS"]);

        var audience = AizenConfigPlaceholders.NullIfUnset(
            configuration["Keycloak:Audience"] ?? configuration["KEYCLOAK_AUDIENCE"]);

        var requireHttpsMetadataValue = configuration["Keycloak:RequireHttpsMetadata"]
            ?? configuration["KEYCLOAK_REQUIRE_HTTPS_METADATA"];

        var requireHttpsMetadata = bool.TryParse(requireHttpsMetadataValue, out var parsed) && parsed;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;

                if (!string.IsNullOrWhiteSpace(metadataAddress))
                    options.MetadataAddress = metadataAddress;

                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = authority,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "preferred_username"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                            return Task.CompletedTask;

                        var realmAccessClaim = context.Principal.FindFirst("realm_access")?.Value;

                        if (!string.IsNullOrWhiteSpace(realmAccessClaim))
                        {
                            using var doc = JsonDocument.Parse(realmAccessClaim);

                            if (doc.RootElement.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    var roleName = role.GetString();
                                    if (!string.IsNullOrWhiteSpace(roleName))
                                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                }
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
