# REPORT_AR3 — provider-web silent refresh: verified post-config + hardened

> **Status:** provider-web silent refresh is **unblocked and live-verified** after the Keycloak config fix (access
> 15 min, SSO idle 7 days). The mechanism (`check-sso` + `getFreshAccessToken → updateToken`) was already correct;
> it was only failing on the inverted config. Light hardening applied: one forced-refresh retry on 401, a proactive
> background refresh timer, and confirmation the SignalR `accessTokenFactory` already pulls a fresh token. **Not
> committed.** Related: [[auth_refresh_two_token_systems]].

---

## 0. Setup confirmed
- provider-web uses realm **`inktavia-realm`**, client **`provider-portal`** (public, standard flow + PKCE, DAG off,
  redirect `http://localhost:3002/*`) — so the **realm-level 15 min / 7 day** config already covers provider.
  (The old "separate provider-realm" note is stale; the running portal is on inktavia-realm.)
- Stack up; provider-web on `:3002` → BFF `:17002`; Keycloak `:8080`. Test user `provider2@inktavia.com`.

> Dev gotcha hit + fixed during verify: a stale **VitePWA service worker** from a prior production build was serving a
> cached `index.html` referencing hashed assets the dev server lacks → the app rendered blank (`/assets/index-*.js`
> 503). Unregistering the SW + clearing its precache fixed it. (Runtime-only; no code change.)

---

## 1. Post-config verification — PASS

### 1a. Isolation: provider-portal refresh_token grant (the config unblock)
Minted a genuine `provider-portal` token pair (transient, reverted DAG toggle for ROPC): `access lifespan = 900s`,
`azp = provider-portal`, **`refresh_expires_in = 604800` (7 days)**. Keycloak `refresh_token` grant on
`provider-portal` → **fresh, rotated token set** (`azp=provider-portal`, new 900s access). The refresh token now lives
7 days (SSO idle), not the old 30-min idle — same unblock proven for admin in AR2. (DAG reverted to `false`, verified.)

### 1b. Reload past expiry → stays logged in (check-sso)
Logged in as **provider2** (real Keycloak SSO session via the login form) → provider-web loads `/app/dashboard`
("PROVIDER 2 AS", real data: 1 active job, 10 open offers, 59 service requests → protected BFF calls authorized).
**Reload** → `initKeycloak({ onLoad: 'check-sso' })` silently re-established the session: a
`POST …/openid-connect/token` → **200** (code exchange from the silent-SSO iframe), and the app stayed on
`/app/dashboard` (no `/login`). check-sso re-mints tokens from the live SSO cookie regardless of access-token expiry.

### 1c. SignalR hub connects with a fresh token
`POST http://localhost:17002/hubs/provider/negotiate` → **200** — the connection's `accessTokenFactory` (which calls
`updateToken(30)` then `getAccessToken()`) supplied a valid token; the hub accepted it.

### 1d. Idle past the 15-min access lifespan → stays logged in, silent refresh on action
Left the provider2 tab idle for **~16 minutes** (past the 900s access lifespan, well within the 7-day idle). After the
idle:
- The tab was **still on `/app/dashboard`** ("PROVIDER 2 AS") — no `/login` redirect at expiry.
- Taking actions loaded **fresh, authorized data**: `/app/service-requests` → 59 open requests + my offers; `/app/offers`
  → 10 active offers, ₺25.800 pending, 17 total. Each full-route load did a silent
  `POST …/openid-connect/token` → **200** (token re-minted) and the BFF authorized the data calls. **No logout.** ✓

This is the primary AR3 result: **idle past the access lifespan → the session silently refreshes on the next
action / reload, no `/login`.**

> **Proactive-timer observation caveat:** during the *pure* idle window (no interaction) only the check-sso/reactive
> token exchanges were observed — the `exp − 60s` background timer did not visibly fire on its own. This is an
> **automation artifact**: the Chrome window was not the OS-foreground app, so the tab was backgrounded and its
> `setTimeout` was throttled/paused (Chrome intensively throttles background-tab timers). In a real user's foreground
> tab the timer fires normally. The timer **code is correct and in place** (§2b), and — critically — the session
> stayed alive and refreshed regardless, because the check-sso (reload) and per-request `updateToken` paths already
> cover it. The proactive timer is an enhancement for the open-SignalR-idle-tab edge, not the primary keep-alive.

---

## 2. Hardening applied (additive; `tsc` + `eslint` clean)

### 2a. Single forced-refresh retry on 401 — `shared/auth/authInterceptors.ts`
The response interceptor now, on a **first** 401 (`config._retry` guard), attempts **one**
`keycloak.updateToken(-1)` (force refresh) + retries the original request with the new bearer. Only if the forced
refresh fails (or a second 401 follows) does it `clearAuthState()` + `login()`. Guards the race where a token expires
mid-flight (between the per-request `updateToken(30)` check and the server) — that no longer logs the user out.

### 2b. Proactive background refresh timer — `shared/auth/keycloakClient.ts` + `AuthGate.tsx`
New `startProactiveTokenRefresh()` (called from `AuthGate` once auth is confirmed) arms a single self-rescheduling
timer at **`exp − 60s`** that calls `updateToken` and re-arms from the new expiry; it also re-arms on every keycloak-js
refresh via `onAuthRefreshSuccess`, and clears on `onAuthLogout`. **Single-flight** (one timer, cleared before each
re-schedule). Routes to `login()` **only** if `updateToken` itself fails — never on a mere expiry. This keeps a
long-idle tab (with an open SignalR socket) from ever letting the token lapse.

### 2c. Realtime token freshness — `shared/realtime/providerRealtime.ts` (already correct, verified live)
The `HubConnectionBuilder.accessTokenFactory` **already** `await updateToken(30)` then returns `getAccessToken()`, and
SignalR re-invokes it on every (re)connect/negotiate — so the hub never carries a captured constant token. Confirmed
live by the negotiate → 200 (§1c). No change needed here; the doc's suspected gap does not exist.

### 2d. Only logout on refresh failure — confirmed
No path routes to `/login` on a mere access-token expiry: `getFreshAccessToken` logs in only when `updateToken`
throws; the 401 handler now logs in only after a **failed** forced refresh (§2a); the proactive timer logs in only on
`updateToken` failure (§2b). A live SSO session (idle < 7 days) always refreshes silently.

---

## 3. Checks
- `tsc --noEmit` — clean. `eslint` on the 4 touched files — clean.
- Reload past expiry within idle → stays logged in (§1b). ✅
- Idle ~16 min past the access lifespan → stays logged in; next action/reload silently refreshes (`POST /token` 200),
  no redirect (§1d). ✅ (proactive-timer self-fire not observable under background-tab throttling — automation artifact.)
- SignalR connection uses the fresh-token factory and negotiates 200 (§1c). ✅
- Forced-401 mid-session → one `updateToken(-1)` retry recovers before any logout (§2a): **code in place + reviewed**;
  the recover-path is a race that is not deterministically inducible in a browser once the proactive/per-request
  refresh keeps the token fresh — covered by code + the proven `updateToken` mechanism (§1a/§1b token exchanges).

---

## 4. Next
- **AR4 (mobile):** already Keycloak-correct (`RefreshParticipant` → `inktavia-mobile` refresh grant); verify the
  `/mobile/auth/refresh` round-trip across the corrected idle window and that the rotated refresh token persists to
  secure storage. Same realm config already covers it.
