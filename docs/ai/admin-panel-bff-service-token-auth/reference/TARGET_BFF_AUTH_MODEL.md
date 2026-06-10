# Target AdminPanel BFF Auth Model

## Incoming browser request

React Admin Web uses Identity login only.

Expected browser request header:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Browser requests must not be required to send:

```http
Authorization: Bearer <keycloakAccessToken>
```

## Outgoing internal API request

AdminPanel BFF calls internal APIs with:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

## Responsibility split

Identity token:

```text
Real user identity
Profile/context
Device context where available
Panel access
Domain authorization
```

Keycloak service token:

```text
BFF service identity
Machine-to-machine permission
Internal API audience
Service account roles
```

## Explicitly forbidden

- Do not forward incoming browser `Authorization` to internal APIs.
- Do not require browser Keycloak token for Admin Web BFF routes.
- Do not call internal module APIs directly from browser clients.
- Do not use Keycloak password grant or client credentials in browser code.
