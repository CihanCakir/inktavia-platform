# Fix — Restore registration: marine-mobile-bff SA realm-management roles + durable

Last open auth item after `REPORT_FIX_S2S_403_RESTORE.md`. Login/OTP were fixed by restoring the
SA's `identity_read`/`identity_write`; **registration / Keycloak user provisioning still 403'd**
because the same recreate wiped the SA's `realm-management` client roles. This closes it and makes
both role sets survive future recreates. No FE / mock / business-logic changes.

Env: `aizen-inktavia-local` compose. `localhost:17003` (bff-marine-mobile), keycloak 25.0.0.
Date: 2026-08-07.

---

## Root cause

`service-account-marine-mobile-bff` had **no** `realm-management` client roles
(`manage-users / view-users / query-users / view-realm`). The **BFF itself** provisions the Keycloak
user during register (`MarineMobileKeycloakAdminClient`, using the same in-process-cached SA token as
login), so with those roles missing the very first admin call —
`GET /admin/.../users?email=…` (`FindUserByEmailAsync`) — returned **403**, failing register before
the user was created. Baseline confirmed:

```
POST /api/v1/mobile/auth/register → errorCode 911 / "403 (Forbidden)"
bff log: HttpRequestException 403 at MarineMobileKeycloakAdminClient.FindUserByEmailAsync (line 64)
kcadm get-roles --uid <sa> --cclientid realm-management → []   (empty)
```

Which service creates the KC user? **bff-marine-mobile** (not identity-api — that uses its own
`provider-portal-bff` admin client). So only the BFF token needed busting.

---

## Step 1 — Grant the roles (least-privilege, matches init.sh 291–294)

SA id resolved fresh (not hardcoded): client `marine-mobile-bff` = `6952ee91-…` →
service-account user = `7fd65ff6-c16b-4d63-8b8d-d9ef9ce8f8cf`.

```bash
docker exec keycloak /opt/keycloak/bin/kcadm.sh add-roles -r inktavia-realm \
  --uid 7fd65ff6-c16b-4d63-8b8d-d9ef9ce8f8cf --cclientid realm-management \
  --rolename manage-users --rolename view-users --rolename query-users --rolename view-realm
```
Confirmed attached: `['manage-users', 'query-users', 'view-realm', 'view-users']`.

## Step 2 — Bust the cached token

The SA token is cached in the BFF's in-process `IMemoryCache` (`ParticipantKeycloakServiceTokenProvider`),
so a token minted before the grant lacks the new roles → `docker compose restart bff-marine-mobile`.
(identity-api needs no restart — it does not mint the KC user-create token; the BFF does.)

## Step 3 — Registration end-to-end ✅

```
POST /api/v1/mobile/auth/register  {email: reg.test.<ts>@inktavia.com, password: RegTest2026!!, …}
  → isSuccess=true, accessToken minted (len 1751)
GET /api/v1/mobile/profile/me (bearer) → HTTP 200, profileId 100032, email = new user
kcadm get users -q email=<new> → returned (enabled:true, emailVerified:true)   ← created in Keycloak
kcadm get-roles --uid <sa> --cclientid realm-management → [manage-users, query-users, view-realm, view-users]
403 count during register → bff-marine-mobile=0, identity-api=0, keycloak=0
```

---

## Step 4 — Durable fix (chosen: bake into the realm import)

**Approach: bake the SA role mappings into `infrastructure/keycloak/inktavia-realm-realm.json`** so a
fresh `docker compose up` (`keycloak … --import-realm`) restores them automatically — no dependence
on the `keycloak-init`/`init.sh` timing that had been letting them slip. Mirrors the existing
`service-account-admin-panel-bff` pattern already in that file. The realm JSON had **no**
`service-account-marine-mobile-bff` user entry at all (the gap); added (17 lines):

```json
{
  "username": "service-account-marine-mobile-bff",
  "enabled": true,
  "serviceAccountClientId": "marine-mobile-bff",
  "realmRoles": ["identity_read", "identity_write"],
  "clientRoles": { "realm-management": ["manage-users","view-users","query-users","view-realm"] }
}
```

This covers **both** the login roles (`identity_*`) and the registration roles (`realm-management`)
in one place, so a fresh import needs no manual grant.

### Recreate test (isolated, non-destructive) ✅

Wiping the live `kc_pg_data` would delete runtime-registered users (qa.owner.aug5, etc.), so instead
the import was proven in a throwaway Keycloak (same image, same file + SPI jar, ephemeral H2 DB):

```
docker run … quay.io/keycloak/keycloak:25.0.0 start-dev --import-realm  (mounts the updated JSON)
  → "Realm 'inktavia-realm' imported" / "Import finished successfully"
fresh-import SA realm roles           → ['identity_read', 'identity_write']
fresh-import SA realm-management roles → ['manage-users','query-users','view-realm','view-users']
```

i.e. a fresh import alone (no init.sh, no manual step) grants the SA everything login **and**
registration need. Throwaway removed; live env re-smoke-tested green (login `/me` 200 + register
isSuccess). `python3 -m json.tool` confirms the JSON is well-formed (5 users).

> Note: `--import-realm` uses `IGNORE_EXISTING`, so it only applies when the realm is (re)created
> from a fresh `kc_pg_data` — exactly the recreate case that dropped the roles. For the belt-and-
> suspenders case (realm persists but roles somehow dropped), the existing idempotent `keycloak-init`
> → `init.sh` still re-grants both sets by `--uid` on every `up`. No init.sh change needed.

---

## Files changed

- `infrastructure/keycloak/inktavia-realm-realm.json` — +17 lines (baked-in `service-account-marine-mobile-bff` role mappings). No code/FE/mock changes.

Runtime grant + BFF restart applied to the live env (register works now); the JSON change makes it durable across future recreates.
