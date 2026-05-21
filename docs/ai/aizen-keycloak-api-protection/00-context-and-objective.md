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
