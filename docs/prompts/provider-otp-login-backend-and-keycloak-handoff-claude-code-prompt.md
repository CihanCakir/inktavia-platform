# Claude Code Prompt — Provider OTP Login: BFF/Identity Groundwork + Keycloak Handoff Decision

Implement the backend for **OTP login** (sign in with a one-time code) as a SEPARATE flow from password recovery.
The frontend is already wired (`/auth/otp-login` → `/auth/otp-verification?mode=otpLogin`, `otpLoginApi`,
`otpLoginStorage`). Keycloak remains the full IdP and the ONLY token/session issuer. **Do not fake a login.**

## Non-negotiable rules
```
- Keycloak issues all tokens/sessions. BFF/Identity NEVER issue a login token to the browser.
- No password grant. No X-Aizen-User-Token. No admin impersonation (unless explicitly approved + documented).
- Provider Web calls only MarineProvider BFF. BFF delegates to Identity via service token.
- OTP login state MUST be separate from password-recovery reset-token state (no shared entity/columns).
- Non-enumerating responses. Never store raw OTP. Reuse the recovery security/notifier patterns.
- Do not mark OTP login production-ready unless a real Keycloak passwordless/session handoff exists (Part E).
```

## Part A — Keycloak handoff decision (do this FIRST, it gates everything)
Audit whether `provider-portal` has a Keycloak-compatible passwordless/session handoff:
```
1. A custom Keycloak SPI passwordless/OTP authenticator on the provider-portal browser flow
2. A Keycloak magic-link / custom REST authenticator that can consume an externally-verified challenge
3. An explicitly-approved token-exchange (Keycloak feature enabled + client permission) — approval required
```
Current finding (verify): the realm setup (`infrastructure/keycloak/provider-realm/setup-provider-realm.sh`) only
configures clients + Google IdP + roles + audience mappers — **none of the above exists**. So:
- **If none exists (expected):** implement request/verify/resend as groundwork; `verify` returns
  `nextAction = "keycloak_handoff_required"`. Document that production login is blocked until a handoff is built.
- **If one exists / is approved:** wire `verify` success to it and return
  `nextAction = "redirect_to_keycloak_handoff"` + `authorizationUrl` (the SPA already redirects there).
Do NOT invent a handoff or issue tokens yourself.

## Part B — Identity: OTP login domain (separate from recovery)
Reuse the recovery patterns but keep state separate. Two clean options — pick and document:
- (Preferred) a dedicated `ProviderOtpLoginRequestEntity` (mirror `ProviderPasswordRecoveryRequestEntity`:
  `LoginRequestId` unique, `KeycloakSubjectId`, `UserId`, `Channel`, `TargetHash`, `MaskedTarget`, `OtpHash`,
  `OtpSalt`, `OtpExpiresAtUtc`, `Attempts`, `MaxAttempts`, `ConsumedAtUtc?`, `LastSentAtUtc`) + EF config + DbSet +
  **migration** `AddProviderOtpLoginRequest`. No reset-token columns (login has no reset token).
- (Alternative) add a `Purpose` discriminator to the recovery entity — only if it does not entangle reset-token
  state; the dedicated entity is cleaner. Document the choice.
Domain service `IProviderOtpLoginDomainService` (Repository impl) with `RequestAsync/VerifyOtpAsync/ResendAsync`:
- Request: normalize identifier; resolve user (email `FindByEmailAsync`, phone `PhoneNumber` lookup); require an
  **Organizer** profile (`GetActiveProfileIdAsync(userId, Organizer)`); if unresolved → synthetic id, persist
  nothing (non-enumerating); else generate OTP (reuse `PasswordRecoverySecurity`), persist, dispatch via the
  notifier. Reuse `PasswordRecoveryOptions`-style options in a `OtpLoginOptions` (OtpLength/TTL/cooldown/attempts).
- VerifyOtp: attempt-limited + TTL; on success mark consumed and return the **handoff result** from Part A
  (`keycloak_handoff_required` today, or the authorizationUrl if a handoff exists). **No token minted.**
- Resend: cooldown-limited; non-enumerating.

## Part C — Notification delivery
Reuse the Notification pipeline: add `NotificationType.OtpLoginCode` + Email (+ SMS) templates, and publish
`ProviderOtpLoginOtpRequestedMessage` (mirror `ProviderPasswordRecoveryOtpRequestedMessage`) from the OTP-login
notifier → consumer → `SendNotificationCommand`. Redact the code from the persisted `NotificationEntity.Body` (same
as recovery). No SMS provider yet → same documented gap; Email is the path.

## Part D — Identity controller + Abstraction DTOs
Controller `ProviderOtpLoginController` (`Aizen.Modules.InktaviaStore.Controller.V1.Identity`, `[Authorize(Policy=
"IdentityWrite")]`), routes:
```
POST /api/v1/identity/auth/provider-otp-login/request   RequestProviderOtpLoginCommand
POST /api/v1/identity/auth/provider-otp-login/verify     VerifyProviderOtpLoginCommand
POST /api/v1/identity/auth/provider-otp-login/resend     ResendProviderOtpLoginCommand
```
Thin: bind **Request DTOs** (Abstraction `Dto/OtpLogin/`), map to commands, delegate to the domain service. Response
DTOs: `RequestProviderOtpLoginResponse { accepted, loginRequestId, maskedTarget, otpLength, expiresInSeconds,
resendAfterSeconds }`, `VerifyProviderOtpLoginResponse { verified, nextAction, authorizationUrl?, expiresInSeconds,
message }`, `ResendProviderOtpLoginResponse { resent, resendAfterSeconds, expiresInSeconds }`.

## Part E — BFF façade (match the frontend contract exactly)
Controller `ProviderOtpLoginController` at `[Route("api/v1/provider/auth/otp-login")]`, `[AllowAnonymous]`,
`[EnableRateLimiting("pwd-recovery-ip")]` (reuse the existing IP limiter or add `otp-login-ip`). Endpoints
`request` / `verify` / `resend` bind BFF request DTOs and delegate to Identity via new `IProviderIdentityRemoteCall`
methods (Abstraction DTOs). Map Identity responses to the shapes the frontend `otpLoginApi` expects (fields:
`loginRequestId`, `maskedTarget`, `otpLength`, `expiresInSeconds`, `resendAfterSeconds`, and verify: `verified`,
`nextAction`, `authorizationUrl?`, `expiresInSeconds`, `message`). Non-enumerating fallbacks like the recovery
handlers. **No login token in any response.**

## Part F — Config + build + migration
- `OtpLogin__*` options in Identity appsettings/compose (OtpLength/TTL/cooldown/attempts; DevExposeOtp reuse the
  same env gate, default false).
- `dotnet ef migrations add AddProviderOtpLoginRequest -p ...Identity.Repository -s ...Identity` + update DB.
- Build Identity (5) + BFF (2) → 0 errors.

## Part G — Smoke
```
1. /provider/auth/otp-login/request {email} → accepted + loginRequestId + maskedTarget (same for unknown email)
2. read dev OTP from logs (DevExposeOtp local) OR the delivered email
3. /provider/auth/otp-login/verify {loginRequestId, otpCode} → verified=true, nextAction="keycloak_handoff_required"
   (no token). Frontend shows the blocked banner. If a handoff exists, nextAction="redirect_to_keycloak_handoff".
4. Negatives: unknown account (generic, no row), wrong OTP (attempts→lock), expired OTP, resend cooldown.
5. Confirm password-recovery flow still works (separate state).
```

## Part H — Docs
Update `../inktavia-marine-provider-web/docs/provider-auth-login-otp-social-routing-report.md` §5/§6/§14 with:
handoff decision result, endpoints implemented, migration name, build/smoke results. Keep the production verdict at
"OTP login blocked until Keycloak handoff" unless a handoff was actually built + approved.

## Guardrails
- Separate OTP-login state from recovery. Non-enumerating. Never store raw OTP. Never issue a login token.
- Do not change the frontend or the public OTP-login request/response field names the frontend already expects.
- Do not claim OTP login production-ready without a real, tested Keycloak passwordless/session handoff.
```
