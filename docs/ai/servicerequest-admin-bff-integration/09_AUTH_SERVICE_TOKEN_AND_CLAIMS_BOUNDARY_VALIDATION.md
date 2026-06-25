# 09 — Validate Auth, Service Token, and Claims Boundary

Validate that ServiceRequest module calls work with the established security model.

## BFF protected endpoint tests

- no token → 401
- participant/customer-only Identity token → 403
- admin Identity token → allowed

## BFF → ServiceRequest module tests

Verify BFF sends:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Verify ServiceRequest module accepts the BFF service token and user claims for admin routes.

## Common bug to avoid

Do not require the Keycloak service token itself to contain the `Admin` user role. User roles come from `X-Aizen-User-Token` and the Aizen info accessor / claims bridge.

## Output

Create `docs/reports/servicerequest-bff-auth-and-token-forwarding-report.md`.
