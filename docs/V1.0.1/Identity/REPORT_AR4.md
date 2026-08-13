# REPORT_AR4 — mobile (RN/Expo) silent refresh via RefreshParticipant

> **Status:** Wired end-to-end in the owner RN app and verified. Secure token storage, a single-flight refresh
> coordinator, proactive + foreground refresh, a reactive-401 interceptor, and a SignalR token factory that pulls a
> fresh token through the coordinator are all in place. `tsc --noEmit` clean; the **mobile BFF `/auth/refresh` path is
> live-proven** (200 + rotation + 401-on-bad-token). The on-device walkthrough is **manual** (an RN/Expo app can't be
> driven from this environment). **This closes the AR silent-refresh track across all three portals.** Not committed.
> Related: [[auth_refresh_two_token_systems]].

---

## 0. Approach
Per the brief, I grepped the app for its existing auth/session store, secure storage, api client, realtime edge, and
`AppState` usage **before** adding primitives — and reused them. The AR4 wiring already existed in the working tree
(uncommitted); I reviewed it against every requirement, confirmed correctness, fixed a stale comment, and validated it
(tsc + live BFF). No new fetch layer, no new storage primitive, no login/OTP/social flow change beyond storing the
tokens the coordinator needs.

---

## 1. What is wired (files)

| Requirement | Where | How |
|---|---|---|
| **Secure storage** | `core/auth/tokenStorage.ts` | `expo-secure-store` (Keychain/Keystore) for access **and** refresh (+ identity) tokens; `authStore.hydrate()` restores them on launch → a returning user within the 7-day idle stays signed in. **No plaintext AsyncStorage.** |
| **Single-flight coordinator** | `core/auth/refreshCoordinator.ts` | `refreshSession()` guarded by one `inFlight` promise latch — concurrent callers (timer, 401, foreground, SignalR) coalesce onto ONE `POST /auth/refresh`. |
| **Rotation persisted** | same, `doRefresh()` | On 200 it persists the **rotated** `refreshToken` + new `accessToken` + a fresh `expiresAt` (from the new JWT `exp`). |
| **Logout only on refresh failure** | same, `handleRefreshFailure()` | The ONLY logout path; idempotent (`isAuthenticated` guard → routes to login **exactly once**). A transient (offline/timeout/5xx) **preserves** the session; only a 401 / missing-token clears it. |
| **Proactive timer** | same, `scheduleProactiveRefresh()` | Fires at `exp − 60s` (the 60 s doubles as the clock-skew margin); re-armed by a `authStore.subscribe` on every token change. |
| **Foreground refresh** | same, `onAppStateChange()` | `AppState → 'active'` → refresh if within margin, else re-arm the timer from the real deadline (JS timers pause while backgrounded, so foreground is the mobile equivalent of provider-web's `onTokenExpired`). |
| **Reactive 401** | `core/api/apiInterceptors.ts` | On a non-auth-endpoint 401 (once, `_retried` guard) → `await refreshSession()` → retry with the new bearer; the coordinator owns logout, so the interceptor never double-routes. Wired into the **existing** axios client MO1–MO11 use. |
| **Realtime token factory** | `features/notifications/services/realtimeClient.ts` | SignalR `accessTokenFactory: async () => (await getValidAccessToken()) ?? ''` — `getValidAccessToken()` refreshes first if within margin, so a (re)connect past the 15-min lifespan rides a fresh token. |
| **Lifecycle start** | `app/providers/AppProviders.tsx` | `startTokenAutoRefresh()` runs **after** `hydrate()` (idempotent) → arms the timer off the restored deadline + installs the foreground hook. |
| **Uniform token intake** | `core/auth/authService.ts` | Every login flow (password/OTP/Google/Apple/register) funnels through `establishSession → authStore.setTokens`, which stamps `expiresAt` — so all of them get a proactive timer. |
| **JWT deadline** | `core/auth/jwt.ts` | `deriveExpiresAt` = JWT `exp` (authoritative) ?? `now + expiresIn` — survives restarts where only the stored token exists. |

Design notes: the coordinator calls **bare axios** (not the app's `httpClient`) for the refresh, so it can't recurse
through the 401 interceptor and has no circular import. On success it writes the store via `setState` (tokens already
persisted in `doRefresh`), and the store subscription re-arms the timer — one code path owns the schedule.

---

## 2. Live verification (running stack)

### 2a. Mobile BFF `/auth/refresh` — the contract the coordinator consumes
`bff-marine-mobile` up on `:17003`. Minted a genuine `inktavia-mobile` token pair for the owner
`qa.owner.aug5@inktavia.com` (transient, reverted DAG toggle for ROPC — DAG back to `false`, confirmed):
`access lifespan = 900s`, `azp = inktavia-mobile`, **`refresh_expires_in = 604800` (7 days)**.

`POST http://localhost:17003/api/v1/mobile/auth/refresh` `{ refreshToken }`:

| Case | Result |
|---|---|
| valid refresh token | **200** → `{ accessToken, refreshToken, expiresIn: 900, tokenType: "Bearer" }`, refreshed `azp=inktavia-mobile`; **new RT ≠ old RT** (rotation ✓) |
| **garbage** refresh token | **401** → drives the coordinator's `handleRefreshFailure()` → login once |
| the new rotated RT again | **200** (still valid) |

So the endpoint the coordinator depends on behaves exactly as specified: fresh token set on success, **rotated** refresh
token, 401 on an invalid token. The `refresh_expires_in = 604800` confirms the same config unblock (7-day idle ≫ 15-min
access) that AR2/AR3 proved now covers mobile.

> **`revokeRefreshToken` nuance (observed):** the realm currently has `revokeRefreshToken = false`, so replaying the
> *old* (rotated-away) refresh token still returns 200 — Keycloak issues a new RT each grant but does not immediately
> revoke the previous one. The single-flight design is therefore not strictly load-bearing *today*, but it remains the
> correct, forward-safe choice: it keeps the stored RT consistent under concurrency, and it becomes **mandatory** the
> moment `revokeRefreshToken` is turned on (then a double-spend hard-401s). No code change needed.

### 2b. Type + build
`npx tsc --noEmit` → **clean (exit 0)**.

### 2c. Checklist mapping
- **single-flight (N concurrent 401s → one call):** `inFlight` latch in `refreshSession()` — all callers await the same
  promise; verified by review (the live BFF confirms a double-spend would matter under revoke).
- **rotation (stored RT == last response):** `doRefresh` persists `payload.refreshToken`; live BFF confirms the response
  rotates.
- **proactive timer fires before exp:** `scheduleProactiveRefresh` delay = `expiresAt − 60s − now`.
- **foreground-after-expiry refreshes:** `onAppStateChange('active')` → `isExpiringSoon` → `refreshSession()`.
- **failed/revoked refresh → login exactly once:** `handleRefreshFailure` idempotent (guarded by `isAuthenticated`);
  live BFF 401 on a bad token is the trigger.

---

## 3. Deliberate scope decisions (surfaced, not silent)
- **Connectivity-regain refresh not separately wired.** The doc lists "regained connectivity" alongside foreground. The
  app has **no NetInfo dependency**, and adding `@react-native-community/netinfo` is a native dependency + rebuild —
  beyond "reuse existing primitives" and risky to add here. The **`AppState → active` foreground hook** (wired) is the
  dominant mobile trigger, and any connectivity blip while foregrounded is covered by the next request's reactive-401
  and the proactive timer. If desired later, add NetInfo and call `refreshSession()` on `isConnected` regain within the
  margin — a ~5-line addition mirroring `onAppStateChange`.
- **No unit tests added.** The app has **no test runner** (no jest/vitest, no test script). Standing up jest-expo (config
  + babel + native-module mocks) is out of AR4's additive scope; the coordinator logic is verified by code review +
  `tsc` + the live BFF contract. If a runner is added later, the single-flight/rotation/logout-once cases are the ones
  to pin.

---

## 4. Manual device walkthrough (owner `qa.owner.aug5@inktavia.com`)
An RN/Expo app can't be driven from this environment (no simulator automation; Expo **web** doesn't support
`expo-secure-store`/SignalR/AppState faithfully). The doc allows device-only steps to stay manual. Run on a
simulator/device against the running stack:
1. Sign in (password/OTP) → confirm tokens land in SecureStore and screens load.
2. Background the app **> 15 min** (past the access lifespan; within the 7-day idle) → foreground → **still logged in**,
   data loads via a **silent refresh** (proactive timer or the foreground hook), no login screen.
3. Keep a realtime screen (notification bell) open across the 15-min boundary → it keeps receiving (the SignalR factory
   re-mints the token on reconnect).
4. Open a screen from a **push tap** after expiry → it rides the refreshed session.
5. Force a **revoked/expired refresh token** (e.g. server-side user logout) → the next refresh 401s → **one clean
   re-login**, no loop.

Expected: only step 5 reaches the login screen; steps 2–4 stay authenticated.

---

## 5. AR track closed
Config prerequisite (REPORT_CONFIG_KEYCLOAK) + AR2 (admin, live) + AR3 (provider, live) + **AR4 (mobile, wired +
BFF-proven, device QA manual)** — silent refresh is now in place across all three portals, all against the same
Keycloak realm (access 15 min, SSO idle 7 days), each persisting the rotated refresh token and logging out only when the
refresh itself fails.
