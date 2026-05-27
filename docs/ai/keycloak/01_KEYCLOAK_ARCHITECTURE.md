# Keycloak Architecture for Inktavia / Aizen Modules

## Target Architecture

The system uses Keycloak as the central authentication and token provider.

Keycloak responsibilities:

```text
- Realm management
- Client management
- Login flow
- Access token generation
- Refresh token generation
- Realm role management
- Audience claim generation
- Application client authentication flow
```

Identity module responsibilities:

```text
- Local user entity management
- ASP.NET Identity / Entity Framework Identity persistence
- User profile management
- Active profile management
- Agreement checks
- Panel-specific business rules
- Domain-level user state
- Local user mapping with Keycloak `sub` claim
```

Payment module responsibilities:

```text
- Payment operations
- Payment read/write authorization
- JWT token validation
- Audience validation for `payment-api`
```

Profile module responsibilities:

```text
- Profile operations
- Profile read/write authorization
- JWT token validation
- Audience validation for `profile-api`
```

## Important Design Rule

Do not remove `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.

Keycloak and ASP.NET Identity are not replacing each other.

They have separate responsibilities:

```text
Keycloak:
  Authentication, token, realm, client, role, audience.

ASP.NET Identity:
  Local application user, profile, agreement, active profile, business user data.
```

## Realm

Realm name:

```text
inktavia-realm
```

## API Clients

These clients represent backend APIs / resource servers:

```text
identity-api
payment-api
profile-api
```

They are not used as login clients.

Each API validates its own audience:

```text
Identity API -> audience: identity-api
Payment API  -> audience: payment-api
Profile API  -> audience: profile-api
```

## Application Clients

These clients are used by users to login:

```text
inktavia-mobile
customer-panel
admin-panel
```

## Access Matrix

| Application Client | identity-api | profile-api | payment-api | Description |
|---|---:|---:|---:|---|
| inktavia-mobile | Yes | Yes | No | Mobile users can authenticate and manage profile data. They cannot directly access Payment API. |
| customer-panel | Yes | Yes | Read only | Customer panel can authenticate, read profile data and read payment data. |
| admin-panel | Read/Write | Read/Write | Read/Write | Admin panel has elevated permissions across all APIs. |

## Audience Mapping

Access tokens must include only the API audiences allowed for the application client.

Expected token audience mapping:

```text
inktavia-mobile:
  - identity-api
  - profile-api

customer-panel:
  - identity-api
  - profile-api
  - payment-api

admin-panel:
  - identity-api
  - profile-api
  - payment-api
```

## Role Model

Use realm roles for the first phase.

Realm roles:

```text
mobile_user
customer_user
admin_user

identity_read
identity_write

profile_read
profile_write

payment_read
payment_write
```

Client roles can be introduced later if service-level isolation becomes more complex.

## Authorization Principle

Application clients define where the token can be used through audience mapping.

Realm roles define what the user can do inside the allowed APIs.

Example:

```text
A token with audience `payment-api` but without `payment_read` or `payment_write`
must be rejected by Payment API policies.

A token with `payment_read` role but without `payment-api` audience
must also be rejected by Payment API audience validation.
```

## Identity and Local User Mapping

Keycloak user identity must be mapped to local Identity user data by:

```text
access_token.sub -> UserEntity.KeycloakSubjectId
```

The Identity module must continue to own local domain-specific user data.
