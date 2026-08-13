# Silent token refresh — stop the ~1h forced logout on all portals (plan)

> **Problem:** when the access token expires (~1h) the user is **logged out** on **all three** clients (admin-web,
> provider-web, mobile) instead of silently getting a new access token from the refresh token. Users shouldn't re-login
> every hour. **Diagnostic-first** (the same discipline that corrected the accept-gate hypothesis): confirm whether the
> block is **Keycloak session/token lifespans** or **client refresh wiring** (or both) before prescribing per-client
> fixes. **Do not commit. No invented Keycloak lifespan values** — those are the ops/owner call; I recommend sane
> defaults and surface the trade-off.

## Confirmed from code (per client — they use TWO different refresh strategies)
- **admin-web** — BFF-proxied refresh. `httpClient` has a **reactive** 401→refresh interceptor that POSTs
  `AUTH_REFRESH` (`RefreshBff` → Identity `RefreshLogin`) with the stored `refreshToken`, sets the new session, and
  retries once. **Two gaps:** (1) `authStore.onRehydrateStorage` **discards BOTH tokens** when `accessExpiresAt < now`
  on load — so a user returning after the access-token TTL is logged out **even though the refresh token is still
  valid**; (2) there is **no proactive** refresh (only reactive on a 401), so the first call after expiry always eats a
  401 (and across a reload, gap #1 nukes the refresh token first).
- **provider-web** — Keycloak **JS adapter**. `getFreshAccessToken()` (request interceptor) can call
  `keycloak.updateToken(minValidity)` to silently refresh; **but** the **provider BFF has NO refresh endpoint** (by
  design — it uses the adapter), and the response interceptor on 401 does a **single** `clearAuthState()+login()` with
  **no `updateToken` retry**. If the adapter's refresh/SSO session is short or a reload loses the token, it logs out.
- **mobile** — the `RefreshParticipant` **BFF command exists** (`RefreshParticipantCommandHandler`); the RN FE wiring
  (secure refresh-token storage + proactive/reactive refresh) is **unverified** (repo not mounted) — treat as
  "confirm + wire".

## The common suspect — Keycloak lifespans (must be captured first)
All three failing at the **same ~1h** points at a **realm/client config** ceiling: if **SSO Session Idle**, **Client
Session Idle**, or the **refresh-token lifespan** ≈ the access-token lifespan (~1h), then **no client-side refresh can
survive** — the refresh token is dead by the time it's used. Client code fixes are necessary but **not sufficient**
until this is confirmed. So AR1 captures the numbers and isolates config-vs-client with a manual refresh-grant test.

## Phases
- **AR1 — diagnose + Keycloak config decision** *(→ `BE_AR1_AUTH_REFRESH_DIAGNOSE.md`)*: capture the realm/client token
  & session lifespans; run a **manual `grant_type=refresh_token`** at T+access-expiry to prove whether the refresh token
  is still valid; enumerate each client's refresh path; decide target lifespans (access short, SSO idle long enough for
  UX, refresh valid); **fix the one unambiguous client bug already found** (admin rehydrate discarding the refresh
  token).
- **AR2 — admin-web** *(→ `FE_AR2_ADMIN_SILENT_REFRESH.md`)*: keep the refresh token on rehydrate (only drop it when
  the **refresh** token is expired/absent, not merely the access token); add **proactive** silent refresh (schedule a
  refresh shortly before `exp`); keep the reactive 401 path as a fallback; **single-flight** refresh (no stampede);
  store the rotated refresh token if Keycloak rotates it.
- **AR3 — provider-web (+ provider BFF if standardizing)** *(→ `FE_AR3_PROVIDER_SILENT_REFRESH.md`)*: make the Keycloak
  adapter refresh robust — proactive `updateToken(minValidity)` ahead of expiry, and on a 401 **try `updateToken` once
  before** `login()`; ensure the token survives a reload (`check-sso` / silent SSO). Only add a provider-BFF refresh
  endpoint if the decision is to move provider off the adapter onto the BFF-proxied pattern (likely **not** needed —
  the adapter already refreshes when the realm session allows it).
- **AR4 — mobile** *(→ `FE_AR4_MOBILE_SILENT_REFRESH.md`)*: wire `RefreshParticipant` in the RN app — persist the
  refresh token in **secure storage**, refresh **proactively** before expiry + **reactively** on 401 (single-flight),
  store the rotated refresh token, and only route to login when the **refresh** call fails.

## Cross-cutting correctness (all clients)
- **Single-flight:** concurrent 401s / near-expiry calls trigger **one** refresh, others await it (no refresh
  stampede, no refresh-token reuse races — relevant if "Revoke Refresh Token" is on).
- **Rotation:** if Keycloak returns a new refresh token, **persist it**; using a rotated-out refresh token fails.
- **Clock-skew margin:** refresh at `exp − margin` (e.g. 30–60s), not exactly at `exp`.
- **Only logout when the refresh fails** — never on a mere access-token expiry.

## Don't-break / QA
- No secrets committed/printed; Keycloak lifespan values are the ops decision (recommend defaults, don't hardcode
  invented ones). Additive client changes; the login/logout flows themselves unchanged.
- Acceptance (per client): leave the app idle past the access-token TTL → the next action **succeeds via silent
  refresh** (no login redirect); a full reload after the access TTL (but within the refresh/SSO window) **stays logged
  in**; only an **expired/absent refresh token** (or a real logout) sends the user to login.

## Reports
Per phase `docs/V1.0.1/Identity/REPORT_AR*.md`. AR1 records the captured lifespans, the manual-refresh isolation result
(config vs client), and the admin rehydrate fix; AR2–AR4 the per-client silent-refresh wiring + the idle-past-expiry
proof.
