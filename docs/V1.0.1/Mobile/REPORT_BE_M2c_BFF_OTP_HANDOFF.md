# REPORT — BE_M2c_BFF_OTP_HANDOFF

Mobile BFF `/mobile/auth/otp/*` endpoints + the native ticket→session handoff. A real OTP flow now
yields Keycloak tokens that `GET /api/v1/mobile/me` (M1) accepts — no browser.

**Verdict: ✅ M2c VERIFIED** — real OTP → Identity ticket → BFF auth-code+PKCE handoff → Keycloak tokens →
`/me` 200 with `mobile_user`.

---

## 1. Files added / changed

**Mobile BFF — Application (`…Mobile.Application`)**
- `Contracts/Auth/OtpLogin/MobileOtpLoginContracts.cs` *(new — `MobileOtp{Send,Verify,Resend}Request/Response`; verify returns `{accessToken, refreshToken, expiresIn, tokenType}`)*
- `Common/Services/ParticipantSessionHandoff.cs` *(new — `IParticipantSessionHandoff`/`ParticipantSessionHandoff`; the OIDC auth-code+PKCE exchange)*
- `Common/RemoteClients/IIdentityRemoteCall.cs` *(added `RequestParticipantOtpLogin`/`VerifyParticipantOtpLogin`/`ResendParticipantOtpLogin` → M2a Identity routes, reusing the shared OTP-login DTOs)*
- `Common/Options/MarineMobileKeycloakOptions.cs` *(added `BffRedirectUri` + `AuthorizeEndpoint`)*
- `Auth/Command/RequestParticipantOtpLogin/{Command,Handler}.cs` *(new)*
- `Auth/Command/VerifyParticipantOtpLogin/{Command,Handler}.cs` *(new — Identity verify → handoff → tokens)*
- `Auth/Command/ResendParticipantOtpLogin/{Command,Handler}.cs` *(new)*
- `DependencyInjection.cs` *(registered `IParticipantSessionHandoff`)*

**Mobile BFF — Web (`…Mobile`)**
- `Controllers/V1/AuthController.cs` *(new — `POST /api/v1/mobile/auth/otp/{send,verify,resend}`, `[AllowAnonymous]`, `[EnableRateLimiting("pwd-recovery-ip")]`)*
- `configuration/appsettings.json` *(added `MarineMobileKeycloak:BffRedirectUri`)*

**Cross-touches (allowed, minimal)**
- `docker-compose.yaml` — `MarineMobileKeycloak__BffRedirectUri` env for `bff-marine-mobile`.
- `infrastructure/keycloak/init.sh` *(git-ignored `*.sh`)* — (a) register the BFF handoff `redirect_uri` on `inktavia-mobile` (add-if-missing); (b) grant the `marine-mobile-bff` **service account** the `identity_read`+`identity_write` realm roles it needs for BFF→Identity calls (the M2b durable client was created bare — this repairs it, idempotently).
- `Modules/Identity/…/Context/Seed/SeedIdentityBase.cs` — DEV seed now resolves the **real** Keycloak subject of `mobile.user@inktavia.com` (best-effort Admin-API lookup via the Identity `IdentityKeycloak` service account) and links it to the participant, replacing the M2a placeholder that blocked M2b's final user-resolution.

**Build:** mobile BFF + Identity build **0 errors**.

---

## 2. Handoff mechanism (authorize → code → token)

The app is native (no browser), so the BFF runs OIDC **authorization-code + PKCE** server-side against the
**public** `inktavia-mobile` client (the client the M2b SPI flow is bound to). No confidential secret is
used for this USER token exchange.

1. Generate `code_verifier` + `code_challenge` (S256) + `state`.
2. `GET {BaseUrl}/realms/inktavia-realm/protocol/openid-connect/auth?client_id=inktavia-mobile&response_type=code&scope=openid&redirect_uri=<BFF redirect>&code_challenge=<c>&code_challenge_method=S256&state=<s>&login_ticket=<ticket>` with an HttpClient that has **auto-redirect OFF**; the SPI validates+consumes the ticket, authenticates the user, and 302s to the redirect_uri carrying `code`. The BFF reads `code` from the 302 `Location` (it does **not** get called back).
3. `POST {BaseUrl}/…/token` `grant_type=authorization_code&client_id=inktavia-mobile&code=<code>&redirect_uri=<same>&code_verifier=<v>` → `{accessToken, refreshToken, expiresIn, tokenType}`.

**Exact redirect_uri used:** `http://localhost:17003/auth/callback` (registered on `inktavia-mobile` by
`init.sh`; identical in the authorize + token requests).

Observed handoff (single hop):
```
hop 0: 302  Location=http://localhost:17003/auth/callback?state=…&code=…   (matches redirect_uri → code extracted)
```

Keycloak/SPI failures map to `AizenBusinessException` → clean HTTP 400 (never 500), no secrets/tokens logged.

> **Implementation note (real bug found + fixed):** the handoff's HttpClient **must** be constructed with
> its own `HttpClientHandler { AllowAutoRedirect = false }`. Configuring auto-redirect off via the
> `IHttpClientFactory` named client did not take effect here — the client auto-followed the authorize 302
> to the (container-unreachable) redirect_uri host and threw `Connection refused`. Constructing the client
> directly in the service fixed it. Also note the token `iss` is `http://localhost:8080/realms/inktavia-realm`
> even though the BFF calls Keycloak at `keycloak:8080` — `KC_HOSTNAME=localhost` fixes the issuer — so it
> matches the M1 inbound validation authority.

---

## 3. send → verify → /me transcript (real round-trip, no browser)

```
POST http://localhost:17003/api/v1/mobile/auth/otp/send  { "identifier":"mobile.user@inktavia.com" }
→ 200 { "header":{"isSuccess":true}, "body":{ "loginRequestId":"…","maskedTarget":"m***@inktavia.com","expiresInSeconds":300 } }

Identity [DEV-ONLY] log:  [DEV-ONLY] OTP login code for m***@inktavia.com: 613376   (masked identifier)

POST …/otp/verify  { "loginRequestId":"…","otpCode":"613376" }
→ 200 { "body":{ "accessToken":"eyJ…","refreshToken":"…","expiresIn":300,"tokenType":"Bearer" } }
```

Decoded access token (masked):
```
iss                 : http://localhost:8080/realms/inktavia-realm
aud                 : [ …, "marine-mobile-bff", "identity-api", … ]     ← includes marine-mobile-bff
sub                 : c1544dac-2cdd-4045-9afc-fe4fa54182f1              ← the real seed user
preferred_username  : mobile.user@inktavia.com
realm role          : mobile_user  ✓
```

**The milestone — feed that token to `/me`:**
```
GET http://localhost:17003/api/v1/mobile/me   Authorization: Bearer <access token>
→ 200 { "body":{ "subject":"c1544dac-…","email":"mobile.user@inktavia.com",
                 "roles":[ …, "mobile_user", … ] } }
```
A real OTP → Keycloak → `/me` round-trip with no browser. ✓

---

## 4. Negatives

| Case | Result |
| --- | --- |
| `/me` with **no** token | **401** |
| verify with **wrong** `otpCode` | **400** business error — `"The code is invalid or has expired. Please request a new code."` (not 500) |
| **reused** (already-consumed) `loginRequestId` | **400** business error (same clean message) |

No secrets or full tokens are logged (OTP identifiers masked; token/ticket never logged; the temporary
hop-debug log was removed from the final source).

---

## 5. init.sh + seed changes (verified)

- `init.sh` re-run (idempotent): registered `http://localhost:17003/auth/callback` on `inktavia-mobile`
  (`redirectUris` now includes it alongside the 3 seed URIs); granted `marine-mobile-bff` service account
  `identity_read`+`identity_write` (verified via `get-roles`); other M2b objects reported already-present.
- Identity seed log: `[SEED] Participant linked to real Keycloak subject (c1544dac…) for mobile.user@inktavia.com.`

---

## 6. Scoped `git status`

My M2c changes are confined to `Bff/src/Marine.Participant.Mobile/**`, `docker-compose.yaml`,
`infrastructure/keycloak/init.sh` (git-ignored `*.sh`), and the Identity dev-seed
`SeedIdentityBase.cs`. Most were auto-committed mid-session into HEAD `f46f644` (13 mobile-BFF files +
`docker-compose.yaml` + `SeedIdentityBase.cs` + `configuration/appsettings.json`); the two files still
showing as modified (`ParticipantSessionHandoff.cs`, `DependencyInjection.cs`) are my final cleanup edits.

Everything else in `git status` is **unrelated parallel work I did not touch** (e.g.
`Modules/Messaging/**`, `docs/V1.0.1/Notification/**`). MarineProvider/AdminPanel BFFs, provider-web,
admin-web, CargoDry, and the Provider/Admin OTP-login code were not modified.

---

## 7. Channel-mapping deviation (flagged)

The task said map phone→`sms`, but the M2a Identity participant domain service keys on `channel=="phone"`
(and `"email"`). To keep phone OTP functional against M2a, the controller maps an identifier containing
`@` → `"email"`, otherwise → `"phone"`. Following the literal `"sms"` would make Identity treat phone
requests as unknown (synthetic, no OTP). The acceptance path uses email; phone is wired consistently with
M2a.

---

## 8. Client follow-up (separate `inktavia-marine-mobile` slice — not implemented here)

The RN app must: point OTP calls at `/api/v1/mobile/auth/otp/{send,verify,resend}`; carry `loginRequestId`
from `send`→`verify`; and read tokens from the normalized `.data` (the envelope adapter from the earlier
FE slice already maps `body`→`data`, so `response.data.data.accessToken`). This is a small client task.

---

## 9. M2d handoff (next)

M2d adds real participant **provisioning** — `ProvisionParticipantFromKeycloak` + profile-by-subject — so
genuinely new Keycloak users (not just the seed) resolve at handoff (the participant equivalent of the
admin `admin-provision` real-subject linkage already in `init.sh`), plus Google/Apple. This slice's DEV
seed-link is the interim stand-in for the seed user only. M2e adds password register/login/recovery/
refresh/logout.
