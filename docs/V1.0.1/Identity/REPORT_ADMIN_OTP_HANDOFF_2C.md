# REPORT — Admin OTP Handoff, Kickoff 2c (admin-web callback fix)

**Repo:** `inktavia-marine-admin-web` (FE only). **Scope:** land the Keycloak session in the app after the OTP →
Keycloak login-ticket handoff, mirroring provider-web. Backend/Keycloak/provider-web/CargoDry untouched.

## Outcome

✅ **The FE defects are fixed and verified end-to-end.** After the Keycloak handoff, the session is now durably
committed to `authStore` **at bootstrap** (before any guard/`clearAuth` can run), the admin dashboard renders
authenticated as **Admin User**, and `ProtectedRoute` admits the `Admin` realm role.

⛔ **One remaining blocker is outside FE scope:** the AdminPanel BFF (`bff-adminpanel`, `:17001`) returns **HTTP 401**
for the (well-formed) Keycloak access token on its data endpoints. This is a BFF token-authentication/config issue,
not an admin-web issue — see "Backend finding" below. The admin-web changes are complete regardless.

## Files changed

| File | Change | Defect addressed |
|------|--------|------------------|
| `src/shared/auth/authService.ts` | `init()` now resolves a live Keycloak OAuth callback **first** (`resolveKeycloakSession`) and commits the session **before** the persisted-token check / `clearAuth()` path. Gated by `keycloakConfigured` + `urlHasOAuthResponse()`. | A + B |
| `src/shared/auth/keycloakClient.ts` | Added `responseMode: 'query'` to `keycloak.init(...)` so the authorization `code` returns as a `?code=` query param (survives `createBrowserRouter`) instead of the default URL fragment. keycloak stores this and reuses it for the `createLoginUrl` handoff redirect. | B (+ 3) |
| `src/pages/public/AuthCallbackPage.tsx` | Slimmed to match provider-web: it no longer performs the code exchange itself. Bootstrap owns the exchange; the page just waits for `isInitialized` then routes (`isAuthenticated` → dashboard, else `/login`). | B (+ 4) |

Net change to `src/shared/api/httpClient.ts`: **none** (temporary `[HTTP-DEBUG]` instrumentation was added while
diagnosing and has been fully removed — see "Diagnostics removed").

### Why these fixes (mapping to the two defects)

- **Defect A — bootstrap `authService.init()` race wiped the committed session.** Previously `init()` restored a
  persisted token or immediately called `clearAuth()`, racing the callback page's async commit. Now the Keycloak
  callback resolution + `commitKeycloakSession` happen as the **first** step inside `init()`; the `clearAuth()` path
  is only reached when there is no OAuth response and no valid persisted token. A valid Keycloak login survives
  bootstrap.
- **Defect B — the OAuth callback was processed too late (in `AuthCallbackPage.useEffect`).** The code exchange now
  runs during bootstrap, before routes render and before the guard evaluates. With `responseMode: 'query'` the
  `code` is a query param that the SPA router preserves.

### Important design note (loop avoidance)

`resolveKeycloakSession()` runs keycloak-js **only when the URL actually carries an OAuth response**
(`urlHasOAuthResponse()` → `?code=&state=`). It intentionally does **not** run `check-sso` on every normal load.
Reason (observed live): with a pre-existing Keycloak SSO session, keycloak-js `check-sso`'s silent iframe fails when
third-party cookies are blocked and falls back to a **full-page redirect**, which re-authenticates and reloads on an
infinite loop, blanking the app. Gating on the presence of an OAuth response keeps day-to-day BFF/JWT `identity`
behavior fully intact and eliminates that loop while still landing the callback session. Identity-only deployments
(no Keycloak env) skip the path entirely via `keycloakConfigured`.

## Acceptance / verification

- `npm run typecheck` — **clean** (the three changed files compile with no errors). Note: `npm run build`
  (`tsc -b`) surfaces **pre-existing, unrelated** type errors in `ProvidersPage.tsx` / `ProviderDetailPage.tsx`
  that are not touched by this work and predate this branch.
- **Live end-to-end run** (services up: admin-web `:3000`, keycloak `:8080`, `bff-adminpanel` `:17001`,
  `identity-api`). Masked transcript:
  1. `http://localhost:3000/login` → enter `admin.user@inktavia.com` → request code. Backend logs
     `[DEV-ONLY] OTP login code for a***@inktavia.com: ******`.
  2. Enter code → verify. First verify returns `redirect_to_keycloak_handoff` + `loginTicket`; FE
     `loginWithTicket` redirects to Keycloak; Keycloak SPI authenticates (no password) and 302s to
     `/auth/callback?...&code=...&state=...` (query mode ✅).
  3. Bootstrap `authService.init()` → `resolveKeycloakSession()` → `initKeycloak()` completes the PKCE code
     exchange; console logs `[AuthService] ✅ Keycloak session resolved at bootstrap — committing`. `authStore`
     is populated with the Keycloak access token (`preferred_username=admin.user@inktavia.com`,
     `realm_access.roles` includes `Admin`).
  4. The app renders `/app/dashboard` **authenticated as "Admin User"** — `ProtectedRoute`/role check admit the
     `Admin` realm role.
- **Login page no longer loops** on a fresh load (verified the previous infinite reload/blank-screen is gone).
- provider-web + CargoDry: not modified.

## Backend finding (out of FE scope — needs a backend/BFF change)

With the Keycloak session committed and the dashboard rendered, the first authenticated BFF calls return **401**:

```
GET /api/v1/admin-panel/dashboard/overview        → 401   (Authorization: Bearer eyJhbGci…RS256)
GET /api/v1/admin-panel/notifications/unread-count → 401   (Authorization: Bearer eyJhbGci…RS256)
```

The Keycloak Bearer token **is** sent correctly and is well-formed (RS256; `preferred_username =
admin.user@inktavia.com`; `realm_access.roles` includes `Admin` + the full permission set). The BFF rejects it at
the **authentication** layer (HTTP 401, not 403 — so this is token *validation* failing, upstream of any
`[Authorize(Policy = "AdminPanelAccess")]` policy). The admin-web `httpClient` 401 handler then attempts a refresh
against the BFF `/auth/refresh` endpoint with the Keycloak refresh token, which the BFF's `RefreshCommandHandler`
rejects (`Refit.ApiException: 400 (Bad Request)`), so it clears auth and returns to `/login`. That refresh-then-logout
chain is the "bounces back to /login" symptom — but its **root cause is the BFF 401**, which is backend territory the
kickoff scoped out ("Backend + Keycloak are DONE — do NOT touch them").

**Recommended backend check (not done here — FE-only mandate):** confirm `bff-adminpanel`'s JWT bearer authentication
is configured to accept tokens from this Keycloak realm — matching `Authority`/`issuer`
(`http://localhost:8080/realms/inktavia-realm`), the expected `audience` (the token's `aud`/`azp` for
`client_id=admin-panel`), and the realm's signing key. Since the failure is 401 (authentication), the fix is at the
JWT validation config, not the `AdminPanelAccess` authorization policy.

## Diagnostics removed

- No `enableLogging` flag was left in `keycloak.init` (none added permanently).
- The temporary `[HTTP-DEBUG]` `console.log` added to `src/shared/api/httpClient.ts` during diagnosis has been
  **removed**; that file is back to its original behavior (net diff = 0).
- The `[AuthService]` logs are the pre-existing, dev-only (`import.meta.env.DEV`) logger already in the codebase and
  were not added by this work.
