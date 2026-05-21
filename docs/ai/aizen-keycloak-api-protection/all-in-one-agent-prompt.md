# All-in-One Agent Prompt - Aizen Keycloak API Protection

Execute this task as a coding agent.

You must preserve the existing Aizen framework architecture.

The solution already uses Keycloak. Your task is not to create a new authentication system. Your task is to centralize authorization policy behavior under `Core/Auth/src/Aizen.Core.Auth`, wire it through `Aizen.Core.Starter.Operation`, and ensure all `*Controller` endpoints require a valid Keycloak token by default.

Follow every section in order.


---

# 00-context-and-objective.md

# 00 - Context and Objective

## Context

The solution already uses Keycloak.

Clients can successfully obtain tokens and call APIs by sending:

```http
Authorization: Bearer {token}
```

However, controller endpoints are not protected by default. Requests without tokens can still reach controller actions.

## Objective

Implement centralized API protection so that every controller endpoint requires a valid Keycloak token by default.

## Architecture Constraints

Do not create a new parallel authentication system.

Do not randomly add scattered authentication/authorization code into each service `Program.cs`.

The existing framework architecture must be preserved.

The startup model is defined under:

```text
Core/Starter/src
```

The service/module `Program.cs` files use a framework-level flow through:

```text
BuildForOperation
```

The service and application startup implementations are located under:

```text
Aizen.Core.Starter.Operation
```

The two classes that must be analyzed are:

```text
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
```

The existing authentication extension is located under:

```text
Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs
```

The method to inspect and extend is:

```csharp
AddAizenAuth
```

## Required Design Direction

All policy logic must be placed under:

```text
Core/Auth/src/Aizen.Core.Auth
```

This package must become the central place for:

- Authentication registration
- Keycloak/JWT bearer configuration
- Authorization defaults
- Fallback policy
- Optional named policies
- Any framework-level auth options

The operation starter layer must only wire this behavior into the existing service and application configuration flow.

## Security Requirement

All classes ending with `Controller` must require a valid Keycloak token by default.

Only explicitly public endpoints may be accessible without a token.

## Public Endpoint Rule

Do not guess.

Only mark endpoints as `[AllowAnonymous]` if they are clearly intended to be public, such as:

- Login
- Token generation/acquisition
- Register
- OTP send
- OTP verify
- External login callback/start
- Public configuration/version/agreement endpoints
- Health checks
- Swagger/OpenAPI where intended

## Output Required

Before editing code, summarize your understanding of:

1. The existing startup flow.
2. Where `AddAizenAuth` is called.
3. How Keycloak token validation is currently configured.
4. Why controllers are currently not protected.
5. Where the centralized fix should be implemented.


---

# 01-discovery-architecture-map.md

# 01 - Discovery: Architecture Map

Do not modify code in this step.

Map the current architecture.

## Search Targets

Search and inspect these paths and symbols:

```text
Core/Starter/src
BuildForOperation
Aizen.Core.Starter.Operation
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
Core/Auth/src/Aizen.Core.Auth
BuilderExtensions.cs
AddAizenAuth
UseAuthentication
UseAuthorization
AddAuthentication
AddAuthorization
FallbackPolicy
MapControllers
RequireAuthorization
AllowAnonymous
Authorize
```

## Questions to Answer

### 1. Program.cs Flow

Find the relevant `Program.cs` files.

Answer:

- How is `BuildForOperation` called?
- Which service configuration classes are used?
- Which application configuration classes are used?
- Does `Program.cs` directly call auth-related methods?
- Should the fix be added in `Program.cs` or in the framework starter?

### 2. Operation Starter Flow

Inspect:

```text
Aizen.Core.Starter.Operation
```

Answer:

- What does `AizenOperationServiceConfiguration` register?
- What does `AizenOperationApplicationConfiguration` configure?
- Where are services registered?
- Where is the middleware pipeline configured?
- Where are controllers mapped?
- Does this layer already call `AddAizenAuth`?
- Does this layer already call `UseAuthentication` and `UseAuthorization`?

### 3. Auth Package Flow

Inspect:

```text
Core/Auth/src/Aizen.Core.Auth
```

Answer:

- Where is `AddAizenAuth` defined?
- What services does it register?
- Does it add `AddAuthentication`?
- Does it add JWT Bearer?
- Does it add `AddAuthorization`?
- Does it define default or fallback policies?
- Does it expose options for auth behavior?

### 4. Controller Protection

Search all `*Controller.cs` files.

Answer:

- How many controllers exist?
- Which controllers/actions already use `[Authorize]`?
- Which controllers/actions already use `[AllowAnonymous]`?
- Are controllers protected by route mapping or by attributes?
- Is there any global authorization policy?

## Required Output

Produce this report before changing anything:

```md
# Architecture Discovery Report

## Program.cs Flow
- ...

## BuildForOperation Usage
- ...

## AizenOperationServiceConfiguration
- ...

## AizenOperationApplicationConfiguration
- ...

## AddAizenAuth Location and Current Behavior
- ...

## Current Controller Protection State
- ...

## Root Cause
- ...

## Recommended Implementation Location
- ...
```


---

# 02-starter-operation-flow.md

# 02 - Starter Operation Flow

This step focuses on the existing startup framework.

Modify code only if the correct integration points are clear.

## Target Area

Inspect:

```text
Core/Starter/src
Aizen.Core.Starter.Operation
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
BuildForOperation
```

## Goal

Understand how the operation service configuration and application configuration are used so the auth protection can be integrated without breaking the architecture.

## Service Configuration Requirements

`AizenOperationServiceConfiguration` should be checked for service registration responsibilities.

Determine whether this is the correct place to call or ensure:

```csharp
services.AddAizenAuth(...)
```

or the existing equivalent.

Do not duplicate registration if `AddAizenAuth` is already called elsewhere.

## Application Configuration Requirements

`AizenOperationApplicationConfiguration` should be checked for middleware pipeline responsibilities.

Determine whether this is the correct place to ensure:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

These must be called before controller mapping.

## Middleware Order Requirement

The expected order should be logically equivalent to:

```csharp
app.UseRouting();

app.UseCors(...); // if used by the existing project

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

If the framework uses custom wrappers, preserve them.

## Do Not

Do not:

- Bypass the starter architecture.
- Add one-off code to every service `Program.cs`.
- Change `BuildForOperation` public behavior unless required.
- Break existing service registration order.
- Break existing pipeline conventions.
- Remove existing middleware.

## Required Output

Produce:

```md
# Starter Operation Flow Result

## Correct Service Registration Point
- ...

## Correct Application Pipeline Point
- ...

## Required Changes
- ...

## Files To Change
- ...

## Risks
- ...
```


---

# 03-auth-package-analysis.md

# 03 - Auth Package Analysis

This step focuses on the `Aizen.Core.Auth` package.

## Target Area

Inspect:

```text
Core/Auth/src/Aizen.Core.Auth
Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs
```

Find:

```csharp
AddAizenAuth
```

## Goal

Understand the current Keycloak/JWT implementation and decide how to extend it so all authorization policy behavior is centralized in this package.

## Required Analysis

Answer these questions:

### Authentication

- Does `AddAizenAuth` call `AddAuthentication`?
- Does it use `JwtBearerDefaults.AuthenticationScheme`?
- Does it call `AddJwtBearer`?
- Where are Keycloak options loaded from?
- Are Authority, Audience, ClientId, Realm, MetadataAddress or similar settings used?
- Is configuration hard-coded or options-based?

### Token Validation

Check:

- `ValidateIssuer`
- `ValidateAudience`
- `ValidateLifetime`
- `ValidateIssuerSigningKey`
- `ClockSkew`
- `RequireHttpsMetadata`
- `MapInboundClaims`
- Claims mapping compatibility

### Authorization

Check:

- Does `AddAizenAuth` call `AddAuthorization`?
- Are there named policies?
- Is there a fallback policy?
- Is there a default policy?
- Is there any custom authorization handler?

### User Context Compatibility

Search for current user context/accessor classes and claim usage.

Do not break existing claim names such as:

```text
sub
userId
jti
version
refreshTokenExpire
role
scope
preferred_username
```

## Required Output

Produce:

```md
# Aizen.Core.Auth Analysis Report

## AddAizenAuth Current Behavior
- ...

## Keycloak Configuration Source
- ...

## Token Validation Current State
- ...

## Authorization Current State
- ...

## Missing Pieces
- ...

## Safe Extension Plan
- ...
```


---

# 04-central-policy-design.md

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


---

# 05-implement-auth-options-and-policies.md

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


---

# 06-wire-operation-service-configuration.md

# 06 - Wire Operation Service Configuration

This step wires the centralized auth behavior through the existing operation service configuration.

## Target

Inspect and update if required:

```text
Aizen.Core.Starter.Operation
AizenOperationServiceConfiguration
```

## Goal

Ensure all operation-based services call the centralized auth registration from:

```text
Core/Auth/src/Aizen.Core.Auth
```

Specifically, ensure the flow used by `BuildForOperation` results in:

```csharp
AddAizenAuth(...)
```

being called once and in the correct service registration stage.

## Requirements

### 1. Use Existing Starter Architecture

If `AizenOperationServiceConfiguration` is responsible for service registration, this is likely where the auth extension should be included or verified.

### 2. Do Not Duplicate

If `AddAizenAuth` is already called from another shared service configuration invoked by `BuildForOperation`, do not add it again.

Instead, confirm that all operation services go through that path.

### 3. Respect Existing Configuration Source

If `AddAizenAuth` requires configuration, options, environment or builder references, pass them using the existing framework pattern.

### 4. Avoid Direct Service Program.cs Changes

Do not modify every service `Program.cs`.

Only modify service `Program.cs` if the architecture itself requires a missing starter call and there is no central way to apply it.

## Required Output

Produce:

```md
# Operation Service Configuration Result

## AddAizenAuth Call Location
- ...

## Is It Already Called?
- Yes / No

## Change Made
- ...

## Why This Covers All API Services
- ...

## Files Changed
- ...
```


---

# 07-wire-operation-application-pipeline.md

# 07 - Wire Operation Application Pipeline

This step wires authentication and authorization middleware through the operation application configuration.

## Target

Inspect and update if required:

```text
Aizen.Core.Starter.Operation
AizenOperationApplicationConfiguration
```

## Goal

Ensure every operation API application uses the authentication and authorization middleware in the correct order.

## Required Middleware

The effective pipeline must include:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

They must run before controller endpoint execution.

## Required Order

The expected effective order should be equivalent to:

```csharp
app.UseRouting();

app.UseCors(...); // if used

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

The project may use minimal hosting or extension wrappers. Preserve existing structure.

## Controller Mapping

If the framework maps controllers centrally, consider whether this should become:

```csharp
app.MapControllers().RequireAuthorization();
```

However, if the fallback policy is properly configured in `Aizen.Core.Auth`, this may not be necessary.

Use one consistent strategy.

Preferred:

- Central fallback/default policy in `Aizen.Core.Auth`
- Pipeline calls in operation starter
- Public endpoints opt out with `[AllowAnonymous]`

## Do Not Break

Do not break:

- Swagger
- Health checks
- CORS
- Exception handling
- Logging
- Localization
- Rate limiting
- Existing endpoint mappings

## Required Output

Produce:

```md
# Operation Application Pipeline Result

## UseAuthentication
- Added / Already exists

## UseAuthorization
- Added / Already exists

## Middleware Order
1. ...
2. ...
3. ...

## Controller Mapping
- ...

## Files Changed
- ...

## Reasoning
- ...
```


---

# 08-protect-all-controllers.md

# 08 - Protect All Controllers

This step verifies that all `*Controller` classes are protected by default.

## Target

Search all controller classes:

```text
**/*Controller.cs
```

Especially under:

```text
Modules/**/src/**/Controller/V1/**/*Controller.cs
Modules/**/src/**/Controllers/V1/**/*Controller.cs
Core/**/src/**/*Controller.cs
```

## Goal

No controller action should be reachable without a Keycloak token unless it is explicitly marked as public.

## Preferred Protection Mechanism

The preferred protection mechanism is the central fallback policy in:

```text
Core/Auth/src/Aizen.Core.Auth
```

This means you should not need to add `[Authorize]` to every controller.

However, you must verify that the fallback policy actually applies to MVC controllers.

## Verification Work

For representative controllers:

- Check whether requests without tokens now receive `401` or `403`.
- Check whether valid Keycloak tokens allow access.
- Check whether `[AllowAnonymous]` is respected.

## Attribute Strategy

Do not add `[Authorize]` to every controller unless fallback policy does not apply in this architecture.

If fallback policy is not enough because of the endpoint mapping style, consider:

```csharp
app.MapControllers().RequireAuthorization();
```

from the central starter mapping location.

## Required Output

Produce:

```md
# Controller Protection Result

## Protection Strategy
- FallbackPolicy / RequireAuthorization / Controller attributes

## Controller Scan Summary
- Total controllers found:
- Controllers already using Authorize:
- Controllers/actions using AllowAnonymous:

## Gaps Found
- ...

## Changes Made
- ...
```


---

# 09-allow-anonymous-public-endpoints.md

# 09 - Allow Anonymous Public Endpoints

This step prevents intentionally public endpoints from being blocked by the new default protection.

## Goal

Add `[AllowAnonymous]` only to endpoints that must be accessible without a token.

## Search Targets

Search all `*Controller.cs` files and identify public endpoint categories.

Possible public endpoints include:

- Login
- Username login
- Phone login
- OTP send
- OTP verify
- Register
- External login start
- External login callback
- Token acquisition
- Refresh token if the existing design expects it to be called without access token
- Public agreement
- Public version
- Public app config
- Health-check controller if implemented as controller

## Important Security Rule

Do not mark an endpoint public just because it currently fails.

If it reads authenticated user context, user profile, active profile, role, device, payment, participant, organizer, venue owner or admin data, it must remain protected.

## Required Attribute

Use:

```csharp
using Microsoft.AspNetCore.Authorization;
```

and:

```csharp
[AllowAnonymous]
```

on the specific action.

Prefer action-level `[AllowAnonymous]` over controller-level unless the entire controller is truly public.

## Required Output

Produce:

```md
# AllowAnonymous Result

## Public Actions Marked
- `POST /api/v1/...` => reason

## Controllers Not Made Public
- `...Controller` => reason

## Files Changed
- ...
```


---

# 10-swagger-health-check-and-dev-exceptions.md

# 10 - Swagger, Health Check and Development Exceptions

This step verifies that infrastructure endpoints still work with the new default protection.

## Swagger

If Swagger/OpenAPI is configured in the framework, preserve the existing location.

Do not scatter Swagger configuration into service `Program.cs` if the framework already has a Swagger starter.

Ensure Swagger supports Bearer JWT input.

If missing, add a Bearer security definition in the existing Swagger/OpenAPI configuration location.

Reference shape:

```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
});
```

and security requirement referencing `"Bearer"`.

## Health Checks

If health checks are mapped centrally, keep them public if that is the existing intended behavior.

Examples:

```csharp
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapHealthChecks("/ready").AllowAnonymous();
app.MapHealthChecks("/live").AllowAnonymous();
```

Only apply routes that actually exist.

## Development Exceptions

Do not make Swagger publicly available in production if the existing project intentionally limits it by environment.

Preserve current environment checks.

## Required Output

Produce:

```md
# Swagger and Health Result

## Swagger Bearer Support
- Already exists / Added

## Health Check Public Access
- Already exists / Added

## Environment Rules Preserved
- ...

## Files Changed
- ...
```


---

# 11-verification-tests-and-final-report.md

# 11 - Verification, Tests and Final Report

This is the final implementation verification step.

## Build

Run the appropriate build command.

Prefer solution-level build if available:

```bash
dotnet build
```

If the repository requires a specific solution file, use it.

## Tests

Run tests if available:

```bash
dotnet test
```

If tests are not available or fail due to unrelated existing issues, report that clearly.

## Manual Verification

Verify these scenarios.

### 1. Protected endpoint without token

```http
GET /api/v1/{protected-endpoint}
```

Expected:

```http
401 Unauthorized
```

or:

```http
403 Forbidden
```

### 2. Protected endpoint with valid Keycloak token

```http
GET /api/v1/{protected-endpoint}
Authorization: Bearer {valid_keycloak_token}
```

Expected:

```http
200 OK
```

or the endpoint's normal business response.

### 3. Login endpoint without token

```http
POST /api/v1/auth/login/username
Content-Type: application/json

{
  "username": "test-user",
  "pin": "1234",
  "deviceId": "test-device",
  "notificationToken": "test-notification-token"
}
```

Expected:

```http
200 OK
```

or normal validation/business error.

It must not return `401 Unauthorized` just because the request has no token.

### 4. Health check

```http
GET /health
```

Expected:

```http
200 OK
```

if health checks are intentionally public.

### 5. Swagger

- Swagger UI opens according to existing environment rules.
- Bearer token can be entered.
- Protected endpoints can be tested with Bearer token.

## Final Report Format

Produce exactly this report:

```md
# Final Report - Aizen Keycloak API Protection

## 1. Root Cause

- ...

## 2. Architecture Preserved

- `Core/Starter/src`:
- `BuildForOperation`:
- `Aizen.Core.Starter.Operation`:
- `AizenOperationServiceConfiguration`:
- `AizenOperationApplicationConfiguration`:
- `Core/Auth/src/Aizen.Core.Auth`:

## 3. Changed Files

- `path/to/file.cs`
  - ...

## 4. AddAizenAuth Changes

- ...

## 5. Central Policy Design

- Default policy:
- Fallback policy:
- Named policies:

## 6. Operation Starter Changes

- Service configuration:
- Application configuration:

## 7. Controller Protection

- Strategy:
- All `*Controller` classes protected by default:
- Exceptions:

## 8. Public Endpoints

- ...

## 9. Swagger and Health Check

- ...

## 10. Verification

### Build
- ...

### Tests
- ...

### Manual Checks
- Protected without token:
- Protected with token:
- Login without token:
- Health:
- Swagger:

## 11. Risks and Follow-up

- ...
```
