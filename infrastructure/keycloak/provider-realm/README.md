# Provider Realm Setup (Keycloak)

Artifacts to configure `provider-portal` + `provider-portal-bff` on the existing `inktavia-realm`.
**Prepared, not executed** (Keycloak was not reachable from the authoring environment). Run locally.

## Files
- `setup-provider-realm.sh` — idempotent `kcadm.sh` script (clients, roles, mappers, Google IdP, service-account roles, verify-email/forgot-password). Preferred.
- `provider-portal-clients.json` — partial-import for clients + roles + Google IdP (does **not** cover service-account role grants or realm login flags — use the script or Admin Console for those).
- `provider-portal-realm-setup.md` — detailed manual steps, env vars, and expected token contents.

## Quick start (script)
```bash
export KEYCLOAK_URL=http://localhost:8080
export KEYCLOAK_ADMIN=admin KEYCLOAK_ADMIN_PASSWORD=admin
export KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET=<generate-a-strong-secret>
export GOOGLE_OAUTH_CLIENT_ID=<google-client-id>
export GOOGLE_OAUTH_CLIENT_SECRET=<google-client-secret>
export PROVIDER_WEB_BASE=http://localhost:3002
bash setup-provider-realm.sh
```

## Quick start (partial import)
```bash
# replace ${...} placeholders first (do NOT commit secrets)
kcadm.sh config credentials --server "$KEYCLOAK_URL" --realm master --user "$KEYCLOAK_ADMIN" --password "$KEYCLOAK_ADMIN_PASSWORD"
kcadm.sh create partialImport -r inktavia-realm -f provider-portal-clients.json
# then grant service-account roles + enable verify-email/forgot-password (see setup md)
```

## Secrets (env only, never committed)
`${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}`, `${GOOGLE_OAUTH_CLIENT_ID}`, `${GOOGLE_OAUTH_CLIENT_SECRET}`.
These also feed the BFF (`MarineProviderKeycloak:AdminClientSecret`) and the Identity role-sync
(`IdentityKeycloak:AdminClientSecret`).
