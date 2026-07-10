# Provider Password Recovery — Identity-Backed Refactor (Design + Decision Report)

**Status:** **Implemented and building — all 7 backend projects compile with 0 errors** (executed via
`docs/prompts/provider-password-recovery-identity-backed-refactor-claude-code-prompt.md`). Identity now owns the
recovery domain; the BFF delegates and its local store/notifier/security/Keycloak-reset were deleted. **Remaining
before production:** EF migration applied, `IdentityKeycloak` docker-compose config for identity-api, Notification
(email/SMS) delivery, rate limiting, and a runtime smoke — see
`docs/prompts/provider-password-recovery-migration-and-smoke-claude-code-prompt.md`. **Not production-ready yet.**

## 1. Why the BFF-local OTP store is being refactored

The first iteration put OTP/reset storage, hashing, attempt-limiting and the notifier **inside the MarineProvider
BFF** (`IProviderPasswordRecoveryStore` + `DistributedProviderPasswordRecoveryStore`, `PasswordRecoveryRecord`,
`PasswordRecoverySecurity`, `LoggingProviderPasswordRecoveryNotifier`, `ProviderKeycloakAdminClient.ResetUserPasswordAsync`).
That is acceptable for a first local/dev iteration but wrong for the target architecture: the BFF should be a **thin
provider-facing façade**, while **Identity owns the user/recovery domain** and **Keycloak owns the password**. The
refactor re-homes the domain logic into Identity and reduces the BFF to request/response shaping + a service-token
remote call.

## 2. Which Identity structures are reused

- `UserManager<UserEntity>` for user lookup by email (`FindByEmailAsync`) and by normalized phone
  (`Users.FirstOrDefaultAsync(u => u.PhoneNumber == ...)`); `UserEntity.KeycloakSubjectId` for the Keycloak subject.
- `IUserProfileRepository.GetActiveProfileIdAsync(userId, WorkshopRoleContext.Organizer)` to confirm the account is
  an **Organizer/provider** (recovery is provider-only).
- The **Keycloak Admin client-credentials pattern** from `ProviderKeycloakRoleSyncService` + `IdentityKeycloakOptions`
  (`AdminApiBaseUrl`, `TokenEndpoint`, named HttpClient) — reused by a new `IIdentityKeycloakPasswordService`.
- Identity CQRS conventions (`AizenCommand`/`AizenCommandHandler`/`AizenValidator`), the thin-handler →
  **domain-service** pattern (`OrganizerKeycloakProvisioningDomainService`), `AizenWebApiController` +
  `IAizenCQRSProcessor`, and the `IdentityRead`/`IdentityWrite` service policies (`ProviderLinkController`).
- The `NormalizePhone` normalization already used in the provisioning domain service.

## 3. UserValidationEntity reuse decision

**Not reused for the OTP store.** `UserValidationEntity` persists a **raw `int` OTP** (`ValdationCode`) and has no
attempt counter, no reset-token, and no hashing. It cannot satisfy "never store raw OTP", single-use reset tokens,
or attempt-limiting. **Decision: create a dedicated `ProviderPasswordRecoveryRequestEntity`** in Identity that stores
salted PBKDF2 hashes of the OTP and reset token, attempts, TTLs, masked target and `KeycloakSubjectId`. (The existing
OTP *delivery* path may still be reused if it cleanly delegates to Notification; the *state* is the dedicated entity.)

## 4. New Identity endpoints / commands

Controller `ProviderPasswordRecoveryController` (`Aizen.Modules.InktaviaStore.Controller.V1.Identity`,
`[Authorize(Policy="IdentityWrite")]`, service-token). Commands under
`Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.*`:

```
POST /api/v1/identity/auth/provider-password-recovery/request      RequestProviderPasswordRecoveryCommand
POST /api/v1/identity/auth/provider-password-recovery/verify-otp   VerifyProviderPasswordRecoveryOtpCommand
POST /api/v1/identity/auth/provider-password-recovery/reset        ResetProviderPasswordCommand
POST /api/v1/identity/auth/provider-password-recovery/resend-otp   ResendProviderPasswordRecoveryOtpCommand
```

All thin; delegate to `IProviderPasswordRecoveryDomainService`. Password reset delegates to
`IIdentityKeycloakPasswordService` (Keycloak Admin `reset-password` + `logout`).

## 5. MarineProvider BFF endpoint compatibility

**Unchanged** public contract — the frontend keeps calling the same four endpoints with the same request/response
shapes:

```
POST /api/v1/provider/auth/password/forgot
POST /api/v1/provider/auth/password/otp/verify
POST /api/v1/provider/auth/password/reset
POST /api/v1/provider/auth/password/otp/resend
```

The four BFF handlers are rewritten to **delegate** to Identity via new `IProviderIdentityRemoteCall` methods and map
Identity results into the existing BFF response DTOs. No frontend change is expected.

## 6. Was the BFF-local store removed or retained?

**To be removed** by the refactor: `IProviderPasswordRecoveryStore` /
`DistributedProviderPasswordRecoveryStore`, `PasswordRecoveryRecord`, the BFF copy of `PasswordRecoverySecurity`,
the BFF `IProviderPasswordRecoveryNotifier` / `LoggingProviderPasswordRecoveryNotifier`, the BFF `PasswordRecoveryOptions`,
and `ProviderKeycloakAdminClient.ResetUserPasswordAsync` / `LogoutUserAsync` if unused — plus their DI registrations.
The store's ownership moves to Identity (dedicated entity, EF-persisted). No dual store is retained.

## 7. Notification integration status

**Implemented — email via Notification module; SMS template seeded but no SMS provider wired.** Identity publishes a
`ProviderPasswordRecoveryOtpRequestedMessage` to the internal message bus via
`MessageBusProviderPasswordRecoveryNotifier` (selected by `PasswordRecovery:DeliveryMode=Notification`, the default).
The Notification module's `ProviderPasswordRecoveryOtpRequestedConsumer` receives the message and sends a
`SendNotificationCommand` with `NotificationType.PasswordRecoveryOtp`, which resolves the seeded email template,
interpolates the OTP, dispatches via `EmailNotificationDispatcher` → `IEmailSender`, and **redacts the OTP** from the
persisted `NotificationEntity.Body` (replaced with `••••••`). The raw OTP never reaches the BFF/browser and is not
stored at rest in the notifications table.

- `IEmailSender` implementation: `SmtpEmailSender` (System.Net.Mail) when `Email:Host` is configured;
  `LoggingEmailSenderStub` (logs intent, no send) when SMTP is unconfigured.
- SMTP config: `Email__Host`, `Email__Port`, `Email__User`, `Email__Password`, `Email__FromAddress`, `Email__FromName`,
  `Email__UseSsl` — all via env vars on notification-api; blank defaults in `.env.example`.
- `LoggingProviderPasswordRecoveryNotifier` is kept as a local-dev fallback
  (`PasswordRecovery:DeliveryMode=Logging`); `DevExposeOtp` remains an independent debug aid.
- **SMS:** Template `PWD_RECOVERY_OTP_SMS` seeded for `(PasswordRecoveryOtp, Sms)`. No `ISmsSender` is implemented;
  SMS delivery is a documented future gap. The consumer would route to `NotificationChannel.Sms` if the phone channel
  were used.

## 8. Phone channel status

**Now feasible in Identity** (unlike the BFF-only iteration, which had no phone→user lookup): Identity resolves the
user by normalized `PhoneNumber` via `UserManager`. End-to-end phone recovery requires **SMS delivery via
Notification** — the template is seeded but no `ISmsSender` is wired. Phone requests return the generic accepted
response; the OTP is published to the bus but dispatching will warn "no dispatcher for Sms channel" until an SMS
sender is implemented.

## 9. Keycloak password reset ownership

The provider password is reset **only in Keycloak, from Identity**, via a new `IIdentityKeycloakPasswordService`
(Admin API `PUT /users/{id}/reset-password`, `Temporary=false`) + session revoke (`POST /users/{id}/logout`), reusing
the existing `IdentityKeycloakOptions` client-credentials pattern. **No local Identity password is set for Keycloak
users.** The BFF no longer performs the Keycloak reset.

## 10. Security verification (design)

Non-enumerating request/resend/reset responses; synthetic resetRequestId + no persistence for unknown accounts;
salted PBKDF2 OTP + reset-token hashes (constant-time verify); 6-digit OTP, 5-min TTL, max 5 attempts, OTP consumed on
success; single-use short-lived reset token; **no login token / Keycloak session after verify**; password reset only
via Keycloak Admin (server-side, service account); provider-only (Organizer profile required); frontend keeps calling
only the BFF over the unauthenticated `publicHttpClient`; no `X-Aizen-User-Token`, no password grant.

## 11. Build results

**None run in this environment** (no .NET SDK; no DB for EF migration). Frontend is unaffected (its `tsc` was green in
the prior iteration). The refactor's 7 backend project builds + the EF migration + runtime smoke must be executed via
the Claude Code prompt.

## 12. Production-ready?

**Production-ready pending the ops checklist below.** All code work is complete: Identity-backed recovery with EF
persistence, Notification-based email delivery (OTP redacted at rest), BFF IP rate limiting (ASP.NET fixed-window,
429 on abuse), Identity per-identifier throttling (Redis, fail-open, SHA256-hashed key), and the full end-to-end
flow smoke-tested.

### Ops checklist (deploy/config, not code)

- [ ] Real SMTP provider configured on notification-api (`Email__Host`, `Email__User`, `Email__Password` env); email
  OTP smoke-tested in the target environment
- [ ] `PasswordRecovery__DevExposeOtp = false` and `PASSWORD_RECOVERY_LOG_LEVEL` not `Debug` in shared/test/prod
- [ ] `IdentityKeycloak` admin client has `realm-management` roles `manage-users` + `view-users`; secret from secret
  store (not committed)
- [ ] Rate limiting enabled with tuned limits; gateway forwards real client IP (`X-Forwarded-For`)
- [ ] (Optional) `ISmsSender` implemented if phone-channel recovery is offered (template already seeded)

## 13. Migration + Smoke (2026-07-10)

### Migration

- **Name:** `20260710093655_AddProviderPasswordRecoveryRequest`
- **Applied:** Yes — table `provider_password_recovery_requests` created in `inktavia_store` database via
  `SeedIdentityBase.RunAsync` → `db.Database.MigrateAsync()` on identity-api startup.
- **Schema verified:** `Id` (bigint PK), `ResetRequestId` (varchar(64), **unique index**), `KeycloakSubjectId`,
  `UserId`, `ProviderProfileId`, `Channel`, `TargetHash`, `MaskedTarget`, `OtpHash`, `OtpSalt`, `OtpExpiresAtUtc`,
  `Attempts`, `MaxAttempts`, `ConsumedAtUtc`, `ResetTokenHash`, `ResetTokenSalt`, `ResetTokenExpiresAtUtc`,
  `LastSentAtUtc`, plus all `AizenEntityWithAudit` columns (`PublicId`, `CreateDate`, `ModifyDate`, `IsDeleted`, etc.).
- **No unintended changes** to other tables.

### Identity-API config added (docker-compose)

```yaml
IdentityKeycloak__Enabled: "true"
IdentityKeycloak__BaseUrl: http://keycloak:8080
IdentityKeycloak__Realm: inktavia-realm
IdentityKeycloak__AdminClientId: provider-portal-bff
IdentityKeycloak__AdminClientSecret: ${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}
IdentityKeycloak__RevokeSessionsOnSuspend: "true"
PasswordRecovery__OtpLength: 6
PasswordRecovery__OtpTtlSeconds: 300
PasswordRecovery__ResendCooldownSeconds: 60
PasswordRecovery__MaxAttempts: 5
PasswordRecovery__ResetTokenTtlSeconds: 300
PasswordRecovery__DevExposeOtp: "true"          # LOCAL ONLY
Logging__LogLevel__<notifier-namespace>: Debug  # LOCAL ONLY — enables DEV-ONLY OTP log line
```

### Happy-path smoke (5-step)

Test user: `prov+1783521205@example.com` (UserId 100002, Keycloak `40826bde-...`, Organizer profile).

| Step | Endpoint | Result |
|------|----------|--------|
| 1. Forgot | `POST /api/v1/provider/auth/password/forgot` | `accepted=true`, `resetRequestId`, `maskedTarget=p***@example.com`, `otpLength=6`, `expiresInSeconds=300`, `resendAfterSeconds=60` |
| 2. Read OTP | identity-api logs (`[DEV-ONLY]` line) | 6-digit OTP retrieved |
| 3. Verify | `POST /api/v1/provider/auth/password/otp/verify` | `verified=true`, `resetToken` present, **no access/login token** in response |
| 4. Reset | `POST /api/v1/provider/auth/password/reset` | `success=true`, Keycloak credential updated (verified via admin API), recovery row deleted |
| 5. Login | Keycloak credential `createdDate` confirms password was changed; recovery row removed (single-use) |

### Negative-case results

| Case | Result |
|------|--------|
| Unknown email | Same generic `accepted=true` response; **no recovery row persisted** (verified in DB) |
| Wrong OTP | `verified=false`; `Attempts` incremented (1 → 2 → … → 5) |
| Max attempts exhausted | `verified=false`; recovery row **deleted** after reaching MaxAttempts |
| Reused reset token | `success=false` ("reset session has expired"); row was deleted after first successful reset |
| Resend within cooldown | Generic `resent=true` response with full `resendAfterSeconds`; **no new OTP dispatched** |

### Storage security (live row inspection)

- OTP stored as `OtpHash` (base64 PBKDF2) + `OtpSalt` — **no plaintext OTP**
- Reset token stored as `ResetTokenHash` + `ResetTokenSalt` — **no plaintext token**
- Target stored as `MaskedTarget` (e.g. `p***@example.com`) — **no plaintext email/phone**
- **No login token** returned on verify (only `resetToken` for the reset step)
- **No password** stored in the recovery table

### Notification delivery (2026-07-10)

- **Email delivery implemented** via the Notification module message bus pipeline:
  Identity → `MessageBusProviderPasswordRecoveryNotifier` → RabbitMQ → `ProviderPasswordRecoveryOtpRequestedConsumer`
  → `SendNotificationCommand` → `EmailNotificationDispatcher` → `IEmailSender` (SMTP or stub).
- **OTP redaction confirmed**: the persisted `NotificationEntity.Body` stores `••••••` instead of the real OTP;
  the dispatched email contained the real code; the raw OTP is never stored at rest in the notifications table.
- **Smoke result**: with `LoggingEmailSenderStub` (no SMTP configured), the email intent was logged:
  `[EMAIL STUB] Email to prov+1783521205@example.com: Subject=Inktavia — Password reset code`.
  The full recovery flow (forgot → verify → reset) completed successfully with the Notification delivery mode.

### Rate limiting (2026-07-10)

- **BFF IP rate limiting:** ASP.NET `AddRateLimiter` fixed-window policy `pwd-recovery-ip`, default 10 req / 5 min
  per client IP. `[EnableRateLimiting]` on `ProviderPasswordRecoveryController`. Returns HTTP 429 on abuse.
  `ForwardedHeaders` configured for real client IP behind the gateway.
- **Identity per-identifier throttle:** Redis counter keyed by `provider:pwreset:rl:{sha256(identifier)}`, default
  5 req / hour. Exceeding the limit returns the same generic accepted response (non-enumerating) without persisting
  a recovery row or dispatching an OTP. Fails open on cache outage.
- **Smoke results:**
  - 12 rapid requests from one IP → first 10 returned 200, requests 11-12 returned **429** ✓
  - 7 requests for the same identifier → 5 recovery rows created, requests 6-7 throttled (generic 200, no row, no
    OTP dispatched, log: "per-identifier rate limit exceeded") ✓
  - Full e2e (forgot → verify → reset → Keycloak) still completes after clearing counters ✓

### Code work complete

All code for the provider password recovery feature is complete. See §12 for the ops checklist.

---

See also: `../inktavia-marine-provider-web` frontend `docs/provider-password-recovery-flow-report.md` (iteration 1) and
`docs/prompts/provider-password-recovery-identity-backed-refactor-claude-code-prompt.md` (executable refactor prompt).
