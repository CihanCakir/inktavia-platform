# REPORT_CONFIG_KEYCLOAK — session lifespans applied + admin client reconciled → AR2 silent refresh LIVE-VERIFIED

> **Status:** DONE and **live-proven end-to-end.** The running Keycloak realm now carries the owner-approved
> lifespans; the `admin-panel` public client is enabled with standard flow; the stray `admin-panel-web` client id is
> reconciled to `admin-panel` across SPA/BFF/realm; the refresh-grant isolation re-test returns a fresh token set; and
> admin-web silent refresh works in a real browser (expired-access reload stays logged in; only a dead refresh token
> routes to `/login`). **Not committed.** Related: [[auth_refresh_two_token_systems]].

---

## 1. Session lifespans — applied to the RUNNING realm

Applied via the Keycloak Admin REST API (`PUT /admin/realms/inktavia-realm`, fetch-patch-put) against the running
container, then **read back** to confirm. The committed `infrastructure/keycloak/inktavia-realm-realm.json` already
carried the same values, so fresh imports stay in sync.

| Setting | Before (AR1) | **Running now** | Meaning |
|---|---|---|---|
| `accessTokenLifespan` | 3600 (1h) | **900** | 15 min — short; silent refresh covers the gap |
| `ssoSessionIdleTimeout` | 1800 (30m) | **604800** | 7 days — now **far exceeds** the access lifespan |
| `ssoSessionMaxLifespan` | 36000 (10h) | **2592000** | 30 days — hard re-login ceiling |
| `clientSessionIdleTimeout` | 0 | **0** | unset — does not undercut the realm idle |
| `clientSessionMaxLifespan` | 0 | **0** | unset — does not undercut the realm max |
| `revokeRefreshToken` | false | **false** | rotation-safe (clients persist the rotated RT) |

**The inversion is gone:** idle (7 days) ≫ access (15 min). A refresh token now lives as long as the SSO session
(7-day idle / 30-day max), instead of dying at the old 30-min idle before the access token even expired.

### admin-panel client (running realm — read back)
`enabled: true`, `publicClient: true`, `standardFlowEnabled: true`, `directAccessGrantsEnabled: false` (no ROPC).
Redirect URIs `http://localhost:3000/*`, `http://localhost:3001/*`, `https://admin.inktavia.com/*`; web origins
`http://localhost:3000`, `http://localhost:3001`, `https://admin.inktavia.com`. **The admin-web dev origin
(`http://localhost:3000`) is covered.** (Client was already enabled in the running realm from a prior session; verified,
not re-toggled.)

---

## 2. Admin client-id reconciliation → everything names `admin-panel`

| Actor | Client id used | Source |
|---|---|---|
| SPA login handoff | **`admin-panel`** | `admin-web` `VITE_KEYCLOAK_CLIENT_ID` → `keycloakClient.ts` `new Keycloak({ clientId })` |
| BFF `RefreshBff` refresh_token grant | **`admin-panel`** | `AdminPanelKeycloak:RefreshClientId` (default) |
| Realm enabled public client | **`admin-panel`** | running realm (§1) |

**Finding:** `AdminPanelKeycloak:AdminPanelClientId = "admin-panel-web"` (a client that does **not** exist in the realm)
is **genuinely unused by BFF code** — inbound auth (`AuthenticationExtensions.cs`) validates the audience from
`AdminPanelKeycloak:Audience` → falls back to `AdminPanelBffClientId` (`admin-panel-bff`, the resource-server audience),
never `AdminPanelClientId`. It is a dead key.

**Action:** to remove the landmine, reconciled the stray value `admin-panel-web` → `admin-panel` in the three config
locations (`appsettings.json`, `appsettings.Local.json`, `docker-compose.yaml`) and updated the code comment in
`AdminPanelKeycloakOptions.cs`. No behavior change (the key is unused); the config is now internally consistent. All
three actors that matter already agreed on `admin-panel`.

---

## 3. Refresh-grant isolation re-test — PASS

Minted a **genuine `admin-panel`** token pair (via a **transient**, immediately-reverted `directAccessGrantsEnabled`
toggle for ROPC — the running client ends at DAG=`false`, confirmed), then exercised the refresh grant.

**Token issuance (proves the new realm values apply):**
`access lifespan = 900s`, `azp = admin-panel`, `aud = [admin-panel-bff, …]`, **`refresh_expires_in = 604800` (7 days)**.
→ The refresh token's life is now the 7-day SSO idle, not the old 30-min idle — this is the direct config unblock.

**Keycloak `refresh_token` grant** (`POST …/token grant_type=refresh_token&client_id=admin-panel&refresh_token=…`):
returns a **FRESH token set** (new `azp=admin-panel`, new 900s access, **rotated** refresh token). ✓

**Wait-past-15-min variant** (literal): minted a fresh RT, waited **> 900s** (past the access lifespan, within the
7-day idle), then ran the grant → **fresh token set returned**. Result (real wall-clock):
```
T0 = 07:41:37 UTC  (mint RT)
T1 = 07:57:17 UTC  (refresh)   → elapsed 15m40s, i.e. PAST the 900s (15 min) access lifespan
PAST-15MIN REFRESH SUCCEEDED ✓  azp: admin-panel | fresh access exp: 900s | rotated RT: true | refresh_expires_in: 604800
```
Under the OLD config (idle 1800s < access 3600s) this exact class of refresh — attempted after the access token had
expired — failed (`invalid_grant`, session idle-dead). It now succeeds because idle (7 days) ≫ access (15 min). ✓

**Bonus — AR2 code path proven at the same time:** `POST http://localhost:17001/api/v1/admin-panel/auth/refresh`
(the repointed `RefreshBff` → new `AdminKeycloakAuthClient` → Keycloak) → **HTTP 200**, `header.isSuccess=true`, body
carries a fresh **RS256 Keycloak** access token (`azp=admin-panel`, 900s). This is the exact failure that AR1/AR2
diagnosed — previously `TokenNotFound` (Identity `UserLoginTokenEntity` lookup) — now succeeding against Keycloak.

---

## 4. Live AR2 admin silent refresh (real browser) — PASS

admin-web dev server on `http://localhost:3000`, proxying `/api` → BFF `:17001`; real Keycloak `:8080`. A genuine
`admin-panel` session (real access+refresh token pair) was loaded into the SPA's persisted store. Evidence:

| Scenario | Action | Result |
|---|---|---|
| **Baseline** | Load with valid session | ✅ `/app/dashboard` renders, logged in as "Admin User"; protected widgets populated (14 vessels, payouts, etc.) → API calls authorized |
| **Access expired + valid RT → reload** | Backdate `accessExpiresAt` to the past (client treats access as expired), keep the real refresh token, reload | ✅ **`POST /api/v1/admin-panel/auth/refresh` → 200**; stays on `/app/dashboard` (NO `/login`); store `accessExpiresAt` refreshed to **+15 min** (silent refresh happened) |
| **Dead refresh token** | Corrupt the RT + expire access, reload | ✅ `POST …/auth/refresh` → **400** (RefreshTokenTimeOut); routes to `/login` **exactly once**; `clearAuth` cleared access/refresh/user |

> "Access expired" is induced by backdating the client's `accessExpiresAt` marker — the exact signal the SPA uses to
> decide the access token is stale — while the refresh token used is a **real, valid** one. The refresh that followed
> was fully real end-to-end (real RT → real BFF → real Keycloak → real fresh RS256 token, network 200). The literal
> wall-clock "past 15 min" case is covered by the §3 wait-past-15-min isolation test.

**Before → after:** was **`TokenNotFound` → forced logout at ~1h** (admin refresh hit Identity's store with a Keycloak
token); now **silent refresh keeps the session**, and only a genuinely dead/absent refresh token (or explicit logout)
routes to `/login`, once.

### Root-cause checklist (Step 5) — all green
- Running realm has the new lifespans (idle 7d ≫ access 15m) — §1, read back.
- `admin-panel` client enabled + standard flow on, DAG off — §1, read back.
- BFF `RefreshBff` hits **Keycloak** (fresh RS256 `azp=admin-panel` token), **not** Identity `RefreshLogin` — §3 bonus (200).
- SPA persists + sends the **rotated** refresh token; single-flight coordinator drives the refresh — §4 (200, stays in).

---

## 5. Test artifacts / cleanup notes (dev realm, no commit)
- `directAccessGrantsEnabled` on `admin-panel` was toggled **on→off transiently** only to mint ROPC token pairs for the
  isolation test; it is back to **`false`** (verified twice). Normal admin login is OTP→handoff (auth-code+PKCE), unaffected.
- A password was set on the dev user `admin.user@inktavia.com` to enable ROPC minting. It is **inert** for normal use
  (login is OTP-only; DAG is off), left in place in the dev realm.
- Config edits (working tree, **not committed**): `AdminPanelClientId` `admin-panel-web`→`admin-panel` in
  `appsettings.json`, `appsettings.Local.json`, `docker-compose.yaml`; comment in `AdminPanelKeycloakOptions.cs`.
  `bff-adminpanel` image rebuilt to include the AR2 code (was pre-AR2).

---

## 6. Unblocked next
This closes the **config prerequisite** for all three portals. **AR2 (admin) is now live-verified.** Next:
- **AR3 (provider):** provider-web uses a **separate `provider-realm`** (Keycloak adapter `updateToken`) — capture that
  realm's lifespans from the running server and apply the same rule (idle ≫ access); verify across the idle boundary.
- **AR4 (mobile):** already Keycloak-correct (`RefreshParticipant`); verify the `/mobile/auth/refresh` round-trip and
  that the rotated refresh token persists to secure storage.
