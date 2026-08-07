# Fix — S2S 403: marine-mobile-bff → identity-api after container recreate

Infra/config regression (not app code). All `bff-marine-mobile` → `identity-api` service-to-service
calls returned **403 Forbidden**, so both email/password and OTP login failed before any screen
rendered. Fixed by re-granting the BFF service account's lost Keycloak roles + flushing the BFF's
in-memory service-token cache. No FE / business-logic / mock changes.

Env: `aizen-inktavia-local` compose. `localhost:17003` (bff-marine-mobile), identity-api, keycloak.
User: `qa.owner.aug5@inktavia.com` / `QaReset2026!!`. Date: 2026-08-07.

---

## Root cause — Step 1 (service-account role wiped by the recreate)

The Keycloak service account `service-account-marine-mobile-bff` had **only**
`default-roles-inktavia-realm` — it lost `identity_read` + `identity_write` in the container
recreate. The BFF still mints a valid service token (so the call is *authenticated* → **403**, not
401), but the token carries no `identity_*` role, so identity-api's authorization rejects every
BFF→Identity call (`MintParticipantLoginTicket`, `RequestParticipantOtpLogin`).

**Steps 2 & 3 were ruled out** (checked first, both healthy):
- **Step 2 (allow-list):** `identity-api` env has `BffAssertion__AllowedClientIds__2=marine-mobile-bff`
  and `BffAssertion__SharedSecret` == the BFF's `MarineMobileKeycloak__ModuleAssertionSecret`
  (`1e59347d…`). Assertion passes.
- **Step 3 (azp/secret):** the token mint **succeeds** (403, not 401), and the BFF mints as
  `AdminClientId=marine-mobile-bff` → azp matches the allow-list. Secret is valid.

> Step 0 note: `identity-api` does **not** log auth-layer 403/401 (nothing at info level for a
> rejected call — direct `GET /health` also returns 401 silently). The 403 is only visible in the
> **bff-marine-mobile** log (`Refit.ApiException: … 403 (Forbidden)` from `IIdentityRemoteCall`).
> The role gap was confirmed authoritatively via `kcadm` role-mappings, not the log line.

Canonical source of truth: `infrastructure/keycloak/init.sh` (lines 278–285) grants exactly
`identity_read` + `identity_write` to this SA via `--uid` (the `--uusername service-account-<client>`
form silently no-ops in this Keycloak image — see init.sh:272–274).

---

## Remediation (one line)

Re-grant the identity roles to the SA **by `--uid`** (not `--uusername`), then restart the BFF to
flush its in-memory service-token cache:

```bash
SAID=$(docker exec keycloak /opt/keycloak/bin/kcadm.sh get \
  clients/6952ee91-ac04-4778-be01-44c2f2604a3d/service-account-user -r inktavia-realm --fields id \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['id'])")   # 7fd65ff6-…

docker exec keycloak /opt/keycloak/bin/kcadm.sh add-roles -r inktavia-realm \
  --uid "$SAID" --rolename identity_read --rolename identity_write

docker compose restart bff-marine-mobile
```

(kcadm login used: `config credentials --server http://localhost:8080 --realm master --user admin --password admin`.)

**Why the restart matters:** the token is cached in **in-process `IMemoryCache`**
(`ParticipantKeycloakServiceTokenProvider`, key `marine-mobile-bff:keycloak-service-token`,
TTL = `expires_in − 60s`) — *not* Redis. Before the restart the BFF kept serving the old role-less
token and still 403'd; the restart forces a fresh token carrying the new roles. (No Redis flush
needed for this BFF.)

Confirmed attached after the grant:
```
realm roles: ['default-roles-inktavia-realm', 'identity_read', 'identity_write']
```

---

## Verification (all pass)

| Check | Result |
|---|---|
| **A** — S2S-backed identity read (`GET /api/v1/mobile/profile/me` w/ bearer) | **HTTP 200**, returns the real user (profileId 100030, `QA Owner Aug5`, email, phone, a resolved presigned `avatarUrl`) |
| **B** — email/password login end-to-end | `isSuccess=true`, accessToken minted (len 1749); `/me` → 200 |
| **C** — OTP login end-to-end | send → `[DEV-ONLY] OTP login code … : 531128` in identity-api log → verify (`loginRequestId`+`otpCode`) → `isSuccess=true`, accessToken minted |
| **D** — no 403 during a full login | **0** `403`/`Forbidden` in both `identity-api` and `bff-marine-mobile` logs over the login window |

(Side note: the resolved `avatarUrl` in check A points at the known 159-byte 1×1 artifact from
`REPORT_FIX_AVATAR_RENDER_DOC_PICKER.md` — runtime-confirming that the avatar read path is healthy
and the earlier "black circle" was corrupt stored bytes, not a URL/resolution bug.)

---

## Follow-up NOT applied (needs your approval — same root cause, different flow)

The recreate also wiped this SA's **realm-management** client roles (currently `NONE`). init.sh
(lines 291–294) grants `manage-users / view-users / query-users / view-realm` — needed for
**registration / Keycloak user provisioning**, not for login. Login is fully fixed without them, so
per "stop at the first fix" I left them. Registration will 403 until they're restored. To restore
(matches init.sh, least-privilege):

```bash
docker exec keycloak /opt/keycloak/bin/kcadm.sh add-roles -r inktavia-realm \
  --uid 7fd65ff6-c16b-4d63-8b8d-d9ef9ce8f8cf --cclientid realm-management \
  --rolename manage-users --rolename view-users --rolename query-users --rolename view-realm
```

## Durable fix (avoid the recurrence)

The clean way to avoid re-doing this after every recreate is to re-run `infrastructure/keycloak/init.sh`
as part of the compose bring-up (it is idempotent and grants both role sets by `--uid`), or bake the
grants into the realm import (`inktavia-realm-realm.json`) so a recreate restores them automatically.
