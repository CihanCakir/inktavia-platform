# 05 - Implement Auth Options and Policies

Now implement the centralized policy changes under:

```text
Core/Auth/src/Aizen.Core.Auth
```

## Primary Target

```text
Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs
```

Method:

```csharp
AddAizenAuth
```

## Implementation Requirements

### 1. Preserve Existing Keycloak Authentication

Do not remove the current Keycloak/JWT Bearer registration.

Do not replace it with a separate authentication setup.

If `AddAizenAuth` already configures JWT Bearer, extend it.

### 2. Add Centralized Authorization

Add or extend authorization setup so authenticated users are required by default.

Preferred behavior:

```csharp
services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});
```

If existing policies exist, preserve them and add only missing default/fallback behavior.

### 3. Avoid Policy Duplication

If `AddAuthorization` is already configured in the auth package, modify that existing block instead of adding another unrelated block.

### 4. Use Existing Scheme Names

If the project defines its own scheme constant, use it.

Otherwise use:

```csharp
JwtBearerDefaults.AuthenticationScheme
```

### 5. Add Policy Constants If Useful

If existing style supports constants, add something like:

```csharp
public static class AizenAuthPolicyNames
{
    public const string AuthenticatedUser = "Aizen.AuthenticatedUser";
}
```

Only add this if it is useful and consistent with the codebase.

### 6. Do Not Hard-Code Keycloak Settings

Do not hard-code:

- Keycloak URL
- Realm
- ClientId
- Audience
- MetadataAddress

Use existing options/configuration.

## Required Verification After Implementation

Search the solution for multiple conflicting `AddAuthorization` calls.

If service projects override the auth package fallback policy, report it.

## Required Output

Produce:

```md
# Auth Policy Implementation Result

## Changed Files
- ...

## AddAizenAuth Changes
- ...

## Default Policy
- ...

## Fallback Policy
- ...

## Existing Policies Preserved
- ...

## Notes
- ...
```
