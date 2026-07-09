# Keycloak Provider Realm Setup — Phase 31C

Setup artifacts live in `infrastructure/keycloak/provider-realm/`:
- `setup-provider-realm.sh` — idempotent `kcadm.sh` script (preferred).
- `provider-portal-clients.json` — partial import (clients + roles + Google IdP).
- `README.md`, `provider-portal-realm-setup.md` — run instructions + token expectations.

## Executed or prepared?
**Prepared + statically validated, not executed.** Keycloak was not reachable from the authoring environment.
Static validation passed: `setup-provider-realm.sh` → `bash -n` OK; `provider-portal-clients.json` → valid JSON.
Run the script (or partial import + manual grants) against the target Keycloak locally/in the cluster.

## Covers
1. `provider-portal` public SPA (Auth Code + PKCE, redirect/web-origins for local/dev/test/prod).
2. `provider-portal-bff` confidential client + service account.
3. Realm roles `provider_pending`, `provider_user`, `provider_restricted`.
4. `provider_profile_id` user-attribute → claim mapper.
5. Audience mapper (provider-portal tokens include `provider-portal-bff`).
6. Google identity provider (placeholder secrets).
7. Verify Email required action + Forgot Password enabled.
8. Service-account roles: realm-management `view-users`/`query-users`/`manage-users`; identity-api `identity_read`/`identity_write`.

## Required env vars / secrets (never committed)
`KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET`, `GOOGLE_OAUTH_CLIENT_ID`, `GOOGLE_OAUTH_CLIENT_SECRET`,
`PROVIDER_WEB_BASE`. The BFF secret also feeds `MarineProviderKeycloak:AdminClientSecret` (BFF) and
`IdentityKeycloak:AdminClientSecret` (Identity role sync).

## Expected provider access token
```json
{ "aud": ["provider-portal-bff"], "realm_access": { "roles": ["provider_pending"] }, "provider_profile_id": "12345" }
```
- `provider_profile_id` is a string (BFF parses long); present only after provisioning + attribute write.
- Role changes need token refresh/re-login to appear; **runtime Identity status is authoritative each request**.

## 31C runtime smoke checklist
Register → provision + attribute + verify-email · Google ensure-profile link · `/provider/me/status` pending
→ approve (role flips) → active · phone OTP send/verify persists. See `provider-portal-realm-setup.md` §5.
