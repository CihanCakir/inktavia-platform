# REPORT — ADMIN OTP HANDOFF, Kickoff 2d (admin-panel token audience)

**Status: PASS.** The `admin-panel` access token now carries `aud: admin-panel-bff`, and bff-adminpanel accepts it
(200 where it previously 401'd). This closes the admin OTP login end-to-end (FE handoff + BFF authorization).

## Scope

Keycloak/infra only — `infrastructure/keycloak/init.sh` + the running realm. admin-web, bff-adminpanel, Identity,
provider-web, and CargoDry were **not** touched.

## Change (init.sh)

Added an idempotent step to the admin block, right after the `admin-panel` client fix (where the client uuid is
already resolved). If a protocol mapper named `audience-admin-panel-bff` is not already on the client, it creates an
`oidc-audience-mapper` that adds `admin-panel-bff` to the **access token** audience (mirrors the provider
`provider-portal` → `provider-portal-bff` pattern):

```
HAS_AUD_MAPPER=$($KCADM get "clients/${ADMIN_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} 2>/dev/null | grep -c '"audience-admin-panel-bff"' || true)
if [ "$HAS_AUD_MAPPER" -eq 0 ]; then
  $KCADM create "clients/${ADMIN_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} \
    -s name=audience-admin-panel-bff \
    -s protocol=openid-connect \
    -s protocolMapper=oidc-audience-mapper \
    -s 'config."included.client.audience"=admin-panel-bff' \
    -s 'config."id.token.claim"=false' \
    -s 'config."access.token.claim"=true'
fi
```

Applied via `docker compose up keycloak-init` (no realm re-import, no service rebuilds). Init logged:
`admin-panel: added audience mapper (access token aud += admin-panel-bff).` A second run logs
`… already present` (idempotent).

**Decision on the optional cleanup (step 3):** left the stale `notification-api`/`messaging-api`/`cargodry-api`
audience mappers in place. Adding `admin-panel-bff` is sufficient (.NET passes when ANY `aud` entry matches
`ValidAudience`), and the acceptance criteria explicitly allow the other entries to remain — so removal was skipped
to keep the change minimal and risk-free, per the doc's guidance.

## Verification (fresh login token, masked)

Full OTP → ticket → Keycloak authorize (login_ticket + PKCE, redirect_uri `http://localhost:3001/auth/callback`) →
token exchange, then decoded the access token:

- **Before:** `aud = ["notification-api","messaging-api","cargodry-api"]` → bff-adminpanel 401 (audience mismatch).
- **After:** `aud = ["admin-panel-bff","notification-api","messaging-api","cargodry-api"]`, `azp = admin-panel`,
  `realm_access.roles ⊇ { Admin }`. ✅ contains `admin-panel-bff`.

BFF calls with that Bearer token:
- `GET /api/v1/admin-panel/dashboard/overview` → **HTTP 200** (previously 401). ✅
- Control, no token → **HTTP 401** (auth still enforced). ✅
- Two guessed paths (`notifications/unread-count`, `users?...`) returned **404, not 401** — which further confirms the
  token now clears authentication and reaches routing (a bad audience would 401 upstream of routing). The
  `AdminPanelAccess` policy admits the `Admin` realm role already carried by the token.

(Note: a dedicated non-admin → 403 probe was not run — the OTP path only provisions admins, so minting a non-admin
`admin-panel` token isn't straightforward here. The 200/404-not-401 results already demonstrate the authentication
layer now passes and the policy admits the Admin role.)

## Regression

Provider realm/clients untouched. `admin-panel-bff` service-account audience mappers (the API audiences) untouched.
`.env` not committed.
