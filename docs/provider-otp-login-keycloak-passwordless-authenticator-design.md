# Design — Provider OTP Login: Keycloak Passwordless Authenticator (Broker-Ticket)

This is the mechanism that turns the already-built OTP-login **groundwork** into a **real Keycloak session** —
securely, Keycloak-native, without faking tokens. It is the missing "handoff" from
`provider-auth-login-otp-social-routing-report.md` §6.

## 1. Goal / non-goals
```
Goal: An Identity-verified OTP (email/phone) becomes a genuine Keycloak browser session for provider-portal,
      so keycloak-js exchanges a real auth code → real tokens. Keycloak stays the sole token/session issuer.
Non-goals: password grant, admin impersonation, token-exchange, Identity/BFF issuing login tokens, faking a session.
```

## 2. Chosen approach: custom Authenticator SPI + single-use signed login ticket
Keycloak has **no built-in "externally-verified OTP → session"**. The correct native pattern is a **custom browser
Authenticator** (Keycloak SPI, Java JAR) that trusts a **short-lived, single-use, signed login ticket** minted by
Identity after OTP verification. Identity is the OTP authority; **Keycloak is the session authority** and calls
`context.setUser(user); context.success();` only after validating the ticket. This is a trusted-broker passwordless
login — **not** impersonation (no admin token) and **not** token-exchange.

### Why not the alternatives
```
- Admin impersonation / token-exchange: minting a user session from a service token = impersonation; task forbids
  it without explicit approval; broad blast radius. Rejected.
- Password grant with a server-side password: reintroduces a local password + direct grant. Forbidden. Rejected.
- Keycloak native email-OTP authenticator: keeps OTP inside Keycloak, but our OTP authority is Identity (phone+email,
  Notification delivery, rate limiting already built). A broker ticket reuses all of it. Chosen.
```

## 3. End-to-end sequence
```
1. Provider Web /auth/otp-login → BFF /provider/auth/otp-login/request → Identity: generate+send OTP (built).
2. Provider Web /auth/otp-verification?mode=otpLogin → BFF /provider/auth/otp-login/verify → Identity verify.
3. On verify success, Identity mints a LOGIN TICKET:
     ticket = base64url( header.payload.signature )
     payload = { sub, clientId:"provider-portal", nonce, iat, exp(≤120s), jti }
     - signed with Identity's private key (RS256) OR HMAC with a shared secret Keycloak also holds
     - jti stored in Redis as SINGLE-USE (unconsumed), TTL = exp
   Identity returns: { verified:true, nextAction:"redirect_to_keycloak_handoff",
                       authorizationUrl: "<keycloak>/realms/inktavia-realm/protocol/openid-connect/auth?...&login_ticket=<ticket>",
                       expiresInSeconds:120 }
4. Provider Web (already implemented) redirects: window.location.assign(authorizationUrl). This is a NORMAL
   Auth Code + PKCE authorize request for provider-portal, plus a login_ticket query param.
5. Keycloak runs the provider-portal browser flow. The custom "Provider OTP Login" authenticator:
     - reads login_ticket (from the authorization request → auth session note)
     - validates signature + exp + clientId + nonce
     - calls Identity POST /provider-otp-login/consume-ticket {jti} (server→server) to atomically CONSUME (single-use)
       and resolve the user; OR verifies signature locally + consumes via a Keycloak single-use cache
     - resolves the Keycloak user by sub → context.setUser(user); context.success()
     - on any failure → context.attempted()/failure() → falls through to normal username/password
6. Keycloak issues the auth code → redirects to the SPA callback → keycloak-js exchanges it → REAL Keycloak tokens.
```

## 4. Components to build
```
A. Identity (.NET):
   - Login-ticket service: mint(sub, clientId) → signed ticket + Redis single-use jti; consume(jti) → atomic, once.
   - Wire OTP-login VerifyAsync success → mint ticket → build authorizationUrl → return redirect_to_keycloak_handoff.
   - Endpoint POST /api/v1/identity/auth/provider-otp-login/consume-ticket (service-secret / mTLS; called by KC only).
   - Key material: RS256 private key (or shared HMAC secret) from secret store; publish public key/JWKS if RS256.
B. Keycloak SPI (Java, separate small Maven module):
   - AuthenticatorFactory + Authenticator "provider-otp-login-ticket":
       * reads login_ticket, validates (sig/exp/clientId/nonce), consumes via Identity callback, setUser+success.
       * config: Identity base URL, consume endpoint, public key / shared secret, allowed clientId, max skew.
   - Package as JAR; mount into /opt/keycloak/providers/ (compose volume) so start-dev registers it on boot.
C. Realm flow (config, not code):
   - Duplicate the provider-portal "browser" flow → "Provider OTP Login browser".
   - First step = the custom authenticator as ALTERNATIVE; if it fails/absent, fall through to the standard
     Username/Password + (existing) forms. Bind provider-portal to this flow (or trigger it only when login_ticket
     is present so password login is unaffected). Google IdP path is unaffected.
D. BFF: pass through Identity's verify response (nextAction + authorizationUrl) — already delegating.
E. Frontend: ALREADY DONE — OtpVerificationPage redirects to authorizationUrl on redirect_to_keycloak_handoff.
```

## 5. Security model
```
- Ticket: single-use (Redis/KC single-use cache on jti), short TTL ≤120s, signed (RS256 preferred), bound to
  sub + clientId + nonce. Replay-proof (jti consumed once), tamper-proof (signature), audience-bound (clientId).
- Keycloak sets the user ONLY after Identity attests OTP verification via the consumed ticket. No token is minted by
  Identity/BFF; Keycloak issues the session. No admin token, no impersonation, no token-exchange.
- The consume endpoint is server-to-server only (KC → Identity), authenticated by a shared secret/mTLS; never exposed
  to the browser/BFF. login_ticket is one-time and useless after consume.
- Rate limiting + non-enumeration from the OTP layer still apply. Ticket mint only after a verified OTP.
- Failure of the authenticator falls back to normal login (no lockout of password users).
```

## 6. Packaging + deploy in this compose
```
- Keycloak runs quay.io/keycloak/keycloak:25.0.0, start-dev --import-realm, no providers mount yet.
- Add a volume: ./infrastructure/keycloak/providers/provider-otp-login-authenticator.jar:/opt/keycloak/providers/...jar:ro
  (start-dev auto-builds/loads providers on boot). For production images, run `kc.sh build` with the provider present.
- Import/patch the realm to add the "Provider OTP Login browser" flow + the authenticator config, or apply via
  kcadm in init.sh (mirror setup-provider-realm.sh conventions).
- Config/secrets via env: Identity base URL, consume-endpoint secret, signing key/public key. No secrets committed.
```

## 7. Phased plan
```
Phase 1 (backend groundwork): the OTP-login BFF/Identity endpoints (separate prompt) — verify returns
        keycloak_handoff_required. Ships first; login still blocked. (Frontend already handles it.)
Phase 2 (this design): Identity ticket mint/consume + Keycloak SPI JAR + realm flow → verify returns
        redirect_to_keycloak_handoff + authorizationUrl → REAL login. Flips production-readiness.
Phase 3 (hardening): RS256 + JWKS rotation, KC single-use cache instead of callback (optional), SMS channel,
        observability + alerts on ticket anomalies.
```

## 8. Acceptance / testing
```
- Happy path: request OTP → verify → redirected to Keycloak → authenticator consumes ticket → SPA gets real tokens
  → post-login status routing works (dashboard/onboarding/status).
- Replay: reusing a login_ticket fails (jti consumed). Expired ticket fails. Wrong clientId/sub/signature fails.
- Password login + Google login still work (authenticator only fires with a valid ticket; else falls through).
- No token issued by Identity/BFF; verify never returns a Keycloak token to the browser.
- Password recovery flow unaffected (separate state).
Do not mark OTP login production-ready until Phase 2 is built, the SPI is deployed, and the above pass.
```

## 9. Risks / notes
```
- New language surface: a small Java/Maven SPI module in an otherwise .NET repo — keep it isolated under
  infrastructure/keycloak/spi/. Requires a JDK build in CI/local.
- Keycloak version pinning: build the SPI against 25.0.0 APIs; re-verify on KC upgrades.
- Clock skew: allow small exp skew; keep TTL short.
- If a Java SPI is undesirable, the only other native option is Keycloak's own email-OTP authenticator (moves OTP
  authority into Keycloak, loses phone + Identity Notification reuse) — documented alternative, not recommended.
```

See the executable build prompt: `docs/prompts/provider-otp-login-keycloak-authenticator-claude-code-prompt.md`.
