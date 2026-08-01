# BE_ADMIN_OTP_LOGIN — Kickoff 1 (Identity backend): admin OTP login + Keycloak provisioning

> **Repo:** `addesso-project` (Identity module). **Goal of Kickoff 1:** make the admin panel's OTP login
> actually produce a code. Today the AdminPanel BFF calls `POST /api/v1/identity/auth/admin-otp-login/{request,verify,resend}`
> but **Identity has no such endpoint** (only `provider-otp-login` exists) → the BFF's call fails → it returns the
> generic anti-enumeration message *"If an account exists, a verification code has been sent."* and **no OTP is ever
> generated**. This kickoff mirrors the existing, working **provider-otp-login** vertical for admins, and adds the
> **admin↔Keycloak provisioning** that links the Identity admin user to a Keycloak subject (the real blocker: the
> seeded admin `admin@inktavia.local` is a local account with **no `KeycloakSubjectId`**, so the OTP domain service
> returns synthetic and dispatches nothing).
>
> **Scope of THIS kickoff = Identity backend only, verifiable via logs.** Keycloak flow binding (`init.sh`) and the
> admin-web redirect handoff are **Kickoff 2** (do NOT do them here). **Do NOT touch the MarineProvider BFF,
> provider-web, CargoDry, or the provider-otp-login files** — they are the reference and must stay byte-for-byte.

## 0. Ground truth (inspect these first — mirror exactly, do not invent)

Provider vertical to mirror (all under `Modules/Identity/src`):
- **Controller:** `Aizen.Modules.Identity/Controller/V1/Identity/ProviderOtpLoginController.cs`
  route `api/v1/identity/auth/provider-otp-login`, `[Authorize(Policy = "IdentityWrite")]`, actions
  `request`/`verify`/`resend` + server-to-server `consume-ticket` (`[AllowAnonymous]`, `X-Otp-Login-Consume-Secret`
  header check). **Note the namespace it actually uses** (`Aizen.Modules.InktaviaStore.Controller.V1.Identity`) and
  the command namespaces (`Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.*`) — **match whatever
  the provider file uses; do not "fix" it.**
- **Commands/handlers:** `Aizen.Modules.Identity.Application/Auth/Command/OtpLogin/{RequestProviderOtpLogin,VerifyProviderOtpLogin,ResendProviderOtpLogin}/*`
- **Domain service:** `Aizen.Modules.Identity.Repository/Service/OtpLogin/ProviderOtpLoginDomainService.cs`
  (resolves user by email/phone → requires `user.KeycloakSubjectId` non-empty → requires an active
  **Organizer** profile via `_profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Organizer)` → generates
  OTP, stores `ProviderOtpLoginRequestEntity`, dispatches via `IProviderOtpLoginNotifier`; verify mints a ticket via
  `IProviderOtpLoginTicketService.MintAsync(sub, "provider-portal")`).
- **Ticket service:** `.../Service/OtpLogin/ProviderOtpLoginTicketService.cs` (HMAC ticket + Redis `otplogin:ticket:{jti}`
  → `sub`; **audience-agnostic** — the only per-client value is the `clientId` claim passed to `MintAsync`). **Reuse
  this service as-is for admin** — do NOT fork it; just call `MintAsync(sub, "admin-panel")`.
- **Notifier:** `.../Service/OtpLogin/{LoggingProviderOtpLoginNotifier,MessageBusProviderOtpLoginNotifier}.cs` +
  `OtpLoginOptions.cs`. In `Logging` mode with `PasswordRecoveryOptions.DevExposeOtp=true` the code is logged at
  **Debug**: `[DEV-ONLY] OTP login code for {MaskedTarget}: {Otp}`. **Reuse the SAME `IProviderOtpLoginNotifier`** for
  admin (do not add a second notifier interface) — the notifier is channel/user-generic.
- **Entity:** `Aizen.Modules.Identity.Domain/Entities/OtpLogin/ProviderOtpLoginRequestEntity.cs` (+ its
  `...EntityConfiguration.cs`) and the `IdentityDbContext.ProviderOtpLoginRequests` DbSet; migration
  `20260710113802_AddProviderOtpLoginRequest`.
- **Provisioning reference:** `.../Service/OrganizerKeycloakProvisioningDomainService.cs` — the pattern for
  linking/creating an Identity user to a Keycloak subject (`UserEntity.CreateFromKeycloak`, `SetKeycloakSubjectId`,
  ensure role via `RoleNames`, ensure `WorkshopRoleContext` profile). Mirror this for admin.
- **Abstraction DTOs (REUSE, do not duplicate):** `Aizen.Modules.Identity.Abstraction/Dto/OtpLogin/ProviderOtpLoginDtos.cs`
  — `RequestProviderOtpLoginRequest/Response`, `VerifyProviderOtpLoginRequest/Response`,
  `ResendProviderOtpLoginRequest/Response`, `ConsumeTicketRequest`. The AdminPanel BFF already calls the admin
  endpoints with these exact types (`IIdentityAdminBffRemoteCall.RequestAdminOtpLogin(RequestProviderOtpLoginRequest)`
  etc.), so **the admin controller must accept/return the same DTO types** — no new contracts.
- **Enums/roles:** `WorkshopRoleContext.Admin = 4`; `RoleNames.Admin = "Admin"`.
- **Seed:** `.../Context/Seed/SeedIdentityBase.cs` — seeds `admin@inktavia.local` as a **local** user (Participant
  profile + "Admin" role), **no `KeycloakSubjectId`**. This is why OTP returns synthetic today.

BFF endpoint contract the Identity controller MUST satisfy (already deployed, do not change the BFF):
`IIdentityAdminBffRemoteCall` → `POST /api/v1/identity/auth/admin-otp-login/request` · `/verify` · `/resend`.

## 1. Deliverables (Identity module only)

### 1.1 Admin OTP request entity + migration
Create `AdminOtpLoginRequestEntity` (mirror `ProviderOtpLoginRequestEntity`) under
`Domain/Entities/OtpLogin/`, **with the provider-profile field replaced**: instead of `ProviderProfileId`, store
`AdminProfileId` (the `WorkshopRoleContext.Admin` profile id) — or make it nullable if the admin has no dedicated
profile row; pick whichever keeps the aggregate valid and mirror the provider `Create(...)` factory + `IsOtpExpired`,
`IncrementAttempt`, `MarkConsumed`, `LastSentAtUtc`, `MaxAttempts` members exactly.
- Add `IdentityDbContext.AdminOtpLoginRequests` DbSet + an `AdminOtpLoginRequestEntityConfiguration` (mirror the
  provider config; separate table, e.g. `admin_otp_login_requests`).
- Add an EF Core migration `AddAdminOtpLoginRequest` (same DbContext/provider as the existing OTP migration).
  **PostgreSQL-safe, UTC timestamps** (match the provider entity's column types exactly).

> Rationale for a separate entity/table (not reusing the provider one): keeps module aggregates clean and avoids a
> nullable-provider-profile hack on the provider entity. If you judge reuse is strictly better, you MAY instead make
> `ProviderProfileId` nullable and add an `Audience`/`ClientId` discriminator column — but only if that does not
> weaken the provider path. Default to the separate entity.

### 1.2 Admin OTP domain service
Create `AdminOtpLoginDomainService : IAdminOtpLoginDomainService` (mirror `ProviderOtpLoginDomainService`), with the
**only** behavioral differences:
- Resolve the admin instead of an Organizer: after finding the user by email/phone and requiring
  `user.KeycloakSubjectId` non-empty, require an active **Admin** context —
  `_profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Admin)` **and/or** membership in
  `RoleNames.Admin` (mirror how the codebase authorizes admins elsewhere; if admins are represented by the "Admin"
  **role** rather than an Admin **profile**, gate on the role and pass the resolved profile/user id accordingly).
  If neither present → return the synthetic result (anti-enumeration), exactly like provider.
- Persist to `AdminOtpLoginRequests`.
- On verify success, mint the ticket with **`clientId: "admin-panel"`** (not `"provider-portal"`), reusing
  `IProviderOtpLoginTicketService.MintAsync(record.KeycloakSubjectId, "admin-panel", ct)`. Keep `NextAction`
  semantics identical (`redirect_to_keycloak_handoff` on success, `keycloak_handoff_required` on ticket-mint
  failure/invalid).
- Reuse `IProviderOtpLoginNotifier`, `OtpLoginOptions`, `PasswordRecoverySecurity`, and the rate-limit cache pattern
  (use an admin-scoped key prefix, e.g. `admin:otplogin:rl:`).

Register `IAdminOtpLoginDomainService → AdminOtpLoginDomainService` in the same DI method that registers the provider
one (`Aizen.Modules.Identity.Repository/DependencyInjection.cs`). Reuse the existing `OtpLoginOptions` /
`OtpLoginTicketOptions` / notifier registrations — do NOT create a second ticket service or notifier.

### 1.3 Admin OTP commands + controller
- Commands/handlers `RequestAdminOtpLogin` / `VerifyAdminOtpLogin` / `ResendAdminOtpLogin` (mirror the provider
  commands; each delegates to `IAdminOtpLoginDomainService`). **Return the existing
  `Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.*` response DTOs** (same shape the BFF expects).
- `AdminOtpLoginController` (mirror `ProviderOtpLoginController`) at route
  **`api/v1/identity/auth/admin-otp-login`** with `request` / `verify` / `resend` + a server-to-server
  **`consume-ticket`** action (`[AllowAnonymous]` + `X-Otp-Login-Consume-Secret` check, reusing
  `IProviderOtpLoginTicketService.ConsumeAsync` and the same `OtpLoginTicketOptions.ConsumeSecret`). Keep the same
  `[Authorize(Policy = "IdentityWrite")]` on request/verify/resend, and match the provider file's namespaces.

> The admin `consume-ticket` may share the provider ticket store (jti→sub is client-agnostic). Expose it under the
> admin route anyway so Kickoff 2 can point the admin Keycloak authenticator's `consumePath` at
> `/api/v1/identity/auth/admin-otp-login/consume-ticket` without coupling admin to provider paths.

### 1.4 Admin↔Keycloak provisioning (the real blocker)
Create `AdminKeycloakProvisioningDomainService : IAdminKeycloakProvisioningDomainService` mirroring
`OrganizerKeycloakProvisioningDomainService`, differing only in: ensure `RoleNames.Admin` (not Organizer) and a
`WorkshopRoleContext.Admin` profile; accept a `ProvisionAdminFromKeycloakDomainModel { KeycloakSubjectId, Email,
FirstName, LastName, EmailVerified }`. Reuse the find-by-sub → find-by-email → link (`SetKeycloakSubjectId`) → create
(`UserEntity.CreateFromKeycloak`) resolution ladder, including the controlled `EmailConflictWithExistingUser`.

**Dev-seed hook so THIS kickoff is verifiable without Keycloak wiring:** extend `SeedIdentityBase` (or add an
idempotent dev seeder) so that, in `Local`/`Development`, an admin Identity user exists that is **OTP-ready**:
- Identifiable by a real email the operator will type (default to the realm admin `admin.user@inktavia.com`;
  make it configurable via `Seed:Admin:Email`).
- Has a **non-empty `KeycloakSubjectId`** — read from config `Seed:Admin:KeycloakSubjectId` if provided; otherwise
  generate a stable dev placeholder GUID and log a clear warning that Kickoff 2 must replace it with the real Keycloak
  subject id of `admin.user@inktavia.com`. (The OTP **request/verify** path only needs a non-empty subject id and the
  Admin role/profile; the real subject is validated later by the Keycloak SPI during handoff — Kickoff 2.)
- Has the `RoleNames.Admin` role and a `WorkshopRoleContext.Admin` profile (add the Admin profile; keep existing
  Participant/local admin seed intact and idempotent — do not delete or duplicate).

Guard all seeding with idempotency (find-or-create). **Do not print secrets or passwords.**

## 2. Config / flags to confirm (do not hardcode)
- `OtpLogin` options and the delivery mode (`Logging` vs message bus) come from Identity config — confirm the admin
  path uses the SAME `OtpLoginOptions`/delivery-mode as provider (it should, via the shared notifier).
- `PasswordRecoveryOptions.DevExposeOtp` is `true` in `appsettings.Local.json` — this is what surfaces the code in
  logs. Verify the admin path hits the same notifier so the `[DEV-ONLY]` line fires.
- `OtpLoginTicketOptions.Secret` / `ConsumeSecret` — reused as-is (same secrets provider uses). **Never log them.**

## 3. Acceptance / verification (must all pass)
1. `dotnet build` 0 errors; migration applies cleanly to `inktavia_store`; clean boot; DI resolves
   `IAdminOtpLoginDomainService` + `IAdminKeycloakProvisioningDomainService`.
2. With the dev admin seeded (OTP-ready, `admin.user@inktavia.com`):
   `POST /api/v1/identity/auth/admin-otp-login/request { "channel":"email", "identifier":"admin.user@inktavia.com" }`
   → 200 with `accepted:true`, masked target, `loginRequestId`, and **the Identity log shows**
   `[DEV-ONLY] OTP login code for a***@inktavia.com: NNNNNN`.
3. `POST .../admin-otp-login/verify { loginRequestId, otpCode }` with the logged code → 200,
   `verified:true`, `nextAction:"redirect_to_keycloak_handoff"`, a non-empty `loginTicket`. Wrong/expired code →
   `verified:false`, `keycloak_handoff_required`, attempts increment then invalidate at `MaxAttempts`.
4. `resend` respects the cooldown (no false "resent"); unknown identifier still returns the synthetic accepted
   response (anti-enumeration) with **no** log line and **no** DB row.
5. Provider path unchanged: `provider-otp-login/{request,verify,resend,consume-ticket}` byte-for-byte behavior;
   provider entity/table/migration untouched; MarineProvider BFF & provider-web untouched (git-clean).
6. Add unit tests mirroring any existing provider OTP tests: admin request generates+dispatches for an OTP-ready
   admin; synthetic for unknown/local-only admin (no `KeycloakSubjectId`); verify happy/invalid/expired/max-attempts;
   ticket minted with `clientId="admin-panel"`. Note in the report which provider tests you mirrored.

## 4. Explicitly OUT of scope (Kickoff 2)
- `infrastructure/keycloak/init.sh`: create an "Admin OTP Login browser" flow using the **same**
  `provider-otp-login-ticket` SPI with `allowedClientId=admin-panel`,
  `consumePath=/api/v1/identity/auth/admin-otp-login/consume-ticket`, same secrets; bind the `admin-panel` client to
  it. Plus a server-to-server call that fetches `admin.user@inktavia.com`'s real Keycloak id and provisions/links it
  in Identity (replacing the dev placeholder subject).
- `inktavia-marine-admin-web`: on `verify` success, perform the Keycloak redirect handoff with `login_ticket` (the FE
  is already forward-compatible; it currently shows the `keycloak_handoff_required` blocked state).

## 5. Report
Write `docs/V1.0.1/Identity/REPORT_BE_ADMIN_OTP_LOGIN.md`: files added/changed, the admin resolution rule you chose
(Admin role vs Admin profile) and why, entity-vs-reuse decision, migration name, the exact verification transcript
(request → log line masked → verify → ticket), tests mirrored, and the precise Kickoff-2 handoff (what subject id the
placeholder must be replaced with, and the init.sh config values).
