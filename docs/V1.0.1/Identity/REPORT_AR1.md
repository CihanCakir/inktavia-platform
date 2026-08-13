# REPORT_AR1 — ~1h forced-logout diagnosis + admin rehydrate fix

> **Status:** Diagnosis complete (code + config analysis). One admin-web client bug fixed. Live manual
> refresh-grant test **could not be executed** — the local stack (Keycloak/BFF/Identity) is **not running**
> (`docker ps` shows no containers), so the config-vs-client verdict below is derived from the checked-in realm
> config + code, and the exact reproduction command is provided for the owner to confirm. **No commit.**

---

## 1. Captured lifespans

### 1a. Keycloak realm `inktavia-realm`
Source: `infrastructure/keycloak/inktavia-realm-realm.json` (the file docker-compose imports at line 81).

| Setting | Value | Notes |
|---|---|---|
| `accessTokenLifespan` | **3600 s (1 h)** | Long — this alone sets the ~1h cadence for any Keycloak-issued token. |
| `ssoSessionIdleTimeout` | **1800 s (30 min)** | **⚠️ SHORTER than the access token.** |
| `ssoSessionMaxLifespan` | 36000 s (10 h) | Hard session ceiling. |
| `revokeRefreshToken` | *unset* → default **false** | RT not single-use; not the cause. |
| `refreshTokenMaxReuse` | *unset* → default 0 | — |
| Client Session Idle/Max | *unset on realm & all clients* | Fall back to SSO values. |
| Offline session idle/max | *unset* → defaults (30d) | Only relevant with `offline_access` scope (not used by these clients). |

**🔴 Headline config red flag — the lifespans are inverted:** `ssoSessionIdleTimeout` (30 min) **<**
`accessTokenLifespan` (1 h). For **any Keycloak-issued session**, if no token refresh happens within a 30-minute
window the SSO session goes **idle** and the refresh token dies — **before** the 1-hour access token has even
expired. A client that only refreshes *after* the access token expires (or on the next reload past 1h) will find a
dead refresh token every time.

Per-client wiring (all in the same realm export):
- `inktavia-mobile` — public, standard flow + PKCE (S256). Mobile participant login. (webOrigins `+`.)
- `customer-panel` — public, standard flow + PKCE.
- `admin-panel` — **DISABLED** (`enabled:false`, `standardFlowEnabled:false`); marked *DEPRECATED — BFF now handles
  tokens*. **But** admin-web's `keycloakClient.ts` still uses `clientId = admin-panel` for the login-ticket handoff
  redirect (`VITE_KEYCLOAK_CLIENT_ID`). If the checked-in export reflects the running realm, the handoff redirect
  would fail against a disabled client — **flag for the Step-2 live test / owner** (the runtime realm may differ
  from the export; `init.sh` mutates it).
- `admin-panel-bff`, `marine-mobile-bff` — confidential, `client_credentials` (service accounts only), no
  standard/direct-access flow. Used for S2S, not user tokens.

### 1b. Identity custom token store (the "other" refresh path)
The admin/mobile BFF `/auth/refresh` does **not** call Keycloak's `refresh_token` grant. It calls Identity
`RefreshLoginCommand`, which validates the presented refresh token against its **own** DB table
`UserLoginTokenEntity` and mints a **new Identity-signed JWT** via `InktaviaTokenService.GenerateToken`.

TTLs — config section `"TokenOption"` (bound to `TokenSettings` at `Core/Auth/.../BuilderExtensions.cs:168,297`),
identical in `appsettings.json` / `.Development.json` / `.Production.json`:

| Setting | Value | = |
|---|---|---|
| `AccessTokenExpiration` | **43800 min** | **~30 days** |
| `RefreshTokenExpiration` | **10080 min** | **7 days** |

So the Identity-issued token path is *generous* (30d access / 7d refresh) — it is **not** a source of a ~1h logout.
This split is the crux of the diagnosis (see §2).

---

## 2. Config-vs-client verdict (manual refresh-grant isolation)

> Live test **not run** — stack down. Verdict below is from config + code; **confirm with the command in §2b.**

The two token systems make this **not a single verdict — it splits by which token the client actually holds:**

### 2a. Analysis by client

**Mobile (and any Keycloak-issued session) → CONFIG is the blocker.**
Mobile stores a Keycloak access/refresh pair (`inktavia-mobile`); its BFF refresh
(`ParticipantKeycloakAuthClient`, `grant_type=refresh_token`) hits Keycloak directly. Because
`ssoSessionIdleTimeout (30m) < accessTokenLifespan (1h)`, the Step-2 test ("wait until the access token has
expired at 1h, then POST refresh") is **predicted to return `invalid_grant`** — the 30-min idle killed the session
long before 1h. **No client fix helps until the realm config is corrected.**

**Admin → CLIENT bug + a token-store mismatch risk.**
Admin logs in via OTP → **LoginTicket** → `loginWithTicket()` → Keycloak PKCE handoff, so it holds a **Keycloak**
token (`exp = accessTokenLifespan = 1h`) and a **Keycloak** refresh token. Two independent defects:
1. **Confirmed client bug (fixed in §5):** `authStore.onRehydrateStorage` discarded **both** tokens when the
   access token was expired, and `authService.init()` then `clearAuth()`d — so on any reload past 1h the admin was
   forced to `/login` even though a refresh could have been attempted.
2. **Token-store mismatch (risk — needs live confirm):** the admin's reactive 401 interceptor sends its **Keycloak**
   refresh token to Identity `/auth/refresh`, which looks it up in `UserLoginTokenEntity`. A Keycloak RT is not in
   that table → `TokenNotFound`. If confirmed live, the admin refresh path is wired to the wrong issuer and the §5
   fix keeps the *token* but the refresh call itself would still fail — that becomes the core of **AR2**.

### 2b. Exact reproduction command (run once the stack is up)
```bash
# 1) Get a token pair for the client under test (mobile example — inktavia-mobile is a public PKCE client,
#    so use the real login flow, or for admin-panel-bff-mediated flows capture the pair from the BFF response).
#    For a quick realm-level probe with a direct-access client, or reuse a captured refresh_token:

RT='<paste refresh_token>'
CLIENT='inktavia-mobile'      # or the client the token was issued to

# 2) WAIT until past the access token's exp (≳ the 30-min idle window is the decisive part here).

# 3) Ask Keycloak to refresh:
curl -s -X POST \
  'http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token' \
  -d "grant_type=refresh_token" \
  -d "client_id=${CLIENT}" \
  -d "refresh_token=${RT}" | jq .
```
- **Fresh `access_token` returned** → refresh valid past access expiry ⇒ **bug is client wiring** (AR2–AR4).
- **`invalid_grant` / "Session not active"** → refresh token already dead ⇒ **bug is Keycloak lifespans** — fix
  config first (§4). Given `idle(30m) < access(1h)`, this is the **predicted** result for Keycloak-issued sessions.

---

## 3. Per-client refresh-path confirmation (code findings)

- **admin-web** — reactive `401 → POST /auth/refresh` interceptor exists
  (`src/shared/api/httpClient.ts:53-95`): sends `{refreshToken, deviceId}` (no Bearer), reads
  `body.token.accessToken/refreshToken`, retries once (`_retry`). BFF: `AuthController → RefreshBffCommand →
  IIdentityRemoteCall POST /api/v1/auth/refresh → Identity RefreshLoginCommand`. **No proactive refresh timer**
  (that's AR2). `onRehydrateStorage` bug confirmed and fixed (§5). *Note:* the interceptor does **not** forward the
  expired access token, but Identity `RefreshLoginCommand` requires `ClientInfo.AuthToken` to be present
  (`AccessTokenRequiredForThisPanel`) — another AR2 item to verify.
- **provider-web** — healthier: `getFreshAccessToken()` calls keycloak-js `updateToken(30)` **before every request**
  (`authInterceptors.ts:16`), and `keycloakClient.init({ onLoad:'check-sso', silentCheckSsoRedirectUri })` so a
  reload silently re-obtains a token from the SSO session. On a 401 *after* that silent attempt it `clearAuthState()`
  (logout) — correct, since one refresh already failed. Provider uses a **separate realm** (`provider-realm`,
  configured at runtime by `infrastructure/keycloak/provider-realm/setup-provider-realm.sh` via `kcadm` — its
  lifespans are **not** in any checked-in export, so capture them from the running realm in AR3). Provider is still
  bounded by *that* realm's SSO idle.
- **mobile** — repo **is** mounted (`inktavia-marine-mobile`). Stores `accessToken` + `refreshToken` + `identityToken`
  in **`expo-secure-store`** (`src/core/auth/tokenStorage.ts`) — secure ✅. Refresh via BFF
  `POST /api/v1/mobile/auth/refresh → RefreshParticipant → ParticipantKeycloakAuthClient (grant_type=refresh_token`
  against Keycloak). ⇒ **mobile is fully subject to the inktavia-realm inversion** (access 1h, idle 30m): backgrounded
  > 30 min ⇒ dead RT ⇒ forced re-login. Confirm end-to-end in **AR4**.

---

## 4. Recommended target lifespans (recommendation — **owner/ops applies**, values not committed)

The single change that unblocks *every* Keycloak-issued session: **make SSO idle longer than the access token**,
and shorten the access token so silent refresh has room to work.

| Setting | Current | Recommended (option A — balanced) | Recommended (option B — tighter security) |
|---|---|---|---|
| `accessTokenLifespan` | 3600 (1h) | **300–900 s (5–15 min)** | 300 s (5 min) |
| `ssoSessionIdleTimeout` | 1800 (30m) | **28800 s (8 h)** | 3600 s (1 h) |
| `ssoSessionMaxLifespan` | 36000 (10h) | 36000–86400 s (10–24 h) | 36000 s (10 h) |
| `revokeRefreshToken` | false | keep false, **or** true (see note) | true (rotation) |

Trade-off to present to the owner:
- **Shorter access token** = tighter (a leaked access token expires fast) but requires the clients to refresh — which
  is exactly what AR2–AR4 make reliable.
- **Longer SSO idle** = fewer surprise logouts / better UX, at the cost of a longer window an idle session stays
  resumable. Option A (8h idle) means "log in once per working day"; Option B (1h idle) is stricter.
- **`revokeRefreshToken=true`** (rotation) is more secure but *requires* every client to persist the **rotated**
  refresh token on each refresh — AR2 (admin), AR3 (provider), AR4 (mobile) must store the new RT. Leave it **false**
  until those land, to avoid breaking refresh mid-session.

**Hard rule regardless of option:** `ssoSessionIdleTimeout` **must exceed** `accessTokenLifespan`, or post-expiry
refresh is impossible. The current config violates this.

For the **Identity `TokenOption`** path (admin/mobile BFF refresh): access **30d** is excessive for a browser access
token — consider **15–60 min** access / **7–30d** refresh so the Identity-signed token isn't a long-lived bearer.
(Recommendation only; deferred to AR2/AR4.)

---

## 5. Admin rehydrate fix (applied — client bug #1)

Two minimal, safe edits in `inktavia-marine-admin-web` so a returning admin whose access token expired while away
**stays logged in** as long as the refresh token is valid, instead of being force-logged-out on reload.

**`src/shared/auth/authStore.ts` — `onRehydrateStorage`:** on rehydrate, when the access token is expired, clear
**only** `accessToken`/`accessExpiresAt`; **keep** `refreshToken` and `user`. (Previously it nulled all four.)

**`src/shared/auth/authService.ts` — `init()` step (2.5):** because `ProtectedRoute` redirects to `/login` the
moment the store is `unauthenticated` (so "the first request drives a 401 refresh" never gets a chance — no request
is made), `init()` now, when the access token is invalid **but a refresh token survives**, drives a **silent refresh**
via `POST /auth/refresh` (new `trySilentRefresh()` helper, mirroring the reactive interceptor) before falling through
to `clearAuth()`. On success it commits the fresh session and the admin stays in; only a dead/absent refresh token
routes to `/login`.

> Without the `init()` change, the `authStore` change alone is a **no-op** — `init()` step (3) `clearAuth()`s on an
> invalid access token before any request can trigger the reactive interceptor. Both edits together are what
> actually delivers "reload → stays logged in." Proactive/single-flight refresh is still **AR2**.

**Verification:**
- `tsc --noEmit` — **clean** ✅
- `eslint` on both changed files — **clean** ✅
- **Live "reload-stays-logged-in" proof — PENDING (stack down).** Steps to run when up: (1) log in as admin;
  (2) let the access token expire (or fast-forward `accessExpiresAt`); (3) reload → expect a silent
  `POST /auth/refresh` and the app to stay authenticated; (4) with a truly dead refresh token, expect a single clean
  route to `/login`. ⚠️ This live proof depends on the **token-store mismatch (§2a.2)** being resolved — if the
  admin's Keycloak refresh token is rejected by Identity's `UserLoginTokenEntity`, the silent refresh returns 401 and
  the admin still lands on `/login`; that wiring is **AR2**.

No login/logout flow change, no secrets touched, no config committed.

---

## 6. Next (per verdict)

1. **Config first (owner/ops):** apply §4 — raise `ssoSessionIdleTimeout` above `accessTokenLifespan` and shorten the
   access token. This is the prerequisite that unblocks mobile and any Keycloak-issued session. Run §2b to confirm.
2. **AR2 (admin):** proactive single-flight silent refresh + **resolve the Keycloak-RT-vs-Identity-store mismatch**
   (decide the one issuer for the admin refresh path) + confirm the interceptor forwards the access token Identity
   requires.
3. **AR3 (provider):** capture the `provider-realm` lifespans from the running realm; align them to §4; verify the
   `updateToken` path across the idle boundary.
4. **AR4 (mobile):** confirm the `/mobile/auth/refresh` round-trip across the (corrected) idle window; ensure the
   rotated refresh token is persisted to secure storage.
