# Keycloak Implementation Tasks

## 1. Analyze Existing Repository

Inspect:

```text
Modules/
docker-compose.yaml
src/
tests/
infrastructure/
```

Find these modules:

```text
Identity
Payment
Profile
```

For each module identify:

```text
- Project path
- API project path
- Program.cs or Startup.cs
- Existing authentication setup
- Existing authorization setup
- Existing Swagger setup
- Existing appsettings files
- Existing environment variable patterns
- Docker Compose service name
- Internal port
- External port
```

Do not make assumptions if the information exists in the repository.

Use the existing naming and extension method style where possible.

---

## 2. Update Docker Compose

Update the existing Keycloak service to use this realm import file:

```text
infrastructure/keycloak/inktavia-realm-realm.json
```

Expected Keycloak service shape:

```yaml
keycloak:
  image: quay.io/keycloak/keycloak:25.0.0
  container_name: keycloak
  depends_on:
    - keycloak-db
  command: ["start-dev", "--import-realm", "--http-port=8080"]
  environment:
    KC_DB: postgres
    KC_DB_URL: jdbc:postgresql://keycloak-db:5432/${KC_DB_NAME:-keycloak}
    KC_DB_USERNAME: ${KC_DB_USER:-keycloak}
    KC_DB_PASSWORD: ${KC_DB_PASSWORD:-keycloakpw}

    KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN:-admin}
    KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD:-admin}

    KC_HOSTNAME: ${KC_HOSTNAME:-http://localhost:8080}
    KC_HOSTNAME_BACKCHANNEL_DYNAMIC: "true"
  ports:
    - "8080:8080"
  volumes:
    - ./infrastructure/keycloak/inktavia-realm-realm.json:/opt/keycloak/data/import/inktavia-realm-realm.json:ro
  networks: [aizen]
```

Keep the existing `keycloak-db` service.

Keep the existing `aizen` network.

If the project uses a different network name, use the existing network.

---

## 3. Create Realm Import File

Create:

```text
infrastructure/keycloak/inktavia-realm-realm.json
```

This file must include:

```text
- Realm: inktavia-realm
- Realm roles
- API clients
- Application clients
- Protocol mappers for audience
- Test users
- User role mappings
```

Follow:

```text
docs/ai/keycloak/03_REALM_IMPORT_SPEC.md
```

---

## 4. Add API Environment Variables

Add Keycloak environment variables to each API service in Docker Compose.

Identity API:

```yaml
KEYCLOAK_AUTHORITY: http://localhost:8080/realms/inktavia-realm
KEYCLOAK_METADATA_ADDRESS: http://keycloak:8080/realms/inktavia-realm/.well-known/openid-configuration
KEYCLOAK_AUDIENCE: identity-api
KEYCLOAK_REQUIRE_HTTPS_METADATA: "false"
```

Payment API:

```yaml
KEYCLOAK_AUTHORITY: http://localhost:8080/realms/inktavia-realm
KEYCLOAK_METADATA_ADDRESS: http://keycloak:8080/realms/inktavia-realm/.well-known/openid-configuration
KEYCLOAK_AUDIENCE: payment-api
KEYCLOAK_REQUIRE_HTTPS_METADATA: "false"
```

Profile API:

```yaml
KEYCLOAK_AUTHORITY: http://localhost:8080/realms/inktavia-realm
KEYCLOAK_METADATA_ADDRESS: http://keycloak:8080/realms/inktavia-realm/.well-known/openid-configuration
KEYCLOAK_AUDIENCE: profile-api
KEYCLOAK_REQUIRE_HTTPS_METADATA: "false"
```

Important:

```text
KEYCLOAK_AUTHORITY uses localhost because the token issuer is exposed as localhost for local development.
KEYCLOAK_METADATA_ADDRESS uses keycloak:8080 because API containers access Keycloak over the Docker network.
```

---

## 5. Add .NET Authentication

For Identity, Payment and Profile APIs:

```text
- Add JWT Bearer authentication.
- Validate issuer.
- Validate audience.
- Validate lifetime.
- Map realm_access.roles to ClaimTypes.Role.
- Use KEYCLOAK_AUTHORITY.
- Use KEYCLOAK_METADATA_ADDRESS.
- Use KEYCLOAK_AUDIENCE.
```

Create or adapt:

```text
Extensions/KeycloakAuthenticationExtensions.cs
```

---

## 6. Add Authorization Policies

Create or adapt:

```text
Extensions/AuthorizationPolicyExtensions.cs
```

Policies:

```text
IdentityRead
IdentityWrite
ProfileRead
ProfileWrite
PaymentRead
PaymentWrite
AdminOnly
```

Policy mapping:

```text
IdentityRead  -> identity_read or admin_user
IdentityWrite -> identity_write or admin_user
ProfileRead   -> profile_read or admin_user
ProfileWrite  -> profile_write or admin_user
PaymentRead   -> payment_read or admin_user
PaymentWrite  -> payment_write or admin_user
AdminOnly     -> admin_user
```

---

## 7. Add Current User Service

Create or adapt:

```text
Services/ICurrentUserService.cs
Services/CurrentUserService.cs
```

The service must read:

```text
sub
preferred_username
email
roles
```

The `sub` claim is the Keycloak user id and must be used to map to the local Identity user.

---

## 8. Update Identity Module User Mapping

In the Identity module, keep ASP.NET Identity.

Add or suggest:

```csharp
public string? KeycloakSubjectId { get; private set; }
```

The local user mapping rule is:

```text
Keycloak access token sub claim -> UserEntity.KeycloakSubjectId
```

If local user is not found:

```text
Use controlled provisioning as the preferred first-phase approach.
```

Controlled provisioning means:

```text
After a successful Keycloak login, the Identity API creates the local application user/profile if it does not exist yet.
```

---

## 9. Swagger

Add Bearer token support to Swagger for each API.

The Swagger UI must allow calling protected endpoints with:

```text
Authorization: Bearer {access_token}
```

OAuth2 Authorization Code Flow with PKCE can be added later, but first phase Bearer support is enough.

---

## 10. Documentation

Create:

```text
docs/keycloak/KEYCLOAK_LOCAL_DEVELOPMENT.md
```

It must explain:

```text
- How to run Docker Compose
- How realm import works
- How to access Keycloak admin console
- How to validate clients
- How to validate users
- How to get a token
- How to test APIs
- How to reset local Keycloak data if realm import changes are not applied
```

---

## 11. Implementation Rules

```text
- Do not remove existing ASP.NET Identity usage.
- Do not hardcode secrets.
- Use environment variables.
- Keep the existing module structure.
- Prefer existing extension patterns if available.
- Do not introduce unnecessary refactoring.
- Do not change unrelated services.
```
