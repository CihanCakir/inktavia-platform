# Claude Code Prompt — Provider Password Recovery: Identity-Backed Refactor

Refactor the provider password recovery so the **domain logic moves into the Identity module** and the
**MarineProvider BFF becomes a thin façade** that calls Identity via service-token remote calls. Keycloak stays the
full IdP and the actual password store. This changes the Identity DB schema (new table → **EF migration required**),
so it must be built + migrated in the live .NET environment. Follow the audited conventions below exactly.

## Non-negotiable rules
```
- Provider Web calls ONLY MarineProvider BFF. BFF public endpoints/response shapes MUST NOT change.
- Do NOT change Keycloak realm/client/role/login config, or provider-portal / provider-portal-bff clients.
- Identity may own recovery state + OTP validation, but the provider PASSWORD is reset ONLY in Keycloak via the
  server-side Admin API. Do NOT store/validate provider passwords in Identity. No Identity login token. No password
  grant. No X-Aizen-User-Token. Non-enumerating forgot/resend/reset responses. Never store raw OTP / raw reset
  token / new password. Do NOT leave production delivery as logging-only (document the Notification gap).
```

## Audited conventions (match these)
- **Identity Application** namespace is `Aizen.Modules.InktaviaStore.Application.Identity...` (physical folders
  `Aizen.Modules.Identity.Application/...`). Controllers: `Aizen.Modules.InktaviaStore.Controller.V1.Identity`.
  Domain/Abstraction/Repository use `Aizen.Modules.Identity.*` namespaces.
- CQRS: `AizenCommand<TResult>`, `AizenCommandHandler<TCmd,TResult>` (`override Task<TResult?> Handle`),
  `AizenValidator<TCmd>` (FluentValidation). Handlers are **thin** and delegate to a **domain service** in the
  Repository project (see `OrganizerKeycloakProvisioningDomainService`).
- Controller base `AizenWebApiController`, `IAizenCQRSProcessor _sender`, `_sender.ProcessAsync(command, ct)`,
  `SetResponse(result)`, route `api/v1/identity`, policies `IdentityRead` / `IdentityWrite` (service-token; the
  provider-portal-bff service account already holds these).
- User lookup: `UserManager<UserEntity>` (`FindByEmailAsync`, `Users.FirstOrDefaultAsync(u => u.KeycloakSubjectId==sub)`,
  `Users.FirstOrDefaultAsync(u => u.PhoneNumber==normalized)`). `UserEntity : IdentityUser<long>` has `Email`,
  `PhoneNumber`, `KeycloakSubjectId`. Provider validation: `IUserProfileRepository.GetActiveProfileIdAsync(userId,
  WorkshopRoleContext.Organizer)` must return non-null (an Organizer profile).
- Keycloak admin from Identity: reuse the **client-credentials token pattern** in
  `ProviderKeycloakRoleSyncService` + `IdentityKeycloakOptions` (`AdminApiBaseUrl`, `TokenEndpoint`,
  named HttpClient). Identity DI is `Aizen.Modules.Identity.Repository/DependencyInjection.cs` (registers
  `IdentityKeycloakOptions`, role-sync, provisioning service, the `IdentityKeycloakRoleSync` named client).
- EF: entities derive `AizenEntityWithAudit`; each entity has an `IEntityTypeConfiguration` (see
  `UserValidationEntityConfiguration`); add a `DbSet` to `IdentityDbContext` (confirm config auto-apply via
  `ApplyConfigurationsFromAssembly` or explicit `ApplyConfiguration`). Migrations live in
  `Aizen.Modules.Identity.Repository/Migrations`.

## Ownership decision (implement this)
`UserValidationEntity` stores a **raw `int` OTP** (`ValdationCode`) and has **no attempt count / reset-token /
hashing** — it cannot safely back this flow. **Create a dedicated Identity entity `ProviderPasswordRecoveryRequestEntity`**
(hashed OTP + hashed reset token + attempts + TTL + KeycloakSubjectId). Do NOT keep the recovery store in the BFF.

## Part 1 — Identity Domain + Persistence
1. `Domain/Entities/PasswordRecovery/ProviderPasswordRecoveryRequestEntity.cs` (`AizenEntityWithAudit`):
   `ResetRequestId` (unique), `KeycloakSubjectId`, `UserId`, `ProviderProfileId?`, `Channel`, `TargetHash`,
   `MaskedTarget`, `OtpHash`, `OtpSalt`, `OtpExpiresAtUtc`, `Attempts`, `MaxAttempts`, `ConsumedAtUtc?`,
   `ResetTokenHash?`, `ResetTokenSalt?`, `ResetTokenExpiresAtUtc?`, `LastSentAtUtc`. Add factory + `MarkConsumed`,
   `IncrementAttempt`, `IsOtpExpired`, `SetResetToken`. NEVER a raw OTP/token/password field.
2. `Domain/Entities/PasswordRecovery/ProviderPasswordRecoveryRequestEntityConfiguration.cs` — unique index on
   `ResetRequestId`; register in `IdentityDbContext` (`DbSet` + config).
3. Reuse the crypto helper approach from the BFF (`PasswordRecoverySecurity`: PBKDF2-SHA256 salted hash + FixedTime
   verify + numeric OTP + opaque token + masking) — port it into
   `Aizen.Modules.Identity.Repository/.../PasswordRecovery/PasswordRecoverySecurity.cs` (or a shared Core helper).

## Part 2 — Identity Keycloak password service
`Domain/Interface/Service/IIdentityKeycloakPasswordService.cs` + `Repository/Service/IdentityKeycloakPasswordService.cs`,
mirroring `ProviderKeycloakRoleSyncService`'s client-credentials token acquisition + `IdentityKeycloakOptions`:
```
Task ResetPasswordAsync(string keycloakUserId, string newPassword, CancellationToken ct);   // PUT /users/{id}/reset-password (Temporary=false)
Task RevokeSessionsAsync(string keycloakUserId, CancellationToken ct);                        // POST /users/{id}/logout
```
No secrets/passwords logged. If `IdentityKeycloakOptions.Enabled` is false → throw a controlled error (reset cannot
proceed without Keycloak).

## Part 3 — Identity recovery domain service
`Domain/Interface/Service/IProviderPasswordRecoveryDomainService.cs` + Repository impl. Methods return result DTOs:
```
RequestAsync(channel, identifier)  -> { ResetRequestId, MaskedTarget, OtpLength, ExpiresInSeconds, ResendAfterSeconds }
VerifyOtpAsync(resetRequestId, otpCode) -> { Verified, ResetToken?, ExpiresInSeconds }
ResetAsync(resetToken, newPassword) -> { Success, Message }
ResendAsync(resetRequestId) -> { Resent, ResendAfterSeconds, ExpiresInSeconds }
```
Logic:
- **Request:** normalize identifier; email → `UserManager.FindByEmailAsync`; phone → `Users.FirstOrDefaultAsync(u =>
  u.PhoneNumber == NormalizePhone(identifier))` (reuse the `NormalizePhone` from the provisioning service). Require an
  Organizer profile via `GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Organizer)`. If not resolvable →
  return a **synthetic** `ResetRequestId`, persist **nothing** (non-enumerating). If resolvable → generate OTP,
  persist `ProviderPasswordRecoveryRequestEntity` (hashed OTP, attempts=0, TTL), dispatch OTP via the notifier
  (Part 5). Config via a new `PasswordRecoveryOptions` (OtpLength=6, OtpTtlSeconds=300, ResendCooldownSeconds=60,
  MaxAttempts=5, ResetTokenTtlSeconds=300, DevExposeOtp=false).
- **VerifyOtp:** load by `ResetRequestId`; reject if null/consumed/expired/attempts≥max; `IncrementAttempt` + save on
  mismatch; on success mint single-use reset token `"{resetRequestId}.{secret}"` (store only secret hash + expiry),
  mark OTP consumed, save. Return the raw token once. **No login token / no Keycloak session.**
- **Reset:** split token → load record → verify secret hash + not expired → resolve `KeycloakSubjectId` → call
  `IIdentityKeycloakPasswordService.ResetPasswordAsync` then `RevokeSessionsAsync` (best-effort) → delete the record.
  Do NOT touch any local Identity password. Non-enumerating errors ("reset session expired").
- **Resend:** enforce cooldown; new OTP replaces old, attempts reset; dispatch; non-enumerating.

Persist via `IdentityDbContext` (like the provisioning service) or a MiniUow repository — match whichever the
recovery entity fits; keep it consistent with existing Identity persistence.

## Part 4 — Identity commands + controller (Application + Controller projects)
Under `Aizen.Modules.Identity.Application/Auth/Command/PasswordRecovery/{RequestProviderPasswordRecovery,
VerifyProviderPasswordRecoveryOtp,ResetProviderPassword,ResendProviderPasswordRecoveryOtp}/` (namespace
`Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery...`): Command + thin Handler (delegates to
the domain service) + Validator. Result/DTO types in `Aizen.Modules.Identity.Abstraction/Dto/PasswordRecovery/`.
Controller `Aizen.Modules.Identity/Controller/V1/Identity/ProviderPasswordRecoveryController.cs`
(`Aizen.Modules.InktaviaStore.Controller.V1.Identity`), `[Authorize(Policy="IdentityWrite")]`, thin:
```
POST /api/v1/identity/auth/provider-password-recovery/request      -> RequestProviderPasswordRecoveryCommand
POST /api/v1/identity/auth/provider-password-recovery/verify-otp   -> VerifyProviderPasswordRecoveryOtpCommand
POST /api/v1/identity/auth/provider-password-recovery/reset        -> ResetProviderPasswordCommand
POST /api/v1/identity/auth/provider-password-recovery/resend-otp   -> ResendProviderPasswordRecoveryOtpCommand
```

## Part 5 — Notification / OTP delivery
Add `IProviderPasswordRecoveryNotifier` in Identity. **Preferred:** publish to the Notification module / message bus
(inspect `Modules/Notification` for the existing publisher/event/template pattern) → email/SMS. **If no Notification
integration exists yet:** keep a `LoggingProviderPasswordRecoveryNotifier` that logs delivery **intent only** (never
the code), with a strict local-only `DevExposeOtp` flag (default false) that logs the code at Debug for dev testing.
Document the gap; do NOT pretend production delivery is done. No SMTP/SMS credentials in code.

## Part 6 — Shared Abstraction DTOs + BFF remote call
In `Aizen.Modules.Identity.Abstraction/Dto/PasswordRecovery/` define the request/response DTOs (both Identity and the
BFF reference these). Extend `IProviderIdentityRemoteCall` (BFF) with:
```
[AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/request")]   RequestProviderPasswordRecovery
[AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/verify-otp")] VerifyProviderPasswordRecoveryOtp
[AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/reset")]      ResetProviderPassword
[AizenRemoteCallPost("/api/v1/identity/auth/provider-password-recovery/resend-otp")] ResendProviderPasswordRecoveryOtp
```
(bodies = the Abstraction request DTOs; responses = the Abstraction response DTOs). Auth is the existing service
token injected by `MarineProviderBffAuthDelegatingHandler`.

## Part 7 — BFF refactor (keep public endpoints identical)
Rewrite the 4 handlers in `Bff/.../Application/Auth/Password/*` to **delegate to Identity** via
`IProviderIdentityRemoteCall`, mapping Identity results into the existing BFF response contracts
(`ForgotProviderPasswordResponse`, `VerifyProviderPasswordOtpResponse`, `ResetProviderPasswordResponse`,
`ResendProviderPasswordOtpResponse`) so the **frontend contract is unchanged**. Preserve non-enumeration + provider-
safe messages; forward no browser secrets. Then **retire the BFF-local domain**: remove the DI registrations +
usages of `IProviderPasswordRecoveryStore`/`DistributedProviderPasswordRecoveryStore`,
`IProviderPasswordRecoveryNotifier`/`LoggingProviderPasswordRecoveryNotifier`, `PasswordRecoveryRecord`,
`PasswordRecoverySecurity` (BFF copy), `PasswordRecoveryOptions` (BFF copy), and
`ProviderKeycloakAdminClient.ResetUserPasswordAsync`/`LogoutUserAsync` if now unused (keep the interface tidy).
Delete the now-dead files. `AddDistributedMemoryCache()` may stay if used elsewhere; otherwise remove.

## Part 8 — DI + config + migration
- Register in Identity DI (`Aizen.Modules.Identity.Repository/DependencyInjection.cs`): `PasswordRecoveryOptions`
  (bind `PasswordRecovery` section), `IProviderPasswordRecoveryDomainService`, `IIdentityKeycloakPasswordService`
  (+ reuse/add its named HttpClient), `IProviderPasswordRecoveryNotifier`. Ensure `IdentityKeycloakOptions` is
  populated for identity-api (BaseUrl/Realm/AdminClientId/AdminClientSecret) in docker-compose.
- **EF migration:** `dotnet ef migrations add AddProviderPasswordRecoveryRequest -p Aizen.Modules.Identity.Repository
  -s <identity-api host>` then update the database (or rely on the app's migrate-on-start if present).

## Part 9 — Build + smoke (run for real)
```
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Aizen.Modules.Identity.Abstraction.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj
```
Frontend (should need no change): `npm run build && npm run lint` (verify `passwordRecoveryApi` response types still
match the BFF projection; adjust only the BFF→FE projection or FE types minimally if a field renamed).

Smoke (with `DevExposeOtp=true` locally to read the OTP from logs):
```
1. POST /provider/auth/password/forgot {email}  -> accepted + resetRequestId + maskedTarget (same for unknown email)
2. read dev OTP from identity-api logs
3. POST /provider/auth/password/otp/verify {resetRequestId, otpCode} -> verified + resetToken (NO access token)
4. POST /provider/auth/password/reset {resetToken, new/confirm} -> success; Keycloak password updated; sessions revoked
5. login with the new password via Keycloak; old sessions invalid
Negatives: wrong OTP (attempt++ then lock at max), expired OTP, reused reset token (single-use), unknown account
(generic accepted, no OTP), resend cooldown.
```

## Acceptance
```
- Identity owns recovery state + OTP validation; password reset happens ONLY in Keycloak via Admin API from Identity.
- BFF public endpoints + frontend contract unchanged; BFF-local OTP/reset store removed.
- No raw OTP/token/password stored anywhere; non-enumerating responses; no login token after verify.
- All 7 backend projects build 0 errors; migration applied; smoke passes.
- Notification delivery implemented OR the gap documented with DevExposeOtp gated to local only.
```
Do not claim production readiness unless Notification delivery, persistence/migration, and the runtime smoke are all
actually complete.
