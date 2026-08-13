# FE_AR3 — provider-web silent refresh (verify-after-config + hardening)

> **Repo:** `inktavia-marine-provider-web`. Provider-web's refresh mechanism is **already mostly correct** — it was
> failing for the **same Keycloak config reason** (SSO idle 30 min < access 1h), now fixed
> (`CONFIG_KEYCLOAK_SESSION_AND_ADMIN_CLIENT.md`: access 15 min, idle 7 days). AR3 = **verify** it now silently
> refreshes, then add light hardening. **Do not commit.**

## What's already right (from code)
- `keycloakClient.initKeycloak()` uses `onLoad: 'check-sso'` + `silentCheckSsoRedirectUri` + PKCE → on a **reload** the
  session re-establishes from the Keycloak SSO cookie (no forced re-login while the SSO session is alive).
- `tokenProvider.getFreshAccessToken()` calls `keycloak.updateToken(30)` **before each request** (refreshes if
  expiring within 30 s) → effectively proactive per-request refresh; on failure → `login()`.
- `authInterceptors` request interceptor attaches the fresh token; the 401 handler clears + `login()`.

So with the config fix, the provider portal should **stop logging out at expiry**. First step is to prove that.

## Step 1 — verify (after the config fix + stack up)
Log in as a provider, leave the tab idle **past the 15-min access lifespan** (but within the 7-day SSO idle) → the next
action **succeeds** (an `updateToken` silently mints a new access token; no `/login` redirect). Reload after expiry →
**stays logged in** via `check-sso`. Only past SSO idle/max (or an explicit logout) → one clean re-login. If this
passes, AR3 is essentially done; apply the hardening below and close.

## Step 2 — hardening (small, additive)
1. **Single `updateToken` retry on 401.** The response interceptor currently does `clearAuthState()+login()` on any
   401. Before giving up, attempt **one** `keycloak.updateToken(-1)` (force refresh) + retry the original request once;
   only if that still 401s → clear + login. Guards the case where a token expired mid-flight or was invalidated between
   the proactive check and the server.
2. **Proactive background refresh timer.** `getFreshAccessToken` only refreshes when a request is made; a long-idle tab
   with an open **SignalR** connection can let the token lapse. Schedule a refresh at **`exp − margin`** (e.g. 60 s)
   via `keycloak.onTokenExpired` (or a timer set from `tokenParsed.exp`) that calls `updateToken` and, on failure,
   routes to login. **Single-flight** (keycloak-js already serializes `updateToken`, but don't stack timers).
3. **Realtime token freshness (important).** Ensure the **SignalR** connection uses an `accessTokenFactory` that calls
   `getFreshAccessToken()` (so each (re)connect/negotiate carries a fresh token) — otherwise the hub can hold a stale
   token and drop after 15 min even though HTTP calls refresh fine. Verify the realtime client (the
   `Aizen.Core.Realtime` edge / provider realtime hook) pulls the token through `getFreshAccessToken`, not a captured
   constant.
4. **Only logout when refresh fails.** Confirm no code path routes to `/login` on a mere access-token expiry — only on
   a failed `updateToken` / dead SSO session.

## Don't-break / QA
- Additive; no login/logout flow change. The config fix is the actual unblock; the hardening covers background/realtime
  edges.
- **Tests / checks:** (1) `tsc`/lint clean; (2) idle-past-expiry → silent refresh (no redirect); (3) reload past
  expiry within idle → stays logged in; (4) a forced 401 mid-session → one updateToken retry recovers without logout;
  (5) an open SignalR connection survives past the access lifespan (token factory refreshed).

## Report
`docs/V1.0.1/Identity/REPORT_AR3.md`: the post-config verification result (idle-past-expiry stays logged in), the
401-retry + proactive-timer + SignalR-token-factory hardening, and any realtime-token gap found/fixed. Cross-link
[[auth_refresh_two_token_systems]].
