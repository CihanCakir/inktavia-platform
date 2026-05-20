# .NET Keycloak and ASP.NET Identity Integration

## Main Rule

Do not remove the existing ASP.NET Identity Entity Framework setup.

The Identity module currently uses:

```text
Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

This must remain.

## Responsibility Split

Keycloak:

```text
- Login
- Token generation
- Refresh token
- Realm
- Client
- Role
- Audience
```

Identity module:

```text
- Local user persistence
- User profile
- Active profile
- Agreement
- Domain permissions
- Panel-specific business authorization
- User lifecycle inside the application
```

## User Mapping

Map Keycloak user to local user using:

```text
access_token.sub -> UserEntity.KeycloakSubjectId
```

Add this field to the local Identity user entity if it does not already exist:

```csharp
public string? KeycloakSubjectId { get; private set; }
```

Example:

```csharp
public class UserEntity : IdentityUser<long>
{
    public string? KeycloakSubjectId { get; private set; }

    public void SetKeycloakSubjectId(string subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException("Keycloak subject id cannot be empty.", nameof(subjectId));

        KeycloakSubjectId = subjectId;
    }
}
```

EF Core configuration:

```csharp
builder.Property(x => x.KeycloakSubjectId)
    .HasMaxLength(128);

builder.HasIndex(x => x.KeycloakSubjectId)
    .IsUnique()
    .HasFilter("\"KeycloakSubjectId\" IS NOT NULL");
```

If the project uses SQL Server instead of PostgreSQL, adjust the filtered index syntax.

## Missing Local User Behavior

If Keycloak token is valid but local user does not exist, prefer first-phase controlled provisioning.

Controlled provisioning:

```text
- Token is validated.
- Identity API reads the `sub`, `email`, `preferred_username` claims.
- Identity API checks local UserEntity by KeycloakSubjectId.
- If not found, Identity API creates a local user and base profile.
- User continues with onboarding/profile completion if required.
```

Alternative strict matching:

```text
- Token is valid.
- Local user is not found.
- API returns 401 or 403.
- UI redirects user to onboarding/profile completion.
```

First phase recommendation:

```text
Controlled provisioning
```

## JWT Authentication Extension

Create or adapt:

```text
Extensions/KeycloakAuthenticationExtensions.cs
```

Expected behavior:

```text
- Add JwtBearer authentication.
- Read Authority from KEYCLOAK_AUTHORITY.
- Read MetadataAddress from KEYCLOAK_METADATA_ADDRESS.
- Read Audience from KEYCLOAK_AUDIENCE.
- RequireHttpsMetadata must be configurable.
- Validate issuer.
- Validate audience.
- Validate lifetime.
- Map realm_access.roles to ClaimTypes.Role.
```

Reference implementation shape:

```csharp
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"]
            ?? configuration["KEYCLOAK_AUTHORITY"];

        var metadataAddress = configuration["Keycloak:MetadataAddress"]
            ?? configuration["KEYCLOAK_METADATA_ADDRESS"];

        var audience = configuration["Keycloak:Audience"]
            ?? configuration["KEYCLOAK_AUDIENCE"];

        var requireHttpsMetadataValue = configuration["Keycloak:RequireHttpsMetadata"]
            ?? configuration["KEYCLOAK_REQUIRE_HTTPS_METADATA"];

        var requireHttpsMetadata = bool.TryParse(requireHttpsMetadataValue, out var parsed)
            ? parsed
            : false;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
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
                        var identity = context.Principal?.Identity as ClaimsIdentity;

                        if (identity is null)
                            return Task.CompletedTask;

                        var realmAccessClaim = context.Principal?.FindFirst("realm_access")?.Value;

                        if (!string.IsNullOrWhiteSpace(realmAccessClaim))
                        {
                            using var jsonDocument = JsonDocument.Parse(realmAccessClaim);

                            if (jsonDocument.RootElement.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    var roleName = role.GetString();

                                    if (!string.IsNullOrWhiteSpace(roleName))
                                    {
                                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                    }
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
```

Adapt namespace and project-specific conventions.

## Authorization Policy Extension

Create or adapt:

```text
Extensions/AuthorizationPolicyExtensions.cs
```

Expected policies:

```csharp
public static class AuthorizationPolicyExtensions
{
    public static IServiceCollection AddInktaviaAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("IdentityRead", policy =>
                policy.RequireRole("identity_read", "admin_user"));

            options.AddPolicy("IdentityWrite", policy =>
                policy.RequireRole("identity_write", "admin_user"));

            options.AddPolicy("ProfileRead", policy =>
                policy.RequireRole("profile_read", "admin_user"));

            options.AddPolicy("ProfileWrite", policy =>
                policy.RequireRole("profile_write", "admin_user"));

            options.AddPolicy("PaymentRead", policy =>
                policy.RequireRole("payment_read", "admin_user"));

            options.AddPolicy("PaymentWrite", policy =>
                policy.RequireRole("payment_write", "admin_user"));

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin_user"));
        });

        return services;
    }
}
```

## Current User Service

Create or adapt:

```text
Services/ICurrentUserService.cs
Services/CurrentUserService.cs
```

Expected interface:

```csharp
public interface ICurrentUserService
{
    string? KeycloakSubjectId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
}
```

Expected implementation:

```csharp
using System.Security.Claims;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? KeycloakSubjectId =>
        _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

    public string? Username =>
        _httpContextAccessor.HttpContext?.User.FindFirst("preferred_username")?.Value;

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value;

    public IReadOnlyCollection<string> Roles =>
        _httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct()
            .ToArray()
        ?? Array.Empty<string>();
}
```

Register:

```csharp
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

## Program.cs Expected Usage

Add in each API:

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInktaviaAuthorizationPolicies();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

Respect existing middleware order.

## Swagger

Add Bearer security support to Swagger.

Expected:

```text
Swagger UI must have Authorize button.
Protected endpoints must accept Bearer token.
```

Use existing Swagger extension if available.

## Endpoint Examples

Identity API:

```csharp
[Authorize(Policy = "IdentityRead")]
[HttpGet("me")]
public IActionResult GetMe()
{
    return Ok();
}
```

Profile API:

```csharp
[Authorize(Policy = "ProfileRead")]
[HttpGet("profiles/me")]
public IActionResult GetMyProfile()
{
    return Ok();
}
```

Payment API:

```csharp
[Authorize(Policy = "PaymentRead")]
[HttpGet("payments")]
public IActionResult GetPayments()
{
    return Ok();
}
```

Admin-only:

```csharp
[Authorize(Policy = "AdminOnly")]
[HttpGet("admin-only")]
public IActionResult AdminOnly()
{
    return Ok();
}
```

## Business Authorization Note

JWT authentication confirms who the user is and which Keycloak roles/audiences are present.

Domain-level rules still belong to the application.

Examples:

```text
- Is the user profile active?
- Has the user accepted the latest agreement?
- Is the user allowed to access this panel?
- Is the user blocked or suspended locally?
- Is the user allowed to operate on this domain record?
```

These checks should remain in the Identity module or relevant domain services.
