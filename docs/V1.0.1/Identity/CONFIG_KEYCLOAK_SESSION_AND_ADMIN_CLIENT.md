# CONFIG — Keycloak session lifespans + enable/reconcile the admin client (silent-refresh prerequisite)

> **Repo:** `addesso-project` — `infrastructure/keycloak/inktavia-realm-realm.json` (+ the running Keycloak realm).
> **This is the config prerequisite that unblocks silent refresh on ALL THREE portals** (admin, provider, mobile — all
> Keycloak-issued) and lets the AR2 fix be live-verified. Three unambiguous structural fixes; the exact **lifespan
> values are the owner/ops decision** (I recommend, don't invent). **Config only, no code. Do not commit.**

> **APPLIED to the realm JSON (2026-08-12, owner-approved values):** `infrastructure/keycloak/inktavia-realm-realm.json`
> now has `accessTokenLifespan: 900` (15 min), `ssoSessionIdleTimeout: 604800` (7 days), `ssoSessionMaxLifespan:
> 2592000` (30 days — raised from 10h so the hard ceiling doesn't undercut the 7-day idle), and the `admin-panel`
> client `enabled: true` + `standardFlowEnabled: true` (description corrected — it is NOT deprecated; it's the public
> SPA handoff + refresh_token client; `admin-panel-bff` is the separate confidential client_credentials service client).
> **Remaining in Claude Code:** (a) apply the same to the **running** realm (admin console or re-import) + restart
> Keycloak; (b) **reconcile the BFF `AdminPanelClientId` (`admin-panel-web`, absent) → `admin-panel`** so the SPA
> (`VITE_KEYCLOAK_CLIENT_ID=admin-panel`), the BFF handoff/refresh client id, and the realm all agree; (c) the
> refresh-grant isolation re-test + live proof below.

## Why this blocks everything
All three clients refresh against **Keycloak**. If Keycloak ends the session before the access token even expires, no
client-side refresh (AR2/AR3/AR4) can survive. AR1 found the realm is **inverted**, and AR2 found the admin client is
**disabled** and **mis-referenced**. Fix these first, then the client work is verifiable.

## Fix 1 — session lifespans (inverted: idle < access)
`inktavia-realm-realm.json` (confirmed):
- L13 `"accessTokenLifespan": 3600` (1h)
- L14 `"ssoSessionIdleTimeout": 1800` (30 min)  ← **shorter than the access token**
- L15 `"ssoSessionMaxLifespan": 36000` (10h)

The refresh token's life is bounded by **SSO Session Idle** (30 min) < the access token (1h), so the refresh token is
already idle-dead by the time the access token expires → forced logout. **Rule: `ssoSessionIdleTimeout` must be well
greater than `accessTokenLifespan`.**

**Recommended (owner sets the exact numbers per security posture):**
- `accessTokenLifespan`: **~900** (15 min) — short; silent refresh covers the gap. *(5–15 min are all reasonable.)*
- `ssoSessionIdleTimeout`: **long enough for good UX** — e.g. **604800** (7 days) or the product's chosen idle window;
  must exceed the access lifespan by a wide margin.
- `ssoSessionMaxLifespan`: a hard ceiling (e.g. **2592000** / 30 days, or per posture) — the absolute re-login cadence.
- If `clientSessionIdleTimeout`/`clientSessionMaxLifespan` are set on the clients, they must not undercut the realm
  idle/max. Leave `revokeRefreshToken` as-is; the clients (AR2–AR4) already persist the **rotated** refresh token, so
  rotation is safe.

> Trade-off to note for the owner: a longer idle window = fewer re-logins but a longer-lived session if a device is
> lost. Pick per the admin/provider/owner risk profile (admin could be tighter than owner-mobile).

## Fix 2 — enable the admin client (+ standard flow)
The client AR2's refresh + the admin login handoff target is **disabled**:
- L647 `"clientId": "admin-panel"`, L650 `"enabled": false`, L651 `"publicClient": true`, L652
  `"standardFlowEnabled": false`, L653 `"directAccessGrantsEnabled": false`.

A disabled client can't mint or refresh tokens, and with `standardFlowEnabled:false` it can't complete the
authorization-code **handoff** admin-web uses. Set:
- `"enabled": true`
- `"standardFlowEnabled": true` (the SPA login handoff = authorization code + PKCE)
- keep `"publicClient": true` (SPA; PKCE, no secret) and `"directAccessGrantsEnabled": false` (admin uses the handoff,
  not ROPC)
- ensure its **redirect URIs / web origins** cover the admin-web origin(s) (dev + deployed).

*(`admin-panel-bff` (L683, confidential, enabled) is the BFF's service client — separate concern; the **public**
`admin-panel` is the one the SPA + the refresh_token grant use.)*

## Fix 3 — reconcile the admin client id (config points at a non-existent client)
AR2 found `AdminPanelClientId = "admin-panel-web"` in the BFF config, but **no `admin-panel-web` client exists** in the
realm (only `admin-panel` + `admin-panel-bff`); `RefreshClientId` already defaults to the real `admin-panel`. Align so
the **SPA handoff client**, the **BFF `RefreshClientId`**, and the **realm client** all name the **same enabled public
client**:
- **Confirm** which client `admin-web/src/shared/auth/keycloakClient.ts` initializes (`new Keycloak({ clientId: … })`)
  — that is the SPA's actual login client and must match the realm + the BFF refresh client id.
- **Recommended:** standardize everything on the existing **`admin-panel`** public client (rename the stray
  `admin-panel-web` reference to `admin-panel`), rather than adding a new realm client. If a distinct `admin-panel-web`
  client is genuinely intended, add it to the realm (enabled, public, standard flow, correct redirect URIs) and point
  all three at it — but one client is simpler.
- After the change, the SPA login, the BFF `RefreshBff` (Keycloak `refresh_token` grant), and the realm must all agree.

## Apply + verify
- Apply to the **running realm** (Keycloak admin console or a realm re-import). Keep the committed
  `inktavia-realm-realm.json` in sync so fresh imports carry the fix (config file, no secrets). If the client is
  confidential anywhere, the secret stays in env/k8s — never in the SPA or the repo.
- **Isolation re-test (AR1 Step 2):** get a token pair, wait past the (new, short) access lifespan but within SSO idle,
  then `POST …/token grant_type=refresh_token&client_id=admin-panel&refresh_token=…` → **now returns a fresh token
  set** (was failing because idle < access). This proves the config unblock.
- **End-to-end (stack up):** admin/provider/mobile — leave idle past the access lifespan → next action **silently
  refreshes**, no login redirect; reload within the idle window → **stays logged in**; only past SSO idle/max (or a
  revoked refresh) → one clean re-login. Capture before/after.

## Don't-break / QA
- Config only: session lifespans + enable/flag the admin client + reconcile the client id. No code, no secrets
  committed. Widening idle is a security-posture choice — the owner sets the values; the **structure** (idle > access,
  client enabled, ids aligned) is unambiguous.
- Sanity: enabling `admin-panel` + standard flow must not open ROPC (keep directAccessGrants false); redirect URIs must
  be the real admin origins only (no wildcards to untrusted hosts).

## Report
`docs/V1.0.1/Identity/REPORT_CONFIG_KEYCLOAK.md`: the applied lifespans (chosen values + rationale), the admin-client
enable/flags, the client-id reconciliation (what the SPA/BFF/realm now all use), and the **refresh-grant isolation
re-test result** (fresh token past access expiry). This unblocks AR2 live verification and AR3 (provider) / AR4
(mobile). Cross-link [[auth_refresh_two_token_systems]].
