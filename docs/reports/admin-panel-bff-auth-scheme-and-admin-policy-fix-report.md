# AdminPanel BFF Auth Scheme and Admin Policy Fix Report

## Scope

Fix 9 `AUTH_PIPELINE_DEFAULT_SCHEME_500` failures caused by missing authentication registration in the BFF.

## Root Cause

`AizenBffServiceConfiguration.Configure()` (Core.Starter.Bff) registered controllers, CQRS, remote calls, and info accessor — but never called `AddAuthentication()` or `AddJwtBearer()`. Controllers decorated with `[Authorize]` triggered `ChallengeAsync()` at the `UseAuthorization()` middleware, which threw "No authenticationScheme was specified, and there was no DefaultChallengeScheme found" → HTTP 500.

Additionally, `AizenBffApplicationConfiguration.Configure()` called `UseAuthorization()` without `UseAuthentication()`, so even if services were registered, the authentication middleware would not have run.

## Changes Applied

### 1. `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Program.cs`

Added JWT Bearer authentication reading from `X-Aizen-User-Token` header:

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Validate local symmetric JWT (Identity module token)
        options.TokenValidationParameters = new TokenValidationParameters { ... };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Read exclusively from X-Aizen-User-Token header
                var header = ctx.Request.Headers["X-Aizen-User-Token"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(header) && header.StartsWith("Bearer "))
                    ctx.Token = header["Bearer ".Length..].Trim();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPanelAccess", policy => policy.RequireAuthenticatedUser());
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
```

### 2. `Core/Starter/src/Aizen.Core.Starter.Bff/AizenBffApplicationConfiguration.cs`

Added `app.UseAuthentication()` before `app.UseAuthorization()`:

```csharp
app.UseAuthentication();  // ← Added
app.UseAuthorization();
```

## Security Model Enforced

- `X-Aizen-User-Token: Bearer <identityJWT>` → used for BFF inbound auth
- `Authorization` header → ignored for inbound BFF auth (reserved for BFF→module Keycloak service token)
- `[AllowAnonymous]` → preserved on login/otp/refresh endpoints
- FallbackPolicy → all endpoints require authentication unless explicitly anonymous

## Validation

```
dotnet build Aizen.Bff.AdminPanel.csproj
Build succeeded. 0 Error(s)
```

## Remaining Gaps

- In production, Keycloak authority should be configured in `TokenOption` or via `Keycloak:Authority` for asymmetric key validation.
- `AdminPanelAccess` policy currently only requires `RequireAuthenticatedUser()`. A future enhancement could add a role/claim check for Admin panel context (e.g., `RequireRole("Admin")`).
