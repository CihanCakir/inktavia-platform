# 04 - Central Policy Design

Design the centralized authorization policy model under:

```text
Core/Auth/src/Aizen.Core.Auth
```

## Goal

All controller endpoints should be protected by default with Keycloak authentication.

The implementation should be managed from the auth package, not scattered across service projects.

## Required Design

Create or extend a framework-level policy configuration mechanism.

Possible names, if they fit existing conventions:

```csharp
AizenAuthPolicyNames
AizenAuthorizationDefaults
AizenAuthOptions
AizenAuthorizationOptions
AizenAuthPolicyExtensions
```

Use existing naming conventions in the repository if different.

## Required Default Behavior

The framework should provide a default/fallback policy equivalent to:

```csharp
new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build();
```

or equivalent using the existing scheme constants.

## Preferred Implementation

Inside or near `AddAizenAuth`, configure:

```csharp
services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});
```

But avoid overwriting existing policies incorrectly.

If existing code already calls `AddAuthorization`, extend it safely.

## Policy Ownership

Policy names and default authorization behavior must live under:

```text
Core/Auth/src/Aizen.Core.Auth
```

The starter package may call the extension, but it should not define policy internals.

## Public Endpoints

Public endpoints must opt out explicitly with:

```csharp
[AllowAnonymous]
```

Do not create a broad allowlist based only on route strings unless the current architecture already uses endpoint conventions.

## Required Output

Before implementing, produce:

```md
# Central Policy Design

## Selected Strategy
- FallbackPolicy + DefaultPolicy / RequireAuthorization mapping / Hybrid

## Policy Location
- ...

## New Types
- ...

## Existing Types To Extend
- ...

## Public Endpoint Strategy
- ...

## Why This Preserves Architecture
- ...
```
