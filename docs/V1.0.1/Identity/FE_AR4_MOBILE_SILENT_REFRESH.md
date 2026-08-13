# FE_AR4 — mobile (RN/Expo) silent refresh via RefreshParticipant

> **Repo:** `inktavia-marine-mobile` (owner app). The BFF refresh endpoint **exists and is correct**
> (`RefreshParticipantCommandHandler` → `IParticipantKeycloakAuthClient.RefreshAsync` → Keycloak `refresh_token` grant
> on the public `inktavia-mobile` client). AR4 wires the **RN FE** to actually use it: proactive + reactive silent
> refresh, secure token storage, single-flight, rotation. With the config fix (idle 7 days > access 15 min) the refresh
> token now outlives the access token. **Repo not mounted here → spec against the BFF contract + the app's existing
> patterns; grep the app before writing new primitives.** **Do not commit.**

## BFF contract (live)
- `POST {mobile-bff}/auth/refresh` (RefreshParticipant) — body `{ refreshToken }` → `{ accessToken, refreshToken,
  expiresIn, tokenType }`, or **401** when the refresh token is invalid/expired (handler returns null). Keycloak
  **rotates** the refresh token — the response carries the new one.
- Login (password / OTP / social) already returns the initial `{ accessToken, refreshToken, expiresIn }` from the same
  Keycloak-backed client.

## What to wire in the RN app
1. **Secure storage for the refresh token.** Store `refreshToken` (and the access token) in the platform secure store
   (`expo-secure-store` / Keychain / Keystore — use whatever the app already uses for the current token; **not**
   AsyncStorage in plaintext). Persist across app restarts so a returning user (within the 7-day idle) stays logged in.
2. **A refresh coordinator (single-flight).** One module that owns `refreshSession()`:
   - Calls `POST /auth/refresh` with the stored refresh token.
   - **Coalesces** concurrent callers (a proactive timer + any 401) into **one** in-flight request (no stampede — a
     rotating refresh token must not be spent twice).
   - On success: persist the **rotated** `refreshToken` + new `accessToken` + compute the new `exp`.
   - On failure (401/null): clear the session and route to the login screen — **only here**, never on a mere
     access-token expiry.
3. **Proactive refresh.** Schedule a refresh at **`exp − margin`** (e.g. 60 s), and also refresh on **app foreground**
   (`AppState` → `active`) / regained connectivity if within the margin. (Mobile apps background for long stretches; a
   foreground check is the mobile equivalent of provider-web's `onTokenExpired`.)
4. **Reactive on 401.** The app's HTTP client interceptor: on 401, `await refreshSession()` once, then retry the
   original request with the new token; if refresh fails, go to login. Wire it into the existing api client (the same
   one MO1–MO11 use), not a new fetch layer.
5. **Realtime + push.** Ensure the **SignalR** connection (MO9b realtime edge) uses a token factory that pulls the
   current access token via the coordinator (fresh on each (re)connect), so the socket survives past the 15-min access
   lifespan. Push registration (device token) is unaffected, but a screen opened from a push tap must also ride the
   refreshed session.

## Cross-cutting (same rules as AR2/AR3)
- **Single-flight**, **persist the rotated refresh token**, **clock-skew margin**, **only logout when the refresh
  itself fails**. Login/logout screens unchanged.

## Don't-break / QA
- Additive: secure storage + coordinator + interceptor wiring + timer/foreground hooks. No change to login/OTP/social
  flows beyond storing the tokens the coordinator needs.
- **Tests / checks:** (1) `tsc --noEmit` clean; (2) single-flight (N concurrent 401s → one `/auth/refresh` call);
  (3) rotation — the stored refresh token equals the last response's; (4) proactive timer fires before `exp`;
  (5) foreground-after-expiry refreshes; (6) a failed refresh (revoked token) routes to login exactly once.
- **Live walkthrough (owner `qa.owner.aug5@inktavia.com`, after the config fix + stack up):** sign in → background the
  app past the 15-min access lifespan → foreground → **still logged in**, data loads (silent refresh); an open realtime
  screen keeps receiving; force an expired/revoked refresh token → one clean re-login. (Device-only steps — camera/push
  — as noted in MO11c stay manual.)

## Report
`docs/V1.0.1/Identity/REPORT_AR4.md`: the secure-storage + coordinator + interceptor wiring, the proactive/foreground +
single-flight + rotation proof, the realtime-token-factory hook, and the background-past-expiry-stays-logged-in
walkthrough. **This closes the AR silent-refresh track across all three portals.** Cross-link
[[auth_refresh_two_token_systems]].
