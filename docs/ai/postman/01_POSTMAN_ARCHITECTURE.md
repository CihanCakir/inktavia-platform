# Postman Architecture for Keycloak API Testing

## Purpose

This package provides a repeatable Postman setup for testing the Keycloak integration of the modular APIs.

It must validate:

```text
- Keycloak token acquisition
- Application client to API audience mapping
- User to role mapping
- API audience validation
- API role/policy validation
- Positive API access scenarios
- Negative authorization scenarios
- Anonymous request scenarios
```

## Source of Truth

The source of truth for endpoint discovery is the C# controllers under:

```text
src/MDYKE/**/Controller/V1/**/*Controller.cs
src/MDYKE/**/Controllers/V1/**/*Controller.cs
```

The source of truth for authentication and authorization is the Keycloak realm:

```text
infrastructure/keycloak/inktavia-realm-realm.json
```

If this file exists, the Postman sync process should read or align with:

```text
- Realm name
- Application clients
- API clients / audiences
- Test users
- Role mapping
```

If the Keycloak realm file cannot be parsed, use the default values documented in this folder.

## Keycloak Responsibility

Keycloak provides:

```text
- Realm: inktavia-realm
- Application/login clients
- API/resource audiences
- Access tokens
- Realm roles
- Audience mappers
```

## Postman Responsibility

Postman provides:

```text
- Environment variables for local testing
- Token helper requests
- API request folders
- Authorization headers
- Basic test scripts
- Negative test scenarios
- Diagnostics requests
```

## Backend API Responsibility

Each API validates its own token audience:

```text
Identity API -> identity-api
Profile API  -> profile-api
Payment API  -> payment-api
```

Each API validates policies/roles:

```text
IdentityRead
IdentityWrite
ProfileRead
ProfileWrite
PaymentRead
PaymentWrite
AdminOnly
```

## Client Access Matrix

| Application Client | Login User | Token Variable | identity-api | profile-api | payment-api |
|---|---|---|---:|---:|---:|
| inktavia-mobile | mobile.user@inktavia.com | mobile_access_token | Yes | Yes | No |
| customer-panel | customer.user@inktavia.com | customer_access_token | Yes | Yes | Read only |
| admin-panel | admin.user@inktavia.com | admin_access_token | Read/Write | Read/Write | Read/Write |

## Collection Folder Structure

The Postman collection must use this folder structure:

```text
00 - Keycloak Auth
01 - Identity API
02 - Profile API
03 - Payment API
04 - Auto-Discovered Endpoints
90 - Negative Authorization Tests
99 - Diagnostics
```

## Generated Endpoint Rule

The sync command must add new controller endpoints under:

```text
04 - Auto-Discovered Endpoints
```

Prefer module-based subfolders:

```text
04 - Auto-Discovered Endpoints
  Identity
  Profile
  Payment
  Unknown
```

Do not delete manually curated requests unless explicitly marked as generated.

Generated requests should include this description marker:

```text
Generated from Controller
```

## ASP.NET Identity Reminder

Postman validates Keycloak authentication and API access.

It does not replace application-level Identity logic.

The backend Identity module must still manage:

```text
- Local EF Core Identity users
- User profiles
- Active profile
- Agreement checks
- Domain-specific authorization
- Keycloak `sub` to local `KeycloakSubjectId` mapping
```
