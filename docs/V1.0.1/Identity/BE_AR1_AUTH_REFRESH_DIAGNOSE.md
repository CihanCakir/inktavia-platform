# BE_AR1 — diagnose the ~1h forced logout + Keycloak config decision (+ fix the admin rehydrate bug)

> **Repos:** `addesso-project` (Identity/BFF), `inktavia-marine-admin-web`, `inktavia-marine-provider-web` (+ mobile
> later). **Diagnose before prescribing:** confirm whether the block is **Keycloak lifespans** or **client wiring**,
> then apply the one already-confirmed client fix (admin discards its refresh token on reload). **No invented Keycloak
> values — recommend, the owner/ops decides. Do not commit.**

## Step 1 — capture the Keycloak realm/client lifespans (read-only)
From the `inktavia-realm` config (realm export JSON in the repo and/or the running Keycloak admin), record the actual
values — these decide whether any client refresh can even work:
- **Access Token Lifespan** (expected short, e.g. 5–15 min; if it's ~1h that alone explains the cadence).
- **SSO Session Idle** and **SSO Session Max** (realm).
- **Client Session Idle** / **Client Session Max** (if set on the clients — override the realm).
- **Refresh token**: whether **"Revoke Refresh Token"** is on and the **max reuse**; the effective refresh-token
  lifetime (bounded by SSO Session Idle/Max).
- Per client (`admin-*`, `marine-provider-*`/portal, `marine-mobile-*`): the flows enabled (standard/direct-access),
  and web origins/redirect URIs.

Put the numbers in the report. **Hypothesis to confirm:** if **SSO Session Idle** (or the refresh-token lifetime) ≈ the
access-token lifespan, the refresh token is dead by first use → every client logs out at ~1h **regardless** of code.

## Step 2 — isolate config vs client (manual refresh-grant test)
Get a token pair (login as a test user), **wait until the access token is expired** (past its `exp`), then call
Keycloak directly:
```
POST {realm}/protocol/openid-connect/token
grant_type=refresh_token&client_id={client}&refresh_token={rt}
```
- **If this returns a fresh access token** → the refresh token is valid past access expiry ⇒ **the bug is client
  wiring** (the clients aren't using the refresh token correctly). Proceed to the per-client fixes (AR2–AR4).
- **If this fails** (`invalid_grant` / expired) → the refresh token is **already dead** ⇒ **the bug is Keycloak
  lifespans**; the config must be adjusted first or no client fix helps.
Record the exact result — this single test decides the whole direction.

## Step 3 — enumerate each client's refresh path (confirm the code findings)
- **admin-web** — reactive 401→`AUTH_REFRESH` interceptor exists (`RefreshBff`→Identity `RefreshLogin`). Confirm: (a)
  `authStore.onRehydrateStorage` nulls **both** tokens when `accessExpiresAt < now`; (b) no proactive refresh timer.
- **provider-web** — Keycloak adapter `updateToken` via `getFreshAccessToken`; the 401 handler logs out with **no**
  `updateToken` retry; confirm the adapter's init (`check-sso`/silent) so a reload re-obtains a token.
- **mobile** — confirm whether the RN FE calls the `RefreshParticipant` BFF and where it stores the refresh token
  (secure storage?) — repo not mounted here, so this is a "to verify in AR4" note.

## Step 4 — decide target lifespans (recommend; owner/ops sets them)
Recommend a coherent set that gives silent refresh room without over-long sessions, e.g.:
- Access Token Lifespan: **~5–15 min** (short — refresh covers the gap).
- SSO Session Idle: **long enough for good UX** (e.g. hours–days, per the product's security posture); SSO Session Max
  a hard ceiling.
- Refresh-token lifetime bounded by SSO Session Idle; if "Revoke Refresh Token" is on, ensure rotation is handled
  client-side (AR2–AR4 cover storing the rotated token).
Present the trade-off (shorter idle = more re-logins but tighter security). **Do not commit invented values** — surface
options; the owner picks. Apply via realm/client config (surfaced, secrets/config not committed).

## Step 5 — fix the one unambiguous client bug now (admin rehydrate discards the refresh token)
In `inktavia-marine-admin-web` `src/shared/auth/authStore.ts` `onRehydrateStorage`: **do not** drop the refresh token
just because the **access** token expired. Change the guard so that on rehydrate:
- If the **access** token is expired **but** a `refreshToken` is present → **keep** `refreshToken` (and `user`), clear
  only the stale `accessToken`/`accessExpiresAt`, and let `authService.init()` / the first request drive a **silent
  refresh** (the reactive interceptor already refreshes on the resulting 401; AR2 adds proactive).
- Only clear the refresh token when it is **itself** absent/known-expired (or on explicit logout).
This makes a returning admin user (after >access-TTL) stay logged in as long as the refresh token is valid — the
correct behavior. (Full proactive refresh + single-flight is AR2.)

## Don't-break / QA
- Steps 1–3 are read-only diagnosis; Step 4 is a recommended config decision (owner applies); Step 5 is a minimal,
  safe admin-web change (keeps a valid refresh token instead of discarding it). No login/logout flow change, no secrets.
- **Verify:** (1) the manual refresh-grant result is recorded (config vs client verdict); (2) after Step 5, an admin
  user whose access token expired while away **reloads and stays logged in** (refresh token used), and only a truly
  dead refresh token routes to login; (3) admin-web `tsc`/lint clean.

## Report
`docs/V1.0.1/Identity/REPORT_AR1.md`: the captured Keycloak lifespans, the **manual refresh-grant isolation result**
(the config-vs-client verdict), the per-client path confirmation, the recommended target lifespans (options + trade-off
for the owner), and the admin rehydrate fix with its stay-logged-in proof. Then AR2 (admin proactive silent refresh) /
AR3 (provider) / AR4 (mobile) per the verdict.
