# Claude Code Prompt — Provider OTP Login: Keycloak Passwordless Authenticator (Phase 2)

Turn OTP login into a REAL Keycloak session using a custom Authenticator SPI + a single-use signed login ticket.
Full design: `docs/provider-otp-login-keycloak-passwordless-authenticator-design.md`. Prereq: the OTP-login backend
groundwork (BFF/Identity request/verify/resend) must exist. Keycloak stays the sole token/session issuer — **never
fake a token, never impersonate, no token-exchange, no password grant.**

### v1 decision (LOCKED): HMAC-SHA256 shared secret
Use **HMAC-SHA256** for the login ticket (RS256/JWKS is deferred to Phase 3 — do NOT implement it now).
```
Ticket string = base64url(payloadJson) + "." + base64url( HMAC_SHA256(payloadJson, TICKET_SECRET) )
payloadJson   = { "sub", "clientId":"provider-portal", "nonce", "iat", "exp", "jti" }   // exp ≤ iat+120s
```
`TICKET_SECRET` is a single shared secret held by BOTH Identity and the Keycloak SPI, from env/secret store, never
committed. Signature verification is done LOCALLY on both sides (symmetric). Single-use is enforced separately by the
consume callback below (verifying the signature does not prove single-use).

## Step 0 — Build environment + shared secrets
- Confirm a **JDK 17+ and Maven** are available (the SPI is a Java JAR). If not, install them or build the JAR in a
  Maven/Temurin builder image and copy the artifact out. Do NOT proceed to Step 2 without a working Java build.
- Generate two secrets locally (do NOT commit): `TICKET_SECRET`, `CONSUME_SECRET` (e.g. `openssl rand -hex 32`).
- Wire them via env to BOTH sides in docker-compose (identity-api + keycloak) and `.env.example` (blank, documented):
  identity-api: `OtpLoginTicket__Secret=${OTP_LOGIN_TICKET_SECRET}`, `OtpLoginTicket__ConsumeSecret=${OTP_LOGIN_CONSUME_SECRET}`,
  `OtpLoginTicket__KeycloakAuthorizeBaseUrl=http://localhost:8080`, `OtpLoginTicket__SpaRedirectUri=${PROVIDER_WEB_BASE:-http://localhost:3002}/auth/callback`.
  keycloak (SPI reads them as authenticator config or env): the same `OTP_LOGIN_TICKET_SECRET` + `OTP_LOGIN_CONSUME_SECRET`
  + `identityBaseUrl=http://identity-api:8080`. Same secret values on both sides.

## Step 1 — Identity: login-ticket mint + consume
- `IProviderOtpLoginTicketService` (Repository impl): `Mint(sub, clientId)` → the HMAC ticket above + store `jti` in
  `IAizenDistributedCache` (Redis) as single-use, TTL = exp. `Consume(jti)` → **atomic get-and-delete**; returns the
  bound `sub` on the first call, then nothing (409/410) forever after.
- Wire OTP-login `VerifyAsync` success → `Mint` → return
  `{ verified:true, nextAction:"redirect_to_keycloak_handoff", loginTicket:"{ticket}", expiresInSeconds:120 }`.
  **Return the raw `loginTicket`, NOT a full authorize URL.** The SPA builds the Keycloak authorize URL via
  keycloak-js so PKCE `code_verifier`/state are managed correctly (see Step 5); a server-built URL would break the
  callback code exchange. (If a ticket cannot be minted, keep `keycloak_handoff_required`.)
- Endpoint `POST /api/v1/identity/auth/provider-otp-login/consume-ticket { jti }` — **server-to-server only**,
  authenticated by a SEPARATE header secret `X-Otp-Login-Consume-Secret` (env; distinct from `TICKET_SECRET`); NOT
  exposed to browser/BFF. Returns `{ sub }` on first consume, empty/410 after.
- Config/secrets via env: `OtpLoginTicket__Secret` (= TICKET_SECRET), `OtpLoginTicket__ConsumeSecret`,
  `OtpLoginTicket__TtlSeconds` (default 120), `OtpLoginTicket__KeycloakAuthorizeBaseUrl`, `OtpLoginTicket__SpaRedirectUri`.
  No secrets committed.
- Build Identity (5 projects) → 0 errors.

## Step 2 — Keycloak SPI (Java/Maven module under infrastructure/keycloak/spi/)
- `provider-otp-login-authenticator` module (build against Keycloak 25.0.0 SPI APIs):
  - `AuthenticatorFactory` (id `provider-otp-login-ticket`) + `Authenticator`:
    - read `login_ticket` from the auth request (auth session note / client note). If absent → `context.attempted()`
      (fall through to normal login).
    - **verify HMAC-SHA256** over `payloadJson` with `TICKET_SECRET` (constant-time compare) + check `exp` (small
      skew ≤30s) + `clientId` == `provider-portal` + `nonce` present.
    - call Identity `consume-ticket { jti }` (server→server, `X-Otp-Login-Consume-Secret` header) for **single-use**;
      require a 200 with the same `sub` as the payload.
    - resolve the KC user by `sub` → `context.setUser(user); context.success();`
    - any failure → `context.attempted()` / `context.failure(...)` → fall through to normal login (no lockout).
  - Config properties: `identityBaseUrl`, `consumePath`, `ticketSecret` (TICKET_SECRET), `consumeSecret`,
    `allowedClientId` (provider-portal), `maxSkewSeconds`.
  - Package JAR; add compose volume
    `./infrastructure/keycloak/providers/provider-otp-login-authenticator.jar:/opt/keycloak/providers/...jar:ro`.
    `start-dev` loads providers on boot (for prod images run `kc.sh build`).

## Step 3 — Realm flow (kcadm in init.sh / setup script, mirror existing conventions)
- Duplicate the provider-portal browser flow → "Provider OTP Login browser". Add the custom authenticator as the
  first step, requirement **ALTERNATIVE**, so it fires only with a valid `login_ticket`; otherwise the flow falls
  through to the standard Username/Password forms. Bind `provider-portal` to this flow.
- Do NOT change the password login or the Google IdP path. Verify both still work.

## Step 4 — BFF
- Ensure the BFF passes Identity's verify response through unchanged (`nextAction` + `loginTicket`). No token handling
  in the BFF. Update the BFF verify response DTO to carry `loginTicket` (replacing `authorizationUrl`).

## Step 5 — Frontend (small change — build the authorize URL via keycloak-js)
- Update `otpLoginApi.OtpLoginVerifyResponse` + `OtpVerificationPage` (otpLogin mode): on
  `redirect_to_keycloak_handoff`, build the redirect with **keycloak-js** so PKCE/state are managed:
  `const url = keycloak.createLoginUrl({ redirectUri: window.location.origin + '/auth/callback' }); window.location.assign(url + (url.includes('?') ? '&' : '?') + 'login_ticket=' + encodeURIComponent(loginTicket))`.
  Do NOT `window.location.assign` a server-built authorize URL (breaks PKCE code exchange). Keep the blocked banner on
  `keycloak_handoff_required`.
- Add a `/auth/callback` route if `redirectUri` needs a dedicated page; otherwise ensure the existing
  `keycloak.init()` / check-sso callback handling exchanges the returned code, then the post-login guards route by
  status. Add a `/auth/callback` route only if the redirect_uri needs one.

## Step 6 — Build + smoke
- Build: Identity + BFF (dotnet) + the SPI JAR (Maven/JDK). Deploy the JAR + apply the realm flow.
- Smoke (real login): request OTP → verify → redirected to Keycloak → authenticator consumes ticket → SPA receives
  REAL Keycloak tokens → routed by status. Negatives: replayed ticket (jti consumed) fails; expired ticket fails;
  tampered/wrong-clientId ticket fails; password + Google login still work; password recovery still works.

## Step 7 — Docs
- Update `../inktavia-marine-provider-web/docs/provider-auth-login-otp-social-routing-report.md` §6/§14:
  handoff = implemented (broker-ticket authenticator), OTP login now a real login; record SPI version, realm flow,
  and smoke results. Only then flip the OTP-login production verdict to ready.

## Guardrails
- Keycloak issues all tokens/sessions. Identity/BFF never issue a login token. No impersonation / token-exchange /
  password grant. login_ticket is single-use + short-TTL + signed + clientId/sub/nonce-bound. consume-ticket is
  server-to-server only. Authenticator fails closed to normal login. Keep the Java SPI isolated under
  infrastructure/keycloak/spi/. No committed secrets.
```
