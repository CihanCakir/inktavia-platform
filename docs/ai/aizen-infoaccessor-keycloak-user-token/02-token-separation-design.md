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
