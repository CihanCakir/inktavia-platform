# REPORT — BE_M2f_PASSWORD_RECOVERY

Participant (mobile) password recovery: **forgot → verify-otp → set-new-password**, added by mirroring the
existing `ProviderPasswordRecovery` vertical (Identity domain + BFF façade) **byte-for-byte**, reusing the
shared crypto / options / Keycloak-password service / notifier / DTOs / domain result models. After this
slice, the three client screens (ForgotPassword / VerificationCode / SetNewPassword) can be wired to real
endpoints (separate FE follow-up).

**Verdict: ✅ M2f VERIFIED** — forgot→verify→reset→login-with-new all green end-to-end; the password actually
changed in Keycloak (new pw → 200, non-current pw → 401); anti-enumeration + every negative path return clean
business errors (no 500s).

---

## 1. What was built (mirror of the provider vertical)

### Identity (`Modules/Identity/**`, namespaces `Aizen.Modules.InktaviaStore.*` / `Aizen.Modules.Identity.*`)

**New — the participant reset-request store (mirror of the provider entity):**
- `…Domain/Entities/PasswordRecovery/ParticipantPasswordRecoveryRequestEntity.cs` — same fields as the provider
  entity, `ProviderProfileId` → `ParticipantProfileId`.
- `…Domain/Entities/PasswordRecovery/ParticipantPasswordRecoveryRequestEntityConfiguration.cs` — table
  `participant_password_recovery_requests`, unique index on `ResetRequestId`.
- `…Repository/Context/InktaviaStoreIdentityDbContext.cs` *(edited)* — `DbSet<ParticipantPasswordRecoveryRequestEntity>
  ParticipantPasswordRecoveryRequests` + `ApplyConfiguration(new ParticipantPasswordRecoveryRequestEntityConfiguration())`.
- **EF migration** `20260805192938_AddParticipantPasswordRecoveryRequest` (+ `.Designer` + snapshot) — one
  `CreateTable` + one unique `CreateIndex`, nothing else.

**New — the domain service (mirror; only the gate + entity/DbSet + rate-limit prefix differ):**
- `…Domain/Interface/Service/IParticipantPasswordRecoveryDomainService.cs` — same 4-method contract, reuses the
  shared `Domain.Model.PasswordRecovery` result models.
- `…Repository/Service/PasswordRecovery/ParticipantPasswordRecoveryDomainService.cs` — deltas vs. provider:
  profile gate `WorkshopRoleContext.Participant` (the provider vertical gates on `Organizer`; there is **no**
  `Provider` enum member — see §5), persists to `_db.ParticipantPasswordRecoveryRequests`, rate-limit key prefix
  `participant:pwreset:rl:`. **Kept in the same namespace** `…Repository.Identity.Service.PasswordRecovery` as the
  provider service so the existing per-namespace log-level override surfaces its `[DEV-ONLY]` line.

**New — CQRS (thin, mirror; reuse the shared Abstraction DTOs):**
- `…Application/Auth/Command/PasswordRecovery/RequestParticipantPasswordRecovery/{Command,Handler,Validator}.cs`
- `…/VerifyParticipantPasswordRecoveryOtp/{Command,Handler,Validator}.cs`
- `…/ResetParticipantPassword/{Command,Handler,Validator}.cs`
- `…/ResendParticipantPasswordRecoveryOtp/{Command,Handler,Validator}.cs`
  (each `AizenCommand<T>` / `AizenCommandHandler<T>` / `AizenValidator<T>`; handlers depend only on
  `IParticipantPasswordRecoveryDomainService`; responses reuse the shared `…Abstraction.Dto.PasswordRecovery`
  response types).

**New — the controller:**
- `…Identity/Controller/V1/Identity/ParticipantPasswordRecoveryController.cs` —
  `[Route("api/v1/identity/auth/participant-password-recovery")]`, `[Authorize(Policy="IdentityWrite")]`,
  actions `request` / `verify-otp` / `reset` / `resend-otp` binding the **shared** `…PasswordRecovery.*Request`
  DTOs (mirror of `ProviderPasswordRecoveryController`).

**Edited — DI:**
- `…Repository/DependencyInjection.cs` — added
  `services.AddScoped<IParticipantPasswordRecoveryDomainService, ParticipantPasswordRecoveryDomainService>();`
  next to the provider registration. `PasswordRecoveryOptions`, `IIdentityKeycloakPasswordService`, and the
  notifier were **already registered** — not re-registered (reused).

### Mobile BFF (`Bff/src/Marine.Participant.Mobile/**`)

- `…Application/Common/RemoteClients/IIdentityRemoteCall.cs` *(edited)* — added the **4 participant** methods
  (`RequestParticipantPasswordRecovery` / `VerifyParticipantPasswordRecoveryOtp` / `ResetParticipantPassword` /
  `ResendParticipantPasswordRecoveryOtp`) pointing at the new Identity `participant-password-recovery/*` routes,
  reusing the shared Identity DTOs (same convention as the existing participant-OTP methods).
- `…Application/Contracts/Auth/Password/MobilePasswordRecoveryContracts.cs` *(new)* — the client-facing request +
  response shapes (see §6).
- `…Application/Auth/Command/{ForgotParticipantPassword, VerifyParticipantPasswordOtp, SetNewParticipantPassword,
  ResendParticipantPasswordOtp}/{Command,Handler}.cs` *(new)* — delegate to `IIdentityRemoteCall`. `Forgot`
  derives the channel from the identifier shape (`'@'` → email, else phone), mirroring the OTP-login `send`
  endpoint; anti-enumeration handlers return a neutral shape on any failure; `Verify`/`SetNew` throw
  `AizenBusinessException` on wrong/expired code or reused/expired token.
- `…/Controllers/V1/AuthController.cs` *(edited)* — added `forgot-password` / `verify-otp` / `set-new-password`
  / `forgot-password/resend` at absolute routes `/api/v1/mobile/auth/*` (the class already carries
  `[AllowAnonymous]` + `[EnableRateLimiting("pwd-recovery-ip")]`, so recovery inherits the IP rate-limit).

**Build:** Identity host **0 errors**; mobile BFF host **0 errors**. Migration applies on identity-api startup
(`SeedIdentityAsync → MigrateAsync`) — confirmed the table + `__EFMigrationsHistory` row exist.

---

## 2. Reuse — nothing crypto/security was duplicated

The participant vertical reuses, **unchanged**:
- `PasswordRecoverySecurity` (PBKDF2 OTP/token hash + `MaskEmail`/`MaskPhone`) — shared static.
- `PasswordRecoveryOptions` (same `"PasswordRecovery"` config section / options object).
- `IIdentityKeycloakPasswordService` (`ResetPasswordAsync` + `RevokeSessionsAsync`) — shared registration.
- The recovery **notifier** `IProviderPasswordRecoveryNotifier` (Logging / MessageBus impls) — injected directly.
- The shared `Abstraction.Dto.PasswordRecovery` request/response DTOs and the `Domain.Model.PasswordRecovery`
  result models — no participant variants were needed.

Only the reset-request **entity/table** and the **domain service** are participant-specific (as required — the
persisted request and the profile gate are the only role-specific parts).

---

## 3. End-to-end transcript (all against `http://localhost:17003`, seed `mobile.user@inktavia.com`, Id 100013)

Verification env on identity-api: `PASSWORD_RECOVERY_DELIVERY_MODE=Logging`,
`PASSWORD_RECOVERY_DEV_EXPOSE_OTP=true`, `PASSWORD_RECOVERY_LOG_LEVEL=Debug`.

**1) forgot-password** `{ "identifier":"mobile.user@inktavia.com" }` → **200**
```json
{ "header": { "isSuccess": true }, "body": {
  "resetRequestId": "4oIscBBPbwEafghQzMz08BD7C2cyTpjk5BBd_LeAVhk",
  "maskedTarget": "m***@inktavia.com", "expiresInSeconds": 300 } }
```
Identity log (masked target, dev-only code):
```
Password recovery OTP dispatched to m***@inktavia.com via email (Logging mode — no real delivery).
[DEV-ONLY] Recovery OTP for m***@inktavia.com: 365345
```

**2) verify-otp** `{ resetRequestId, "otpCode":"365345" }` → **200**
```json
{ "header": { "isSuccess": true }, "body": {
  "resetToken": "4oIscBBPbwEafghQzMz08BD7C2cyTpjk5BBd_LeAVhk.gjjd…Zw" } }   // {resetRequestId}.{secret}
```

**3) set-new-password** `{ resetToken, "newPassword":"NewPass1234!", "confirmPassword":"NewPass1234!" }` → **200**
```json
{ "header": { "isSuccess": true }, "body": { "success": true } }
```

**4) login** `{ email, "password":"NewPass1234!" }` → **HTTP 200** (`accessToken` present, `tokenType":"Bearer"`).
**login with a non-current password** `{ email, "password":"WrongOldPass000!" }` → **HTTP 401**.
→ The password was actually changed in Keycloak and sessions revoked (best-effort).

> **Password-length note (honest deviation from the task's literal example):** the reset validator is mirrored
> byte-for-byte from the provider vertical, which enforces `NewPassword` `MinimumLength(12)`. The task's example
> `"NewPass123!"` is **11** chars and would (correctly) fail that rule, so the transcript uses the 12-char
> `"NewPass1234!"`. Mirroring exactly took precedence over the illustrative example.

---

## 4. Anti-enumeration + negative proofs

- **Anti-enumeration** — `forgot-password { "identifier":"nobody-unknown@nowhere.xyz" }` → **same 200 synthetic**
  (`resetRequestId` + `maskedTarget:"n***@nowhere.xyz"` + `expiresInSeconds:300`), and the identity `[DEV-ONLY]`
  OTP-line count was **unchanged (1 → 1)** — no OTP minted or logged for the unknown identifier.
- **Wrong `otpCode`** (fresh request, code `000000`) → business error `isSuccess:false`,
  `"The code is invalid or has expired. Please request a new code."`, `body:null`.
- **Reused/consumed `resetToken`** (the already-used token from step 3) → business error `isSuccess:false`,
  `"Your reset session has expired. Please restart the password recovery."`, `body:null`.
- **Short (<12) password** → business error (Identity validator rejects; surfaced as a clean 400-class error,
  no 500).

All negatives are business errors (400-class fail header), never 500s.

**No secrets logged:** the raw OTP appears **only** in the masked `[DEV-ONLY]` identity line (target masked, gated
by `DevExposeOtp` + Debug + the explicit env flags — same behavior as the provider vertical). The mobile BFF
never sees the OTP; no plaintext password or full reset token appears in any BFF log.

---

## 5. Notes / deviations

- **Profile gate:** the task text said `WorkshopRoleContext.Provider`, but that enum has **no `Provider` member**
  (`Participant=1, Trainer=2, Organizer=3, Admin=4, VenueOwner=5`) — the provider vertical actually gates on
  `Organizer`. The participant service gates on `WorkshopRoleContext.Participant` (1), which is the correct
  participant gate (same one M2a/M2e use).
- **Notifier reused, not twinned:** the task listed "reuse … the recovery notifier". The participant domain
  service injects the existing `IProviderPasswordRecoveryNotifier` directly — no new message/consumer plumbing
  (the notifier only ships a masked OTP; it is role-agnostic in behavior).
- **DTOs reused:** the shared `…Dto.PasswordRecovery` request/response types fit as-is, so no participant DTO
  variants were added (consistent with how the participant-OTP methods reuse the provider OTP DTOs).

---

## 6. Client-wiring handoff (FE follow-up in `inktavia-marine-mobile`)

Three screens → three endpoints, all `[AllowAnonymous]`, standard `AizenApiResponse` envelope
(`{ header:{ isSuccess, errorCode, errorMessage }, body }`). Carry `resetRequestId` from screen 1 → 2, and
`resetToken` from screen 2 → 3.

| Screen | `POST /api/v1/mobile/auth/…` | Request body | Success `body` |
|---|---|---|---|
| ForgotPassword | `forgot-password` | `{ "identifier": "<email or phone>" }` | `{ resetRequestId, maskedTarget, expiresInSeconds }` |
| VerificationCode | `verify-otp` | `{ "resetRequestId", "otpCode" }` | `{ resetToken }` |
| SetNewPassword | `set-new-password` | `{ "resetToken", "newPassword", "confirmPassword" }` | `{ success: true }` |
| (resend) | `forgot-password/resend` | `{ "resetRequestId" }` | `{ resendAfterSeconds, expiresInSeconds }` |

Flow contract:
1. `forgot-password` is **anti-enumeration** — always 200 with a `resetRequestId` (even for unknown accounts);
   the app should proceed to the code screen regardless.
2. `verify-otp` returns `{ resetToken }` on success; a wrong/expired code is a business error
   (`header.isSuccess=false`) — show the `errorMessage`, keep the user on the code screen.
3. `set-new-password` requires `newPassword == confirmPassword` and **min 12 chars**; a reused/expired
   `resetToken` or a mismatch is a business error. On success (`body.success=true`) route the user to Login.
4. After a successful reset the old sessions are revoked — the user signs in fresh with the new password.

---

## 7. Scoped `git status`

My M2f changes are confined to `Modules/Identity/**` (participant recovery entity + config + domain
service/interface + 4 command sets + controller + DI + DbContext + migration/snapshot) and
`Bff/src/Marine.Participant.Mobile/**` (IIdentityRemoteCall + AuthController + contracts + 4 command sets).

I did **not** touch the Provider/Admin password-recovery code (mirrored, not modified), MarineProvider/AdminPanel
BFFs, provider-web, admin-web, or CargoDry. The `Modules/Payment/**` and `Modules/ServiceRequest/**` (pricing
attributes / economics) entries in `git status` are **unrelated concurrent work from a parallel session** (their
migration timestamps `192934`/`192943` bracket mine `192938`) — not authored or touched by this slice. Nothing
was committed.

---

## 8. Out of scope (unchanged)

- Client screen wiring (a small separate `inktavia-marine-mobile` FE follow-up — contracts in §6).
- M3 features.
