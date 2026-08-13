# BE_AR2 — admin silent refresh: repoint to Keycloak + proactive refresh

> **Repos:** `addesso-project` (AdminPanel BFF), `inktavia-marine-admin-web`. AR1 confirmed **all three clients are
> Keycloak-issued**, but admin **refreshes against the wrong system**: its login is a **Keycloak handoff** (it stores a
> Keycloak access+refresh token), yet its refresh calls `/auth/refresh` → `RefreshBff` → Identity `RefreshLogin`, which
> validates Identity's **own `UserLoginTokenEntity` store** — the Keycloak refresh token isn't there → `TokenNotFound`.
> Fix = refresh admin against **Keycloak**, plus proactive silent refresh. **Config prerequisite** (AR1): SSO idle must
> exceed the access-token lifespan or no refresh survives. **Do not commit.**

## Confirmed (from code, AR1)
- `admin-web/src/shared/auth/keycloakClient.ts` = a `keycloak-js` adapter; `resolveKeycloakSession()` commits the
  **Keycloak** `token`/`refreshToken` into `authStore` on the OAuth callback.
- BUT `httpClient` (reactive 401) and `authService.init` step 2.5 both exchange the refresh token via **`POST
  /auth/refresh`** → `RefreshCommandHandler` → `IIdentityRemoteCall.Refresh` → Identity **`RefreshLoginCommandHandler`**,
  which matches `UserLoginTokenEntity` by `RefreshToken == … AND AccessToken == currentPanelAccess AND DeviceId AND
  !IsRevoked`. A **Keycloak** refresh token has no such row ⇒ `TokenNotFound`. This is the admin refresh failure.
- Mobile already does it right: `RefreshParticipantCommandHandler` → `IParticipantKeycloakAuthClient.RefreshAsync` →
  **Keycloak** `refresh_token` grant (public `inktavia-mobile` client). Provider-web uses the Keycloak adapter
  (`updateToken`). Only **admin** is mis-wired to the vestigial Identity-store path.

## Fix — Option B (recommended: repoint the admin BFF refresh to Keycloak)
Minimal + robust for admin's architecture (persisted store + BFF-call refresh, no per-reload adapter re-init needed).

### BFF (`AdminPanel`)
- Give the AdminPanel BFF a **Keycloak auth client** mirroring mobile's `ParticipantKeycloakAuthClient` — a
  `RefreshAsync(refreshToken)` that calls Keycloak `POST {realm}/protocol/openid-connect/token`
  `grant_type=refresh_token&client_id={admin client}&refresh_token=…` (client secret only if the admin client is
  confidential; keep it server-side, never in the SPA). Returns `{ accessToken, refreshToken, expiresIn, tokenType }`
  or null on invalid/expired.
- Repoint `RefreshCommandHandler` (the `RefreshBff` command) to call this **Keycloak** client instead of
  `IIdentityRemoteCall.Refresh` (the Identity `UserLoginTokenEntity` store). Return the Keycloak token set in the
  existing `UserLoginResponse`/token shape the admin-web already parses (`body.token.{accessToken,refreshToken}` /
  `body.{accessToken,refreshToken}`). On null → the existing error envelope (admin-web then routes to login).
- Drop the Identity-store-only requirements from this path (device-id match, current-access-token match). The Keycloak
  grant needs only the refresh token (+ client id). Leave Identity `RefreshLogin` untouched for any legacy channel that
  still uses it (it's now unused by admin).

### admin-web
- The store + interceptor shape **already fits**: the httpClient 401 interceptor and `authService.init` step 2.5 POST
  the refresh token and persist the returned `{ accessToken, refreshToken }`. Keep that. Remove any Identity-store
  specifics from the refresh request body (e.g. don't rely on `deviceId`/the current access token matching); send what
  the Keycloak-backed BFF needs (the refresh token).
- **Persist the rotated refresh token:** Keycloak returns a new refresh token on each refresh (and may revoke the old
  one) — always store the returned `refreshToken`, not just the access token (the AR1 store fix already keeps the
  refresh token across reloads; this ensures it's the *latest* one).

## Add proactive silent refresh (both — don't wait for a 401)
Reactive-only means the first call after expiry always eats a 401 (and background timers/websockets may drop). Add a
**proactive** scheduler in admin-web:
- After each successful login/refresh, schedule a silent refresh at **`exp − margin`** (e.g. 30–60s before expiry).
- **Single-flight:** if a refresh is already in progress, concurrent callers (proactive timer + any 401) **await the
  same** in-flight refresh — no stampede, no double-spend of a rotating refresh token.
- On tab focus / regaining connectivity, if within the margin, refresh once.
- Keep the reactive 401 path as the fallback; **only route to `/login` when the refresh itself fails** (dead/absent
  refresh token), never on a mere access-token expiry.

## Config prerequisite (owner/ops — AR1)
None of this survives if Keycloak kills the session first. Ensure **SSO Session Idle > Access Token Lifespan** (AR1
found idle 30m < access 1h — inverted). Recommend access ~5–15 min + SSO idle long enough for UX (hours–days per
security posture); the owner sets the exact values. Apply via realm/client config (surfaced; secrets not committed).

## Alternative — Option A (client-side Keycloak adapter, not recommended for admin now)
admin-web already has a `keycloak-js` adapter; it *could* silent-refresh via `keycloak.updateToken(minValidity)` like
provider-web. But on a **normal reload** admin only runs the adapter callback when `urlHasOAuthResponse()`, so the
adapter isn't re-initialized with the stored refresh token every load — using `updateToken` reliably would need
`check-sso`/silent-SSO wiring. Option B avoids that by keeping the refresh server-side. (If you later standardize all
web portals on the adapter, do it as a separate unification.)

## Don't-break / QA
- BFF: only the admin `RefreshBff` downstream changes (Identity store → Keycloak grant); the command/response contract
  and the controller route are unchanged, so admin-web's calls keep working. Client secret (if any) stays server-side.
- admin-web: additive proactive scheduler + single-flight; the AR1 rehydrate fix stays. No login/logout flow change.
- **Tests:** (1) BFF unit — `RefreshBff` with a valid Keycloak refresh token returns a fresh token set; invalid →
  clean error (mirror mobile's handler tests). (2) admin-web — single-flight (N concurrent 401s → one refresh call);
  rotated refresh token is persisted; only a failed refresh routes to login. (3) `tsc`/lint clean; BFF `dotnet build`
  green.
- **Live (after the config fix + stack up):** log in as admin; let the access token expire while idle → the next action
  **succeeds via silent refresh** (no login redirect); reload after expiry (within SSO idle) → **stays logged in**;
  revoke/expire the refresh token → routes to login exactly once. Capture the before/after (was: `TokenNotFound` →
  logout).

## Report
`docs/V1.0.1/Identity/REPORT_AR2.md`: the BFF repoint (Identity-store → Keycloak grant), the admin-web proactive +
single-flight wiring, and the live "idle-past-expiry stays logged in" proof. Then AR3 (provider adapter hardening) /
AR4 (mobile RefreshParticipant FE wiring) — both already Keycloak, so mostly client wiring + the shared config fix.
Cross-link [[auth_refresh_two_token_systems]].
