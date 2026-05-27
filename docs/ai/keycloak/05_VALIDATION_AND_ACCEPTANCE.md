# Keycloak Validation and Acceptance Criteria

## Required Generated Files

The implementation must create or update:

```text
docker-compose.yaml
infrastructure/keycloak/inktavia-realm-realm.json
docs/keycloak/KEYCLOAK_LOCAL_DEVELOPMENT.md
```

The implementation must add or adapt these files in the appropriate API projects:

```text
Extensions/KeycloakAuthenticationExtensions.cs
Extensions/AuthorizationPolicyExtensions.cs
Services/ICurrentUserService.cs
Services/CurrentUserService.cs
```

The implementation must update or suggest Identity user model/migration for:

```text
KeycloakSubjectId
```

## Docker Compose Validation

Run:

```bash
docker compose up -d
```

Expected:

```text
- keycloak-db starts.
- keycloak starts.
- Keycloak imports inktavia-realm.
- Keycloak is reachable at http://localhost:8080.
```

Admin credentials:

```text
Username: admin
Password: admin
```

## Realm Validation

In Keycloak admin console, verify:

```text
Realm: inktavia-realm
```

## Client Validation

API clients must exist:

```text
identity-api
payment-api
profile-api
```

Application clients must exist:

```text
inktavia-mobile
customer-panel
admin-panel
```

## Role Validation

Realm roles must exist:

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

## User Validation

Users must exist:

```text
mobile.user@inktavia.com
customer.user@inktavia.com
admin.user@inktavia.com
```

Password:

```text
Password123!
```

Temporary password:

```text
false
```

## Access Matrix Validation

### mobile.user@inktavia.com

Expected roles:

```text
mobile_user
profile_read
profile_write
```

Expected audiences:

```text
identity-api
profile-api
```

Must not have:

```text
payment-api
payment_read
payment_write
```

### customer.user@inktavia.com

Expected roles:

```text
customer_user
profile_read
payment_read
```

Expected audiences:

```text
identity-api
profile-api
payment-api
```

Must not have:

```text
payment_write
identity_write
profile_write
```

### admin.user@inktavia.com

Expected roles:

```text
admin_user
identity_read
identity_write
profile_read
profile_write
payment_read
payment_write
```

Expected audiences:

```text
identity-api
profile-api
payment-api
```

## API Token Validation

Each API must validate its own audience:

```text
Identity API -> identity-api
Payment API  -> payment-api
Profile API  -> profile-api
```

Expected behavior:

```text
- Token without required audience must be rejected.
- Token without required role must return forbidden.
- Token with correct audience and role must be accepted.
```

## Swagger Validation

Each API Swagger UI must allow:

```text
Authorization: Bearer {access_token}
```

## Local Development README

Create:

```text
docs/keycloak/KEYCLOAK_LOCAL_DEVELOPMENT.md
```

It must include:

```text
- Docker Compose run command
- Keycloak admin console URL
- Admin username/password
- Realm import explanation
- Client list
- Role list
- Test user list
- How to get token
- How to decode token
- How to test Identity API
- How to test Profile API
- How to test Payment API
- How to test forbidden cases
- How to reset local Keycloak data
```

## Realm Import Reset Note

Add this warning to README:

```text
Keycloak startup import may not overwrite an existing realm.
If you change infrastructure/keycloak/inktavia-realm-realm.json and the changes are not reflected, reset local volumes:

docker compose down -v
docker compose up -d

Warning: docker compose down -v removes local volumes. Use only in local development.
```

## Final Response Expected From Agent

After implementation, summarize:

```text
- Files created
- Files updated
- Keycloak clients created
- Roles created
- Users created
- API integration changes
- How to run
- How to validate
- Any assumptions made
```

## Acceptance Checklist

The implementation is acceptable when:

```text
[ ] docker compose up -d starts Keycloak and Keycloak DB.
[ ] inktavia-realm is imported automatically.
[ ] identity-api client exists.
[ ] payment-api client exists.
[ ] profile-api client exists.
[ ] inktavia-mobile client exists.
[ ] customer-panel client exists.
[ ] admin-panel client exists.
[ ] Realm roles exist.
[ ] Test users exist.
[ ] Application clients have correct audience mappers.
[ ] APIs validate JWT tokens.
[ ] APIs validate their own audience.
[ ] Role policies work.
[ ] Swagger Bearer authentication works.
[ ] Identity EF Core user structure is preserved.
[ ] Keycloak sub claim can be mapped to local user KeycloakSubjectId.
```
