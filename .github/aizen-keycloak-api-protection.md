# Aizen Keycloak API Protection Implementation Task

## Purpose

The current solution already uses Keycloak. API clients can successfully obtain tokens and send requests with `Authorization: Bearer {token}`.

However, the API controller endpoints are not fully protected. The objective is to make every controller endpoint require a valid Keycloak token by default, without breaking the existing Aizen framework architecture.

## Architectural Context

The project contains a framework-driven startup model.

The `Program.cs` files under services/modules should not be randomly modified with scattered authentication or authorization logic.

The existing startup architecture is primarily defined under:

```text
Core/Starter/src
```

The `Program.cs` files reference a `BuildForOperation` flow. The service and application implementations used by this flow are located under:

```text
Aizen.Core.Starter.Operation
```

The following classes must be analyzed and respected:

```text
AizenOperationServiceConfiguration
AizenOperationApplicationConfiguration
```

The authentication extension currently used by the framework is located under:

```text
Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs
```

The method to analyze and extend is:

```csharp
AddAizenAuth
```

## Required Direction

All authorization policy logic must be centralized under:

```text
Core/Auth/src/Aizen.Core.Auth
```

Authentication and authorization behavior should be managed from a single framework-level location.

The operation starter layer should only wire/use this framework-level behavior in the correct service registration and application pipeline stages.

## Main Goal

All classes ending with `Controller` must be protected by default.

No API endpoint should be accessible without a valid Keycloak token unless it is explicitly marked as public.

## Expected Security Model

Default behavior:

```text
Protected by default
```

Public behavior:

```text
Only explicitly allowed endpoints should use AllowAnonymous
```

## Required Public Endpoint Handling

Do not blindly make endpoints public.

Only these categories may be public after confirmation from the existing code:

- Login
- Token acquisition
- OTP send
- OTP verify
- Register
- External login callback/start endpoints
- Public agreement/version/config endpoints if they are intentionally public
- Health check endpoints
- Swagger/OpenAPI endpoints in development or intended environments

## Do Not Break

Do not break:

- Existing Keycloak token validation
- Existing Aizen starter architecture
- Existing `BuildForOperation` flow
- Existing service registration order
- Existing application middleware order unless it is wrong for authentication/authorization
- Existing controller routes
- Existing response models
- Existing user/claim accessor behavior
- Existing module boundaries

## Required Copilot Execution Order

Run the prompt files in this order:

1. `ai/aizen-keycloak-api-protection/00-context-and-objective.md`
2. `ai/aizen-keycloak-api-protection/01-discovery-architecture-map.md`
3. `ai/aizen-keycloak-api-protection/02-starter-operation-flow.md`
4. `ai/aizen-keycloak-api-protection/03-auth-package-analysis.md`
5. `ai/aizen-keycloak-api-protection/04-central-policy-design.md`
6. `ai/aizen-keycloak-api-protection/05-implement-auth-options-and-policies.md`
7. `ai/aizen-keycloak-api-protection/06-wire-operation-service-configuration.md`
8. `ai/aizen-keycloak-api-protection/07-wire-operation-application-pipeline.md`
9. `ai/aizen-keycloak-api-protection/08-protect-all-controllers.md`
10. `ai/aizen-keycloak-api-protection/09-allow-anonymous-public-endpoints.md`
11. `ai/aizen-keycloak-api-protection/10-swagger-health-check-and-dev-exceptions.md`
12. `ai/aizen-keycloak-api-protection/11-verification-tests-and-final-report.md`

## Acceptance Criteria

### Protected endpoint without token

```http
GET /api/v1/{any-protected-endpoint}
```

Expected:

```http
401 Unauthorized
```

or, depending on policy:

```http
403 Forbidden
```

### Protected endpoint with valid Keycloak token

```http
GET /api/v1/{any-protected-endpoint}
Authorization: Bearer {valid_keycloak_token}
```

Expected:

```http
200 OK
```

or the endpoint's normal business response.

### Login endpoint without token

```http
POST /api/v1/auth/login/username
```

Expected:

```http
200 OK
```

or a normal validation/business error such as:

```http
400 Bad Request
```

It must not return `401 Unauthorized` only because the request has no token.

### Health check

```http
GET /health
```

Expected:

```http
200 OK
```

if health checks are intentionally public.

### Swagger

Swagger/OpenAPI should remain accessible according to the existing environment rules. It must support Bearer token input for testing protected endpoints.
