# 02 — Fix BFF Inbound Auth Scheme and Admin Policy

Fix all failures caused by:

```text
No authenticationScheme was specified, and there was no DefaultChallengeScheme found.
```

Requirements:

1. Inspect `Program.cs`, startup/starter extensions, dependency injection, and existing Aizen auth conventions.
2. Register the correct default authentication/challenge scheme for AdminPanel BFF incoming Identity tokens.
3. Authenticate from `X-Aizen-User-Token`, not browser Keycloak token.
4. Add or fix `AdminPanelAccess` policy.
5. Apply policy to protected AdminPanel BFF controllers/actions.
6. Leave auth login/otp/refresh endpoints anonymous where appropriate.
7. Ensure unauthenticated requests return 401/403, not 500.

Generate:

```text
docs/reports/admin-panel-bff-auth-scheme-and-admin-policy-fix-report.md
```
