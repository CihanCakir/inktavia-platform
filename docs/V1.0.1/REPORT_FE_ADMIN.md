# REPORT_FE_ADMIN — Phase C (admin-web login/OTP UX, BFF-mediated Keycloak)

**Repo:** `inktavia-marine-admin-web`. **Spec:** `docs/V1.0.1/FE_ADMIN_AUTH_ALIGNMENT.md` (Faz C). Mirrors the
MarineProvider provider-web OTP-login pattern against the Admin BFF Phase A+B contract. **provider-web untouched
(reference only); CargoDry business logic untouched.**

## Result — ✅ typecheck 0 errors · auth tests green · OTP flow renders · old username/PIN path removed

The admin login is now **BFF-mediated Keycloak OTP** (passwordless, no browser redirect): identifier (email/phone) →
`/auth/otp-login/request` → code → `/auth/otp-login/verify` → Keycloak handoff. The legacy username+PIN + Identity-HS256
path is gone (it was already dead — the BFF rejects Identity tokens after Phase A+B).

## Changes

**OTP-login API + models (mirror provider `otpLoginApi`):**
- `shared/api/publicHttpClient.ts` (new) — unauthenticated axios client (no auth interceptor), same base URL +
  `X-Client-Type: AdminWeb`.
- `features/auth/api/otpLoginApi.ts` (new) — `request` / `verify` / `resend` via `publicHttpClient` to
  **`/auth/otp-login/{request,verify,resend}`**. DTOs match the A+B `AdminOtpLoginContracts` exactly
  (`{accepted,loginRequestId,maskedTarget,otpLength,expiresInSeconds,resendAfterSeconds,message}` /
  `{verified,nextAction,loginTicket,expiresInSeconds,message}` / `{resent,...}`), with **optional
  `accessToken`/`refreshToken`/`accessTokenExpiredDate`** on verify for forward-compat when the handoff mints tokens.
- `features/auth/model/otpLoginTypes.ts` + `otpLoginStorage.ts` (new) — channel/nextAction types + a sessionStorage
  session store (non-sensitive metadata only; never the OTP/token), keyed `inktavia.admin.otpLoginSession`.
- `shared/api/endpoints.ts` — replaced `AUTH_LOGIN_USERNAME/PHONE/OTP` + `AUTH_OTP_SEND/CHECK` with
  `AUTH_OTP_LOGIN_REQUEST/VERIFY/RESEND`.

**authStore → Keycloak access token (not identityToken):**
- `shared/auth/authStore.ts` — renamed the persisted fields `identityToken/identityRefreshToken/identityExpiresAt` →
  **`accessToken`/`refreshToken`/`accessExpiresAt`** (a Keycloak access token). Same zustand `persist` (`inktavia-auth`,
  localStorage, expiry-discard on rehydrate). `tokenProvider` → `getAccessToken()`/`getRefreshToken()`.
- The authenticated `httpClient` already sends `Authorization: Bearer` (token-management step); `requestHeaders.ts` now
  reads `tokenProvider.getAccessToken()`. All readers updated (`useAuth`, `useNotificationHub` SignalR
  `accessTokenFactory`, `CargoDryQrActivationPage` auth gate, `httpClient` refresh, test).

**authService — Keycloak-token model:**
- `shared/auth/authService.ts` — removed the Identity username/PIN + phone-OTP login methods. Kept `init()`
  (restore-from-store when the token is still valid, else unauthenticated), `logout()`, `getAccessToken()`. Added
  `commitKeycloakSession()` — builds `AuthUser` from the token claims and stores the session.
- `shared/auth/jwtUtils.ts` — `extractRolesFromJwt` now reads the Keycloak **`realm_access.roles`** claim (was the MS
  role claim). `shared/auth/roles.ts` — `ROLES.ADMIN = 'Admin'` (the Keycloak realm role the BFF `AdminPanelAccess`
  policy requires) + `isAdmin()`. `AuthGate`/`ProtectedRoute` gate on the store's `isAuthenticated`; role checks read
  the `Admin` realm role.

**LoginPage → OTP UX (Stitch layout preserved):**
- `pages/public/LoginPage.tsx` — replaced the 2-stage username+PIN→OTP flow with **identifier → code**: channel toggle
  (email/phone), identifier input → request; then a 6-digit code input with a **resend countdown** (`resendAfterSeconds`)
  + resend + back. On verify: if tokens are returned → `commitKeycloakSession` + navigate to the return path; if
  `nextAction === keycloak_handoff_required` → a blocked "handoff not configured" message (never a fake login) — the
  provider-mirror groundwork behavior. Same BrandLogo/Stitch classes.

**Redirect leftovers removed:**
- Deleted `pages/public/AuthCallbackPage.tsx`, `shared/auth/keycloakClient.ts`, `features/auth/api/authApi.ts`
  (unused after cutover — `keycloakClient` had no importers; `authApi.changePassword` had no callers). Removed the
  `AUTH_CALLBACK` route (`routes.tsx` + `routeObjects.tsx`).

**i18n:** `tr/auth.json` + `en/auth.json` — added `otpLogin.*` keys (title/description/tabs/labels/placeholders/submit/
sending/accepted/genericError/handoffMissing) mirroring the provider `otpLogin` keys, and extended `otp.*`
(sentTo/resend/resendIn/back).

## Verification

- **`tsc --noEmit` → 0 errors** (all touched files clean).
- **Auth tests green:** `src/test/authHeaders.test.ts` updated to `getAccessToken`/`Bearer mock-kc-token` → **3/3 pass**;
  all `src/` test files pass (**5 files, 12 tests**).
- **provider-web untouched** (git-clean); CargoDry business logic untouched (only the activation page's token *read* was
  renamed with the store).
- **Grep-clean:** no `identityToken`/`getIdentityToken` remain in `src`; no refs to the removed
  `authApi`/`keycloakClient`/`AuthCallbackPage`/`AUTH_CALLBACK`/username-PIN endpoints.

## Notes / follow-ups

- **`npm run build` (`tsc -b`)** surfaces **4 pre-existing errors in files this task did not touch**
  (`ProvidersPage.tsx`, `providers/ProviderDetailPage.tsx`, `identity/api/mock/approvalsMockData.ts`,
  `useRequestOrganizerRevisionMutation.ts` — null-checks, recharts `Formatter` types, a mock shape, an `ApiResult`
  misuse). Verified pre-existing (present with this task's changes stashed); **none reference anything changed here.**
  Out of scope for Faz C.
- **Groundwork, not yet a live login:** the A+B OTP `verify` returns `keycloak_handoff_required` (no token) until the
  Identity `admin-otp-login` endpoint + Keycloak passwordless handoff are live (Phase D prerequisite). The FE is
  forward-compatible — when `verify` returns Keycloak tokens, `commitKeycloakSession` stores them and the user enters the
  app. Until then the UI shows the blocked handoff message. This is the intended safe cutover.
- **Keycloak (Phase D):** `admin-panel-web` (public) needs the `admin-panel-bff` audience mapper; the OTP handoff must
  mint a Keycloak `Admin`-role token. `test/api/*` integration suites fail on a pre-existing `@test/setup/testClient`
  alias gap (unrelated to auth; untouched).
