# REPORT — ADMIN OTP HANDOFF, Phase 2b (admin-web redirect handoff)

**Status: IMPLEMENTED; verified end-to-end at the protocol level against the running SPA origin. The on-screen
browser click-through is pending (the Claude browser extension is not connected in this session).**

Repo: `inktavia-marine-admin-web` (sibling of `addesso-project`). Mirrors provider-web's Keycloak handoff.

## Files

- **`src/shared/auth/keycloakClient.ts`** (new) — minimal keycloak-js adapter mirroring provider-web: a
  `Keycloak({url,realm,clientId})` instance (clientId from env, `admin-panel`), `initKeycloak()` (memoized promise,
  `onLoad:'check-sso'` + PKCE S256 + `silentCheckSsoRedirectUri`, `checkLoginIframe:false`), and
  **`loginWithTicket(loginTicket)`** which awaits init, builds `createLoginUrl({ redirectUri: <origin>/auth/callback })`
  and appends `&login_ticket=`, then `window.location.assign`. Env imported from `@app/config/env`; callback path from
  `ROUTES.AUTH_CALLBACK` (admin-web has no provider-style `paths` module). Keycloak env vars are optional in the
  schema, so coerced with `?? ''`.
- **`public/silent-check-sso.html`** (new) — the standard silent-SSO relay (copied from provider), required by
  keycloak-js `check-sso`.
- **`src/pages/public/AuthCallbackPage.tsx`** (new) — OAuth redirect target. Dynamically imports the keycloak client,
  runs `initKeycloak()` (completes the PKCE code exchange), then lifts the Keycloak access token into the existing
  JWT store via `authService.commitKeycloakSession({ accessToken, refreshToken, accessTokenExpiredDate })` and routes
  to the dashboard. StrictMode-guarded (single-use code). Falls back to `/login` on failure — never fakes a session.
- **`src/app/router/routes.tsx`** — added `AUTH_CALLBACK: '/auth/callback'`.
- **`src/app/router/routeObjects.tsx`** — registered the callback route **bare** (no Public/Protected wrapper) so
  keycloak-js can process the `?code=` callback regardless of current auth state.
- **`src/pages/public/LoginPage.tsx`** — in `handleOtpSubmit`, added the handoff branch: on
  `verified && nextAction==='redirect_to_keycloak_handoff' && loginTicket` → `await loginWithTicket(loginTicket)`
  (dynamic import). Kept the direct-token branch (forward-compat) and the `keycloak_handoff_required` blocked branch.
- **Env** — set `VITE_KEYCLOAK_URL` / `VITE_KEYCLOAK_REALM` / `VITE_KEYCLOAK_CLIENT_ID=admin-panel` in `.env.local`
  (active dev), and fixed the three `.env.*.example` files (uncommented; client id corrected from the wrong
  `admin-panel-bff` to the public `admin-panel`).

`AuthGate`/`ProtectedRoute`/roles were left as-is: they already gate on the `Admin` realm role read from
`realm_access.roles` (`ROLES.ADMIN='Admin'`), and `authService.commitKeycloakSession` already maps those claims.

## Verification

- **typecheck (`tsc --noEmit`): 0 errors.** My changed files are all clean.
- **build (`tsc -b && vite build`): fails with 12 pre-existing TS errors** — all in files I did not touch
  (`ProvidersPage.tsx`, `ProviderDetailPage.tsx`, plus a mock-data and a hook file; recharts/null-typing issues).
  Confirmed pre-existing by stashing my work: the clean tree fails with the **same 12 errors**. My changes add **zero**
  new build errors. (Flag: the admin-web build was already red before this task — out of scope here.)
- **Dev server** runs and serves `/login` (HTTP 200). Note: port **3001** (a stale process held 3000; vite fell back).
  `http://localhost:3001/*` is already a registered `admin-panel` redirect URI + web origin, so the handoff is
  unaffected; I did not kill the unknown process on 3000.
- **Protocol-level end-to-end against the running SPA origin (`http://localhost:3001/auth/callback`):**
  OTP request (BFF) → `[DEV-ONLY]` code → verify → `redirect_to_keycloak_handoff` + ticket → Keycloak authorize with
  `login_ticket` + PKCE + the FE's exact `redirect_uri` → **HTTP 302 with an auth code and no password prompt** →
  token exchange → access token with `preferred_username=admin.user@inktavia.com` and the **`Admin`** realm role.
  This exercises exactly the URL/redirect/PKCE contract the FE's `loginWithTicket` + callback rely on.
- **Not yet exercised on-screen:** the pure client-side glue (keycloak-js `createLoginUrl` param assembly and
  `commitKeycloakSession` writing the zustand store), because the browser extension is not connected. This code is
  typecheck-clean and a close mirror of the proven provider-web flow. Manual click-through: start admin-web, go to
  `/login`, enter `admin.user@inktavia.com`, request + enter the code (from identity-api logs), and confirm the browser
  bounces through Keycloak (no password) into the dashboard with admin BFF calls returning 200.

## Regression / hygiene

- provider-web and CargoDry: not modified for this task.
- `.env.local` (dev secrets/values) not committed; only `.env.*.example` templates updated.
- The many other modified files under `inktavia-marine-admin-web` in `git status` are pre-existing uncommitted
  branch work (Faz C etc.), not part of this change.
