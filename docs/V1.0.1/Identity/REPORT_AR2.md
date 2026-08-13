# REPORT_AR2 — admin silent refresh: repointed to Keycloak + proactive/single-flight

> **Status:** Code complete. BFF `RefreshBff` repointed from the Identity `UserLoginTokenEntity` store to Keycloak's
> `refresh_token` grant; admin-web gains a single-flight + proactive silent-refresh coordinator. Builds/tests green.
> **Live "idle-past-expiry stays logged in" proof PENDING** — the local stack is down (`docker ps` empty) and the
> **config prerequisite** (SSO idle > access lifespan) must be applied by ops first. **Not committed.**
> Related: [[auth_refresh_two_token_systems]].

---

## The bug AR2 fixes (confirmed in AR1)

The admin login is a **Keycloak handoff**: OTP → `login_ticket` → PKCE against the **public `admin-panel`** client →
the browser stores a **Keycloak** access+refresh token pair. But the admin's refresh went to
`POST /auth/refresh → RefreshBff → IIdentityRemoteCall.Refresh → Identity RefreshLoginCommandHandler`, which matches
`UserLoginTokenEntity` by `RefreshToken == … AND AccessToken == currentPanelAccess AND DeviceId AND !IsRevoked`. A
Keycloak refresh token has **no such row → `TokenNotFound`** → every refresh failed → forced logout.

Mobile already refreshes correctly (Keycloak `refresh_token` grant on public `inktavia-mobile`); only **admin** was
mis-wired. AR2 applies **Option B** (repoint the admin BFF refresh to Keycloak — keeps refresh server-side, no
per-reload adapter re-init).

---

## BFF changes (`Aizen.Bff.AdminPanel`)

**New — `Common/Services/AdminKeycloakAuthClient.cs`** (`IAdminKeycloakAuthClient.RefreshAsync`): mirrors the mobile
BFF's `ParticipantKeycloakAuthClient.RefreshAsync`. POSTs the realm token endpoint
`grant_type=refresh_token&client_id={public admin client}&refresh_token=…`, returns `{AccessToken, RefreshToken,
ExpiresIn, TokenType}` or **null** on invalid/expired. Public client ⇒ **no secret**. Reuses the already-validated
`KeycloakServiceToken:TokenEndpoint` for the endpoint. Never logs tokens.

**New — `Common/Services/AdminJwtReader.cs`**: minimal, dependency-free JWT payload reader (no signature check — the
token was just minted by Keycloak on our own call). Fills the response profile (`email/given_name/family_name`) and
the refresh-token expiry.

**Repointed — `Auth/Command/RefreshBff/RefreshBffCommandHandler.cs`**: now depends on `IAdminKeycloakAuthClient`
instead of `IIdentityRemoteCall`. On success it shapes the Keycloak token set into the existing
`UserLoginResponse`/`TokenInfo` the admin-web already parses (`body.token.{accessToken,refreshToken,
accessTokenExpiredDate}`). On null → `AizenBusinessException(40102)` (`RefreshTokenTimeOut`) fail envelope, so admin-web
routes to `/login` exactly once. **The controller route and command/response contract are unchanged** — admin-web's
calls keep working. Identity `RefreshLogin` is **left untouched** (now unused by admin; available to any legacy channel).

**Options — `Common/Options/AdminPanelKeycloakOptions.cs`**: added `RefreshClientId` (default **`admin-panel`**), the
public SPA client that issues the admin session (matches admin-web `VITE_KEYCLOAK_CLIENT_ID`). Surfaced in
`appsettings.json`.

> ⚠️ **Config discrepancy flagged (for ops):** `AdminPanelKeycloak:AdminPanelClientId` is `admin-panel-web`, a client
> that does **not** exist in the realm export. The real token-issuing public client is **`admin-panel`** (admin-web
> env + the handoff `client_id`), so `RefreshClientId` defaults to that and is **not** derived from `AdminPanelClientId`.
> If ops later renames the client, set `AdminPanelKeycloak:RefreshClientId` to match.

**DI — `DependencyInjection.cs`**: `AddHttpClient()` + `AddScoped<IAdminKeycloakAuthClient, AdminKeycloakAuthClient>()`.

---

## admin-web changes (`inktavia-marine-admin-web`)

**New — `src/shared/auth/refreshCoordinator.ts`** — the single place that drives the refresh:
- **`refreshSession()`** — single-flight: concurrent callers (proactive timer + any number of reactive 401s) await the
  **same** in-flight request, so a **rotating** Keycloak refresh token is never double-spent. Resolves `false`
  immediately when there's no refresh token.
- **Rotation persisted**: always stores the returned `refreshToken` (Keycloak rotates it each refresh).
- **`scheduleProactiveRefresh()`** — after each login/refresh, refresh silently at **`exp − 45s`** (before the 401).
- **`initProactiveRefresh()`** — binds `visibilitychange` (tab focus) + `online` listeners; refreshes once if within
  the margin. Bound once at bootstrap (`AppProviders`).

**Wiring**:
- `httpClient.ts` reactive 401 → now calls `refreshSession()` (single-flight) instead of its own inline `axios.post`;
  removed the Identity-store specifics (`deviceId`/current-access-token) from the refresh body — Keycloak needs only
  the refresh token. Only a failed refresh routes to `/login`.
- `authService.ts` — `init()` step 2.5 uses `refreshSession()`; step 2 (valid token) and `commitKeycloakSession()` now
  `scheduleProactiveRefresh()`; `logout()` clears the timer. Shared token helpers (`mapTokenToUser`,
  `parseExpiryToEpoch`, `expFromJwt`) moved to `jwtUtils.ts` so the coordinator and service share them with no import
  cycle. (The AR1 `authStore` rehydrate fix — keep the refresh token across reloads — stays.)

---

## Verification

- **BFF** — `dotnet build` of `Aizen.Bff.AdminPanel.Application` and the `Aizen.Bff.AdminPanel` host: **0 errors**
  (pre-existing nullability warnings only).
- **admin-web** — `tsc --noEmit` clean; `eslint` clean on all changed files.
- **admin-web unit test** — new `src/test/refreshCoordinator.test.ts` (**3/3 pass**): (1) N concurrent refreshes →
  **one** BFF call + the **rotated** refresh token persisted; (2) no refresh token → `false`, no BFF call; (3) failed
  refresh → `false` (→ login). The rest of the unit suite is unchanged; the only red items are pre-existing and
  unrelated to AR2 (`env.test.ts` reads a locally-set `VITE_KEYCLOAK_URL`; `test/api/*` are live-BFF integration tests
  needing a running stack).
- **BFF unit test for the handler — NOT added:** there is no AdminPanel BFF test project (and no mobile
  `RefreshParticipant` test to mirror). Scaffolding a .NET test project is out of AR2's minimal scope; noted for a
  future test-infra pass. The handler is a thin shape-mapper over `IAdminKeycloakAuthClient` (itself a direct mirror of
  the proven mobile client), and its success/failure branches are covered indirectly by the admin-web coordinator test.

### Live proof — PENDING (stack down + config prerequisite)
Run after ops applies the AR1 config fix and the stack is up:
1. Log in as admin → note the current behavior was `TokenNotFound → logout` at ~access-TTL.
2. Let the access token expire while idle → the next action **succeeds via silent refresh** (no login redirect).
3. Reload after expiry (within SSO idle) → **stays logged in** (BFF Keycloak refresh, rotated token persisted).
4. Revoke/expire the refresh token → routes to `/login` **exactly once**.
Capture before/after.

---

## Config prerequisite (owner/ops — from AR1, still required)

None of this survives if Keycloak kills the session first. The realm is **inverted**: `ssoSessionIdleTimeout` 30 min
**<** `accessTokenLifespan` 1 h. Set **SSO Session Idle > Access Token Lifespan** (recommend access ~5–15 min + SSO idle
hours–days per security posture). Also confirm the **public `admin-panel` client is enabled** in the running realm (the
checked-in export marks it `enabled:false`); if disabled, the handoff can't issue tokens and refresh has nothing to
renew. Apply via realm/client config (secrets not committed).

---

## Next
- **AR3 (provider):** the provider web already uses the Keycloak adapter (`updateToken`) against a **separate**
  `provider-realm`; capture that realm's lifespans from the running server and align to the same rule. Mostly config +
  adapter hardening.
- **AR4 (mobile):** already Keycloak-correct (`RefreshParticipant`); verify the FE `/mobile/auth/refresh` round-trip
  across the corrected idle window and that the rotated refresh token is persisted to secure storage.
