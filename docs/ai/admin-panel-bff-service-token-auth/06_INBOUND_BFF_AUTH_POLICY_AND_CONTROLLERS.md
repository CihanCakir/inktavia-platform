# 06 — Inbound BFF Auth Policy and Controllers

Refactor AdminPanel BFF inbound auth so browser requests can be authenticated by Identity token only.

Expected incoming browser header:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Remove or adjust any BFF-level requirement that forces browser requests to include:

```http
Authorization: Bearer <keycloakAccessToken>
```

Do not disable authorization globally.

Implement a clear Identity-token boundary according to existing Aizen conventions.

If the repository has a standard `IAizenInfoAccessor`, user token validator, or auth middleware, use it.

If a route is public auth route, it can allow anonymous access where appropriate:

```text
/auth/login/username
/auth/login/phone
/auth/login/otp
/auth/otp/send
/auth/otp/check
```

Protected routes must require valid Identity user context.

Generate or update:

```text
docs/reports/admin-panel-bff-auth-pipeline-fix-report.md
```
