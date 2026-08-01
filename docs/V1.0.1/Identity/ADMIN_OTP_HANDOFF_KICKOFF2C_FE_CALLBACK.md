# ADMIN OTP HANDOFF — Kickoff 2c (admin-web callback fix): land the Keycloak session in the app

> **Repo:** `inktavia-marine-admin-web` (FE only). **Backend + Keycloak are DONE and verified on-screen** — do NOT
> touch them. This kickoff fixes the LAST gap: after Keycloak authenticates the admin, the admin-web callback does not
> reliably lift the Keycloak session into the app's auth store, so the user bounces back to `/login`.
>
> **Reference (read, do NOT modify):** provider-web (`inktavia-marine-provider-web`) — it processes the Keycloak
> callback at **bootstrap inside `AuthGate`** (which awaits `initKeycloak()` before rendering routes), so by the time
> its `AuthCallbackPage` renders the session is already known. admin-web instead processes it late, inside
> `AuthCallbackPage.useEffect`, which races the bootstrap and loses the result.

## Verified end-to-end (evidence from a live on-screen run — the chain works up to the FE callback)
1. Backend `verify` → `{ verified:true, nextAction:"redirect_to_keycloak_handoff", loginTicket:"eyJ…" }` ✅
2. FE `loginWithTicket` → navigates to `http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/auth?client_id=admin-panel&redirect_uri=http://localhost:3000/auth/callback&…&login_ticket=…` ✅
3. Keycloak SPI logs: `[otp-login] Ticket accepted — authenticating user 'admin.user@inktavia.com'` → Keycloak issues an SSO session and 302s to `/auth/callback` ✅
4. `keycloak-js` (after init) IS authenticated: `keycloak.authenticated===true`, token present, `preferred_username=admin.user@inktavia.com`, and **`realm_access.roles` includes `Admin`** ✅ (Kickoff 2's realm role add worked)
5. **BUG:** `authStore` (`inktavia-auth`) stays `{ accessToken:null, user:null, isAuthenticated:false }` — the Keycloak session is never durably committed to the app store, so `ProtectedRoute` sends the user to `/login`.

## Two concrete FE defects to fix

### Defect A — the committed session is wiped by the bootstrap `authService.init()` race
`AppProviders` calls `authService.init()` on every load. `init()` (src/shared/auth/authService.ts) does: if the stored
access token isn't valid → **`clearAuth()`**. On the callback page this runs concurrently with
`AuthCallbackPage.useEffect` → `initKeycloak()` → `commitKeycloakSession()`. Depending on ordering, `clearAuth()` wipes
the freshly committed Keycloak session (observed: `commit` happens but `authStore` ends empty).

### Defect B — the OAuth callback is processed too late / unreliably (mirror provider-web)
The code exchange must happen **during bootstrap, before routes render and before `authService.init()` decides
unauthenticated** — exactly as provider-web does in `AuthGate`. Doing it in `AuthCallbackPage.useEffect` (post-render)
races the guard/store. (A live probe also showed the OAuth response missing from `window.location` by the time the
page effect read it — processing at bootstrap, before anything else touches the URL, removes this class of races.)

## Required changes (FE only, mirror provider-web)
1. **Process the Keycloak callback at bootstrap.** Introduce (or extend) a single bootstrap auth step — the admin
   equivalent of provider-web's `AuthGate` — that runs **before** the router renders protected routes and **before**
   `authService.init()` clears state. It must:
   - `await initKeycloak()` once (the memoized promise) so keycloak-js completes the PKCE code exchange when the URL
     carries an OAuth response, OR resolves the existing SSO session via check-sso.
   - If `keycloak.authenticated && keycloak.token` → `authService.commitKeycloakSession({ accessToken: keycloak.token,
     refreshToken: keycloak.refreshToken, accessTokenExpiredDate: <from tokenParsed.exp> })` **before** the guard
     evaluates, so `authStore` is populated (roles read from `realm_access.roles`, incl. `Admin`).
   - Only fall through to the normal `authService.init()` / unauthenticated path when keycloak is NOT authenticated.
   Keep the day-to-day BFF/JWT `identity` behavior for non-Keycloak sessions intact.
2. **Stop `authService.init()` from clobbering a live Keycloak session.** Order bootstrap so the Keycloak
   callback/SSO resolution + `commitKeycloakSession` happen first (or make `init()` consult keycloak-js and NOT
   `clearAuth()` when `keycloak.authenticated`). Net: a valid Keycloak login must survive bootstrap.
3. **Make the OAuth response robust to the SPA:** set keycloak-js **`responseMode: 'query'`** (in the `keycloak.init`
   options and/or `createLoginUrl`) so the authorization `code` arrives as a query param (survives `createBrowserRouter`
   cleanly) instead of the default URL fragment. Verify `silent-check-sso.html` is served (it is) and unaffected.
4. **Slim `AuthCallbackPage` to match provider-web:** once bootstrap owns the exchange, the callback page just waits for
   auth to settle then routes (`isAuthenticated()` → dashboard, else `/login`) — it should not be the sole place the
   exchange happens.
5. **Diagnostics while implementing (remove before done):** temporarily set `enableLogging: import.meta.env.DEV` in the
   keycloak `init` options to see keycloak-js's callback processing in the console; confirm a `POST …/openid-connect/token`
   200 and `keycloak.authenticated===true` on the callback. Remove the flag (and any temp logs) before finishing.

## Do NOT touch
Backend (Identity), Keycloak (`init.sh`, SPI, realm), provider-web, CargoDry. `admin-panel` realm client already has
`standardFlowEnabled=true`, `http://localhost:3000/*` redirect URI, the `Admin` realm role assigned, and the SPI flow
bound — all verified working. The env (`VITE_KEYCLOAK_URL/REALM/CLIENT_ID=admin-panel` in `.env.local`) is correct;
the dev server must be started AFTER those exist (already handled).

## Acceptance / verification (fast — a Keycloak SSO session for admin already exists)
- `npm run typecheck` + build clean. From `http://localhost:3000/login`: enter `admin.user@inktavia.com`, request code,
  read `[DEV-ONLY] OTP login code …` from `docker compose logs identity-api`, verify → browser completes the Keycloak
  handoff (no password) and **lands in the admin dashboard authenticated**; `authStore` holds the Keycloak access token;
  admin BFF calls return 200; `AuthGate`/`ProtectedRoute` admit the `Admin` realm role. Non-admin identifier → blocked.
- provider-web + CargoDry git-clean. Report → `docs/V1.0.1/Identity/REPORT_ADMIN_OTP_HANDOFF_2C.md` (files changed,
  which defect each change addresses, the masked end-to-end transcript, and confirmation the diagnostics were removed).

> **Note:** a prior debugging session left `admin@inktavia.local` with a placeholder subject; the canonical admin is
> `admin.user@inktavia.com` (realm user, real subject linked). Log in with that email.
