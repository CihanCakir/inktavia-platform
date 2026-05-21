# All-in-One Agent Prompt - Aizen InfoAccessor Keycloak/User Token Separation

Execute this task as a coding agent.

The repository already uses Keycloak for API protection. Do not create a new authentication system.

Your goal is to analyze and extend `Core/InfoAccessor/src/Aizen.Core.InfoAccessor` so that `AizenUserInfoMiddleware` no longer tries to parse a Keycloak API/client token as an application user token.

There will be two token concepts after login:

1. `Authorization: Bearer {keycloak_api_client_token}` for API protection.
2. `X-Aizen-User-Token: {application_user_token}` for application user context.

The user token header name must be configurable and aligned with existing architecture.

Follow every section in order.


---

# 00-context-and-problem.md

# 00 - Context and Problem

## Context

The project already uses Keycloak successfully. A valid Keycloak token is obtained and sent to protected APIs. The API protection problem was solved earlier by centralizing authorization policy behavior.

## Current Problem

The current user info accessor flow under:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

contains a middleware similar to:

```text
Middlewares/AizenUserInfoMiddleware
```

This middleware tries to parse identity/user claims from the request token.

When the request token is a Keycloak API/client token, the middleware tries to parse it like an application user token and fails. This failure is expected because the Keycloak API/client token and the application user token are different concepts.

## Required Concept

After login, the system will have two tokens:

### Keycloak API/Client Token

Used for API protection.

```http
Authorization: Bearer {keycloak_api_client_token}
```

This token proves API access permission and must not be treated as application user identity.

### Application User Token

Used for application user context. This token represents the logged-in user and should be parsed by InfoAccessor.

It may include user claims such as:

```text
userId
sub
username
phoneNumber
email
profileId
role
version
refreshTokenExpire
```

The exact claim names must be discovered from the repository.

## Objective

Analyze the existing InfoAccessor architecture and implement a clean separation between:

1. API/client authentication token
2. Application user identity token

## Do Not

Do not:

- Break existing Keycloak API protection.
- Parse Keycloak client tokens as application user tokens.
- Hard-code claim names without checking existing conventions.
- Randomly modify all controllers.
- Create a one-off solution only for one API.
- Break existing InfoAccessor consumers.

## Expected Result

The middleware must become token-type aware.

If only the Keycloak API/client token exists, the request should remain authenticated at the API level, but user context should either remain empty/anonymous or be represented as a client context depending on existing framework patterns.

If a valid application user token exists, InfoAccessor should populate the current user context correctly.


---

# 01-discovery-infoaccessor-architecture.md

# 01 - Discovery: InfoAccessor Architecture

Do not modify code in this step.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search for:

```text
AizenUserInfoMiddleware
InfoAccessor
UserInfo
CurrentUser
IAizenUserInfoAccessor
IAizenInfoAccessor
IUserInfoAccessor
AizenUserInfo
AizenUserContext
ClaimsPrincipal
HttpContext
Authorization
Bearer
JwtSecurityTokenHandler
ReadJwtToken
FindFirst
userId
sub
version
refreshTokenExpire
```

## Required Questions

### Middleware

- Where is `AizenUserInfoMiddleware` located?
- What does it do?
- Which token/header does it read?
- What claims does it expect?
- What exception occurs when claims are missing?
- Does it fail hard or silently?
- Is it registered in the application pipeline?

### Accessor Interfaces

- Which interfaces expose user info?
- What models/classes are used for user info?
- Are they scoped/singleton/transient?
- Which parts of the application consume them?

### Current Claim Contract

Find current user token claim expectations. Document actual claim names from code.

### Token Source

Determine whether the middleware reads `Authorization`, `HttpContext.User.Claims`, custom header, cookie, query string, or something else.

### DI and Starter

Find where InfoAccessor is registered. Search:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
AizenInfoAccessor
InfoAccessorServiceCollectionExtensions
InfoAccessorApplicationBuilderExtensions
```

## Required Output

Produce:

```md
# InfoAccessor Discovery Report

## Middleware Location
- ...

## Middleware Current Behavior
- ...

## Current Token Source
- ...

## Expected Claims
- ...

## Current Failure Reason
- ...

## Accessor Interfaces and Models
- ...

## DI Registration
- ...

## Application Pipeline Registration
- ...

## Current Consumers
- ...

## Recommended Extension Point
- ...
```


---

# 02-token-separation-design.md

# 02 - Token Separation Design

Design the separation between Keycloak API/client token and application user token.

## Core Rule

Do not parse `Authorization: Bearer {keycloak_token}` as an application user token unless the token is confirmed to contain the required user identity claims.

## Required Token Types

Create a conceptual model for these token categories if it fits the existing architecture:

```csharp
public enum AizenTokenContextType
{
    None = 0,
    KeycloakClient = 1,
    ApplicationUser = 2
}
```

Use repository naming conventions if different.

## Required Contexts

### API Client Context

Represents the calling client/API identity validated by Keycloak. Possible fields:

```text
clientId
azp
audience
issuer
scope
realm
preferred_username
```

Use actual Keycloak claims present in the project.

### Application User Context

Represents the logged-in user. Possible fields:

```text
userId
sub
username
phoneNumber
email
profileId
role
version
refreshTokenExpire
```

Use actual application claim conventions.

## Recommended Design

Consider introducing these components only if they fit the existing architecture:

```text
IAizenTokenReader
IAizenUserTokenReader
IAizenKeycloakTokenReader
IAizenCurrentUserAccessor
IAizenCurrentClientAccessor
AizenTokenSourceOptions
AizenUserTokenOptions
AizenKeycloakTokenOptions
AizenUserInfoMiddlewareOptions
```

## Token Source Strategy

Because `Authorization` is already used for the Keycloak API/client token, the application user token should be read from a separate, configurable source.

Recommended default:

```http
X-Aizen-User-Token: {application_user_token}
```

The header name must be configurable.

Example config:

```json
{
  "Aizen": {
    "InfoAccessor": {
      "UserTokenHeaderName": "X-Aizen-User-Token",
      "KeycloakTokenHeaderName": "Authorization",
      "ThrowOnMissingUserToken": false,
      "ThrowOnInvalidUserToken": false
    }
  }
}
```

## Behavior Matrix

| Keycloak API Token | Application User Token | Result |
|---|---|---|
| Missing | Missing | API auth pipeline should reject protected endpoints before InfoAccessor matters |
| Present | Missing | API access allowed, user context empty/anonymous/client-only |
| Present | Present valid | API access allowed, user context populated |
| Present | Present invalid | configurable behavior: reject with 401 or keep empty and log |
| Missing | Present | protected endpoint should still fail if Keycloak token is required |

## Required Output

Produce:

```md
# Token Separation Design

## Selected Token Sources
- Keycloak API token:
- Application user token:

## New/Extended Interfaces
- ...

## New/Extended Models
- ...

## Middleware Behavior
- ...

## Configuration
- ...

## Backward Compatibility
- ...
```


---

# 03-user-token-location-and-contract.md

# 03 - User Token Location and Contract

Define how the application user token is carried and parsed.

## Requirement

The application now has two token concepts after login. The `Authorization` header is already used for Keycloak API/client token.

Therefore, the application user token must be read separately unless the existing project already has a different standard.

## Recommended User Token Header

Use a configurable header name.

Default recommendation:

```http
X-Aizen-User-Token: {application_user_token}
```

Prefer this unless the repository already uses another convention.

## Required Configuration

Add or extend options under `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`.

Example:

```csharp
public sealed class AizenInfoAccessorOptions
{
    public string UserTokenHeaderName { get; set; } = "X-Aizen-User-Token";
    public string AuthorizationHeaderName { get; set; } = "Authorization";
    public bool ThrowOnMissingUserToken { get; set; } = false;
    public bool ThrowOnInvalidUserToken { get; set; } = false;
}
```

Use existing options classes if present.

## Claim Contract

Do not invent claim names. Search the Identity/Auth token generation logic and find actual claims.

## Parsing Rule

The user token reader should:

1. Read token from the configured user token header.
2. Remove `Bearer ` prefix if present.
3. Validate whether required user claims exist.
4. Populate user info only when the token is an application user token.
5. Avoid throwing on missing user token unless configured.

## Required Output

Produce:

```md
# User Token Contract Result

## User Token Header
- ...

## Options Class
- ...

## Required Claims
- ...

## Optional Claims
- ...

## Invalid/Missing Token Behavior
- ...

## Files To Change
- ...
```


---

# 04-keycloak-token-safe-handling.md

# 04 - Keycloak Token Safe Handling

Ensure Keycloak API/client tokens are handled safely.

## Requirement

A Keycloak client/API token should not be parsed as an application user token.

## Keycloak Token Detection

Common Keycloak claims may include:

```text
iss
aud
azp
client_id
scope
typ
realm_access
resource_access
preferred_username
exp
iat
jti
```

A client credentials token may not contain application user claims.

## Safe Behavior

When the middleware sees only a Keycloak token:

- Do not throw because `userId`, `version`, `refreshTokenExpire` or similar user claims are missing.
- Optionally populate a client context if the architecture supports it.
- Keep user context empty/anonymous if no application user token exists.
- Let ASP.NET Core authentication/authorization handle API access.

## Optional Client Context

If useful and consistent with current architecture, introduce:

```text
IAizenClientInfoAccessor
AizenClientInfo
AizenKeycloakClientInfo
```

Fields may include:

```text
ClientId
AuthorizedParty
Audience
Issuer
Scope
Subject
PreferredUsername
```

Do not over-engineer if current consumers only need user context.

## Required Output

Produce:

```md
# Keycloak Token Safe Handling Result

## Keycloak Claims Observed
- ...

## Client Token Detection Strategy
- ...

## User Claim Parsing Guard
- ...

## Optional Client Context
- Added / Not added, reason

## Files To Change
- ...
```


---

# 05-implement-new-accessors-and-options.md

# 05 - Implement New Accessors and Options

Implement the selected design inside:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

## Main Goal

Support separate accessors/readers for:

1. Keycloak/API client context
2. Application user context

## Implementation Requirements

### Options

Add or extend options for token sources and behavior.

Recommended properties:

```csharp
public string UserTokenHeaderName { get; set; } = "X-Aizen-User-Token";
public string AuthorizationHeaderName { get; set; } = "Authorization";
public bool ThrowOnMissingUserToken { get; set; } = false;
public bool ThrowOnInvalidUserToken { get; set; } = false;
```

Use existing options conventions if present.

### User Token Reader

Create or adapt a component responsible only for application user token parsing.

Responsibilities:

- Read configured user token header.
- Strip `Bearer ` prefix if present.
- Parse JWT safely.
- Validate expected user claims.
- Return a result object instead of throwing for normal missing claim scenarios.
- Populate existing user info model if valid.

### Keycloak Token Reader

Create or adapt a component that understands Keycloak token shape.

Responsibilities:

- Read `Authorization` header or `HttpContext.User.Claims`.
- Extract client-related claims safely.
- Never require application user claims.
- Return a client context if implemented.

### Result Type

Prefer a safe result type or existing project result pattern.

## Do Not

Do not:

- Throw for missing user token by default.
- Throw for Keycloak client token missing application user claims.
- Break existing accessor interface consumers.
- Hard-code headers where options should be used.

## Required Output

Produce:

```md
# New Accessors and Options Result

## New Types
- ...

## Extended Types
- ...

## Options
- ...

## User Token Reader
- ...

## Keycloak Token Reader
- ...

## Compatibility Notes
- ...
```


---

# 06-middleware-refactor.md

# 06 - Middleware Refactor

Refactor:

```text
Middlewares/AizenUserInfoMiddleware
```

## Goal

Make middleware safe for two-token architecture.

## Required Behavior

### Case 1: Only Keycloak API token exists

Expected:

- Do not attempt to parse it as application user token.
- Do not throw because application user claims are missing.
- Allow request to continue if ASP.NET Core authorization has accepted the request.
- User context remains empty/anonymous or client-only depending on design.

### Case 2: Keycloak API token + valid application user token exists

Expected:

- API access is protected by Keycloak.
- User token is parsed separately.
- Current user context is populated.

### Case 3: Keycloak API token + invalid application user token exists

Expected depends on configuration:

- If `ThrowOnInvalidUserToken = true`, return/reject as unauthorized or throw framework auth exception.
- If `false`, log warning and continue with empty user context.

### Case 4: Missing both tokens

Protected endpoints should already be rejected by Keycloak authorization before user-context logic becomes meaningful.

## Middleware Design

Refactor the middleware so that it delegates parsing logic to reader/accessor services.

Avoid having claim parsing logic directly inside the middleware if the architecture supports services.

## Logging

Add safe debug/warning logs if logging infrastructure exists. Do not log raw token values.

## Required Output

Produce:

```md
# Middleware Refactor Result

## Changed Behavior
- ...

## Token Flow
- ...

## Error Handling
- ...

## Logging
- ...

## Files Changed
- ...
```


---

# 07-starter-and-di-wiring.md

# 07 - Starter and DI Wiring

Wire the new InfoAccessor components into the existing framework registration.

## Target

Inspect the registration points for:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
InfoAccessorServiceCollectionExtensions
InfoAccessorApplicationBuilderExtensions
AizenUserInfoMiddleware
```

## Requirements

Register the new services using the existing lifetime patterns.

Likely lifetimes:

- Accessors storing request context: Scoped
- Token readers without state: Scoped or Singleton depending on dependencies
- Options: Options pattern

Do not guess if existing lifetimes are clear.

## Configuration

Bind options from configuration if the package already supports configuration.

Example config path:

```json
{
  "Aizen": {
    "InfoAccessor": {
      "UserTokenHeaderName": "X-Aizen-User-Token",
      "ThrowOnMissingUserToken": false,
      "ThrowOnInvalidUserToken": false
    }
  }
}
```

Use existing config section naming if different.

## Pipeline

Ensure `AizenUserInfoMiddleware` is still registered in the existing pipeline.

Recommended relative order depends on existing architecture. If it reads `HttpContext.User`, it should run after authentication. If it must be available before authorization handlers, document why.

## Required Output

Produce:

```md
# Starter and DI Wiring Result

## DI Registrations
- ...

## Options Binding
- ...

## Middleware Registration
- ...

## Middleware Order
- ...

## Files Changed
- ...
```


---

# 08-backward-compatibility-and-consumers.md

# 08 - Backward Compatibility and Consumers

Check existing consumers of InfoAccessor.

## Goal

Do not break current code that depends on user info accessor interfaces.

## Search Consumers

Search usages of:

```text
IAizenUserInfoAccessor
IAizenInfoAccessor
AizenUserInfo
CurrentUser
UserId
GetUserId
MetropolUserId
PhoneNumber
Version
RefreshTokenExpire
```

## Required Compatibility Decisions

If current consumers expect user info to always exist, choose one safe pattern:

### Option A - Existing behavior preserved for user endpoints

Protected user-level endpoints must provide `X-Aizen-User-Token`. If missing, accessor returns empty and consumer-level business rules handle it.

### Option B - Explicit user context required

For endpoints that require a user token, add a reusable guard/policy/filter later. Do not implement this broadly unless requested.

### Option C - Accessor exposes availability

Add properties/methods such as:

```csharp
bool IsAuthenticatedUserAvailable { get; }
bool IsClientContextAvailable { get; }
```

Use existing style.

## Important

Do not make Keycloak client token impersonate an application user.

Do not fill `UserId` from `client_id`, `azp`, or `preferred_username` unless the system explicitly defines that mapping.

## Required Output

Produce:

```md
# Backward Compatibility Result

## Consumers Found
- ...

## Potential Breaking Points
- ...

## Compatibility Strategy
- ...

## Additional Work Needed
- ...
```


---

# 09-tests-and-verification.md

# 09 - Tests and Verification

Add or update tests if the repository has test projects.

## Required Test Scenarios

### 1. Keycloak token only

Input:

```http
Authorization: Bearer {keycloak_client_token}
```

Expected:

- Middleware does not throw.
- Application user context is empty/anonymous.
- Optional client context is populated if implemented.

### 2. Keycloak token + valid user token

Input:

```http
Authorization: Bearer {keycloak_client_token}
X-Aizen-User-Token: {application_user_token}
```

Expected:

- Middleware does not throw.
- Application user context is populated.
- Expected claims are mapped correctly.

### 3. Keycloak token + invalid user token

Input:

```http
Authorization: Bearer {keycloak_client_token}
X-Aizen-User-Token: invalid
```

Expected depends on configuration.

### 4. Missing user token

Input:

```http
Authorization: Bearer {keycloak_client_token}
```

Expected:

- No claim missing exception.
- No failure from InfoAccessor layer.

### 5. Existing user token parsing compatibility

If old behavior parsed user token from `Authorization`, test whether backward compatibility is needed. If not implemented, document migration.

## Build and Test

Run:

```bash
dotnet build
dotnet test
```

## Required Output

Produce:

```md
# Verification Result

## Build
- ...

## Tests
- ...

## Test Scenarios
- Keycloak token only:
- Keycloak + user token:
- Invalid user token:
- Missing user token:
- Backward compatibility:

## Issues
- ...
```


---

# 10-final-report.md

# 10 - Final Report

Produce the final report in this exact format:

```md
# Final Report - Aizen InfoAccessor Keycloak/User Token Separation

## 1. Root Cause

- ...

## 2. Existing Architecture Reviewed

- `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`:
- `Middlewares/AizenUserInfoMiddleware`:
- Current accessor interfaces:
- Current token source:
- Current claim expectations:

## 3. New Token Model

### Keycloak API/Client Token

- Source:
- Purpose:
- Parsed by:

### Application User Token

- Source:
- Purpose:
- Parsed by:

## 4. Changed Files

- `path/to/file.cs`
  - ...

## 5. New/Updated Types

- ...

## 6. Middleware Behavior

| Scenario | Behavior |
|---|---|
| Keycloak token only | ... |
| Keycloak + valid user token | ... |
| Keycloak + invalid user token | ... |
| Missing user token | ... |

## 7. Configuration

- Section:
- Properties:
- Default user token header:

## 8. Backward Compatibility

- ...

## 9. Tests and Verification

### Build
- ...

### Tests
- ...

### Manual Checks
- ...

## 10. Migration Notes for API Consumers

- API protection token should be sent with:
  - `Authorization: Bearer {keycloak_api_client_token}`
- Application user token should be sent with:
  - `X-Aizen-User-Token: {application_user_token}`

## 11. Risks and Follow-up

- ...
```
