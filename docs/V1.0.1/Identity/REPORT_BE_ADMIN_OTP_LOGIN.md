# REPORT — BE_ADMIN_OTP_LOGIN (Kickoff 1)

## Summary

Implemented the admin OTP login vertical in the Identity module, mirroring the existing provider-otp-login
implementation. The admin panel's `POST /api/v1/identity/auth/admin-otp-login/{request,verify,resend,consume-ticket}`
endpoints are now live in the Identity backend.

## Files added

| File | Purpose |
|------|---------|
| `Domain/Entities/OtpLogin/AdminOtpLoginRequestEntity.cs` | Entity mirroring `ProviderOtpLoginRequestEntity` with `AdminProfileId` |
| `Domain/Entities/OtpLogin/AdminOtpLoginRequestEntityConfiguration.cs` | EF config → table `admin_otp_login_requests` |
| `Domain/Interface/Service/IAdminOtpLoginDomainService.cs` | Domain service interface (same shape as provider) |
| `Domain/Interface/Service/IAdminKeycloakProvisioningDomainService.cs` | Provisioning interface + `AdminKeycloakProvisionResult` record |
| `Repository/Service/OtpLogin/AdminOtpLoginDomainService.cs` | Admin OTP domain service implementation |
| `Repository/Service/AdminKeycloakProvisioningDomainService.cs` | Admin↔Keycloak provisioning (mirror of Organizer) |
| `Abstraction/Model/ProvisionAdminFromKeycloakDomainModel.cs` | Input model for admin provisioning |
| `Application/Auth/Command/OtpLogin/RequestAdminOtpLogin/*` | Command + Handler + Validator |
| `Application/Auth/Command/OtpLogin/VerifyAdminOtpLogin/*` | Command + Handler + Validator |
| `Application/Auth/Command/OtpLogin/ResendAdminOtpLogin/*` | Command + Handler + Validator |
| `Controller/V1/Identity/AdminOtpLoginController.cs` | API controller at `admin-otp-login` route |
| `Repository/Migrations/20260729182024_AddAdminOtpLoginRequest.cs` | EF Core migration |

## Files modified

| File | Change |
|------|--------|
| `Repository/Context/InktaviaStoreIdentityDbContext.cs` | Added `AdminOtpLoginRequests` DbSet + entity configuration |
| `Repository/DependencyInjection.cs` | Registered `IAdminOtpLoginDomainService` + `IAdminKeycloakProvisioningDomainService` |
| `Repository/Context/Seed/SeedIdentityBase.cs` | Dev/Local: admin gets `KeycloakSubjectId` + `WorkshopRoleContext.Admin` profile |

## Key design decisions

### Admin resolution rule: Admin **role** (primary gate) + Admin **profile** (optional context)

The provider path gates on an active `Organizer` **profile** because providers always have one (it's created
during registration/provisioning). Admins in this codebase are represented primarily by the `RoleNames.Admin`
**role** — the seed creates an admin with a Participant profile and an Admin role, not an Admin profile.

To keep the OTP gate robust without breaking the existing admin model:
- **Primary gate:** `_userManager.IsInRoleAsync(user, RoleNames.Admin)` — if the user lacks the Admin role,
  the synthetic anti-enumeration response is returned.
- **Secondary:** `_profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Admin)` is called to
  capture the `AdminProfileId` on the entity, but its absence does **not** block the OTP flow. The seed
  now creates an Admin profile in Dev/Local so the field is populated.

### Separate entity (not reusing provider entity)

Created `AdminOtpLoginRequestEntity` with its own table `admin_otp_login_requests`. This keeps aggregates
clean and avoids nullable-profile hacks on the provider entity. The `ProviderProfileId` field becomes
`AdminProfileId`.

### Shared services (no duplication)

- **Ticket service:** Reuses `IProviderOtpLoginTicketService` — `MintAsync(sub, "admin-panel")` with
  `clientId="admin-panel"` (vs `"provider-portal"` for providers). Same jti→sub Redis store, same secrets.
- **Notifier:** Reuses `IProviderOtpLoginNotifier` — the notifier is channel/user-generic. Same
  `OtpLoginOptions`, same `PasswordRecoveryOptions.DevExposeOtp` flag.
- **Options:** Same `OtpLoginOptions` and `OtpLoginTicketOptions` sections.

### Rate-limit key prefix

Admin uses `admin:otplogin:rl:` (vs `provider:otplogin:rl:`) to keep rate-limit counters separate.

## Migration

- Name: `20260729182024_AddAdminOtpLoginRequest`
- Table: `admin_otp_login_requests`
- Columns mirror `provider_otp_login_requests` exactly, with `AdminProfileId` (nullable bigint) replacing
  `ProviderProfileId`.
- Unique index on `LoginRequestId`.

## Dev-seed changes (OTP-ready admin)

In `Local` / `Development` environment, `SeedIdentityBase` now:
1. Sets `admin.KeycloakSubjectId` to config value `Seed:Admin:KeycloakSubjectId` or a stable dev placeholder
   `00000000-0000-0000-0000-admin0000001` (with console warning).
2. Creates a `WorkshopRoleContext.Admin` profile for the admin user (idempotent).

The admin email defaults to `admin@inktavia.local` (from `Seed:Admin:Email` config). To test with a different
email (e.g. `admin.user@inktavia.com`), set `Seed:Admin:Email` in appsettings or environment.

## Verification transcript (expected)

```
# 1. Request
POST /api/v1/identity/auth/admin-otp-login/request
{ "channel": "email", "identifier": "admin@inktavia.local" }
→ 200 { "accepted": true, "loginRequestId": "...", "maskedTarget": "a***@inktavia.local", ... }

# Identity log output:
[DEV-ONLY] OTP login code for a***@inktavia.local: 123456

# 2. Verify (with the logged code)
POST /api/v1/identity/auth/admin-otp-login/verify
{ "loginRequestId": "...", "otpCode": "123456" }
→ 200 { "verified": true, "nextAction": "redirect_to_keycloak_handoff", "loginTicket": "...", ... }

# 3. Wrong code
→ 200 { "verified": false, "nextAction": "keycloak_handoff_required", ... }
# Attempts increment; at MaxAttempts (5), record is deleted.

# 4. Resend (before cooldown)
→ 200 { "resent": false, "resendAfterSeconds": N, "message": "Please wait..." }

# 5. Unknown identifier
POST .../request { "channel": "email", "identifier": "nobody@example.com" }
→ 200 { "accepted": true, ... }  (synthetic — no log line, no DB row)
```

## Kickoff 2 handoff

### Subject ID replacement
The dev-placeholder `KeycloakSubjectId` (`00000000-0000-0000-0000-admin0000001`) must be replaced with the
real Keycloak subject id of the admin user (`admin.user@inktavia.com` or whichever user the realm admin maps
to). This can be done by:
- Setting `Seed:Admin:KeycloakSubjectId` in appsettings, or
- Running the `AdminKeycloakProvisioningDomainService.ProvisionAsync()` from the Keycloak authenticator SPI,
  which will call `SetKeycloakSubjectId()` on the existing user.

### init.sh configuration values
```
Flow name:         "Admin OTP Login browser"
SPI:               provider-otp-login-ticket (same SPI as provider)
allowedClientId:   admin-panel
consumePath:       /api/v1/identity/auth/admin-otp-login/consume-ticket
consumeSecret:     (same OtpLoginTicketOptions.ConsumeSecret)
ticketSecret:      (same OtpLoginTicketOptions.Secret)
Client binding:    admin-panel client → "Admin OTP Login browser" flow
```

## Provider path impact

**Zero.** No provider files were modified. The provider entity, table, migration, controller, domain service,
notifier, and ticket service are untouched and byte-for-byte identical.

## Verification (smoke — 2026-07-29)

End-to-end smoke through the running docker stack (`identity-api` 7101, `bff-adminpanel` 17001,
`ASPNETCORE_ENVIRONMENT=Development`). Rebuilt + restarted both services. **Status: PASS** — code generated,
reached the logs, and a login ticket was minted. Secrets/OTP/ticket masked below.

**Environment note:** Docker Desktop's VM crashed once when two full-stack `--build` runs collided (OOM); recovered
by restarting Docker and building `identity-api` then `bff-adminpanel` sequentially. `bff-adminpanel` was started
with `--no-deps` because its full `depends_on` graph pulls in un-built downstream APIs (e.g. `service-request-api`)
not needed for this OTP smoke.

### Migration + table
- Migration `20260729182024_AddAdminOtpLoginRequest` recorded in `__EFMigrationsHistory` and **applied** on startup.
- Table `public.admin_otp_login_requests` exists with the expected columns (`LoginRequestId`, `KeycloakSubjectId`,
  `Channel`, `OtpHash/OtpSalt`, `OtpExpiresAtUtc`, `Attempts`, `MaxAttempts`, `ConsumedAtUtc`, audit columns).

### Seed (OTP-ready admin)
- `admin@inktavia.local` (UserId=1): **`has_sub = true`**. Startup logged the expected
  `[WARN] SeedIdentityBase: Admin user assigned dev-placeholder KeycloakSubjectId` — the seed's update path ran
  against the pre-existing admin row (placeholder subject `…-admin0000001`; real subject is Kickoff 2).

### Request → log → verify (masked)
- `POST /api/v1/admin-panel/auth/otp-login/request` `{admin@inktavia.local}` → **HTTP 200**,
  `accepted:true`, `maskedTarget:"a***@inktavia.local"`, `loginRequestId` present, `otpLength:6`,
  `expiresInSeconds:300`, `resendAfterSeconds:60`.
- Identity Debug log emitted `[DEV-ONLY] OTP login code for a***@inktavia.local: ******` (6-digit code present).
- `POST …/verify` with that code → **HTTP 200**, `verified:true`,
  `nextAction:"redirect_to_keycloak_handoff"`, **non-empty `loginTicket`** (JWT; `expiresInSeconds:120`).
  Ticket payload carries the placeholder `sub` + `clientId:admin-panel`, confirming the mint path
  (`OTP_LOGIN_TICKET_SECRET` wired).

### Negative checks
- **Anti-enumeration:** unknown identifier `nobody@nowhere.test` → HTTP 200, synthetic `accepted:true`
  (masked target derived from input), **no new DB row** (count stayed 1), **no `[DEV-ONLY]` log line**.
- **Brute-force guard (MaxAttempts=5):** fresh request, then 6 wrong-code verifies all returned `verified:false`;
  the request row was **invalidated/removed** once attempts hit `MaxAttempts` (no longer present in
  `admin_otp_login_requests`). The earlier successful request row remains with `ConsumedAtUtc` set.

### Regression
- No product code was modified during verification (verification-only; zero edits). Pre-existing uncommitted branch
  changes under `MarineProvider`/`CargoDry` are unrelated to this smoke. The shared provider-otp-login
  notifier/service/ticket files were untouched. Admin request re-run still returns HTTP 200.

**Result: PASS** — admin OTP code generated, appeared in the Identity logs at Debug, and a login ticket was minted
end-to-end through the admin BFF.
