# REPORT — BE_M2a_PARTICIPANT_OTP_LOGIN

Identity-only participant (mobile) OTP-login vertical, mirrored from the Admin/Provider variants.
Verified end-to-end from Identity logs + HTTP (no Keycloak/BFF changes).

**Verdict: ✅ M2a VERIFIED** — builds 0 errors, migration applies cleanly, request→verify→consume works
with the masked `[DEV-ONLY]` OTP log, single-use 410 holds, and anti-enumeration generates no OTP.

---

## 1. Files added / changed (all under `Modules/Identity/`)

**Domain**
- `Aizen.Modules.Identity.Domain/Entities/OtpLogin/ParticipantOtpLoginRequestEntity.cs` *(new — mirror of `AdminOtpLoginRequestEntity`, `AdminProfileId`→`ParticipantProfileId`)*
- `Aizen.Modules.Identity.Domain/Entities/OtpLogin/ParticipantOtpLoginRequestEntityConfiguration.cs` *(new — table `participant_otp_login_requests`)*
- `Aizen.Modules.Identity.Domain/Interface/Service/IParticipantOtpLoginDomainService.cs` *(new)*

**Repository**
- `Aizen.Modules.Identity.Repository/Service/OtpLogin/ParticipantOtpLoginDomainService.cs` *(new — mirror of the Provider service; Participant profile gate; mints with `inktavia-mobile`)*
- `Aizen.Modules.Identity.Repository/Context/InktaviaStoreIdentityDbContext.cs` *(+2 lines: `DbSet<ParticipantOtpLoginRequestEntity>` + `ApplyConfiguration`)*
- `Aizen.Modules.Identity.Repository/DependencyInjection.cs` *(+1 line: `IParticipantOtpLoginDomainService → ParticipantOtpLoginDomainService`, in the same method as provider/admin)*
- `Aizen.Modules.Identity.Repository/Context/Seed/SeedIdentityBase.cs` *(+~76 lines: DEV-only OTP-ready participant seed block)*
- `Aizen.Modules.Identity.Repository/Migrations/20260805120000_AddParticipantOtpLoginRequests.cs` + `.Designer.cs` *(new migration)*
- `Aizen.Modules.Identity.Repository/Migrations/IdentityDbContextModelSnapshot.cs` *(+participant entity block)*

**Application** (thin commands delegating to the domain service; reuse the shared DTOs)
- `Auth/Command/OtpLogin/RequestParticipantOtpLogin/{Command,Handler,Validator}.cs` *(new)*
- `Auth/Command/OtpLogin/VerifyParticipantOtpLogin/{Command,Handler,Validator}.cs` *(new)*
- `Auth/Command/OtpLogin/ResendParticipantOtpLogin/{Command,Handler,Validator}.cs` *(new)*

**Web**
- `Aizen.Modules.Identity/Controller/V1/Identity/ParticipantOtpLoginController.cs` *(new — `api/v1/identity/auth/participant-otp-login`, `request`/`verify`/`resend` `[Authorize(Policy="IdentityWrite")]` + `consume-ticket` `[AllowAnonymous]` + `X-Otp-Login-Consume-Secret` guard. Deliberately **no** `participant-provision` endpoint — provisioning is M2d.)*

**Migration name:** `20260805120000_AddParticipantOtpLoginRequests` → creates table `participant_otp_login_requests` (+ unique index on `LoginRequestId`).

### Build & migration
- `dotnet build Aizen.Modules.Identity.csproj` → **Build succeeded, 0 Error(s)**.
- Migration applied cleanly via the app's own startup migrator (`IdentityDbContext.MigrateAsync()`); `identity-api` log:
  `Applying migration '20260805120000_AddParticipantOtpLoginRequests'.` → `CREATE TABLE participant_otp_login_requests …` → history row `('20260805120000_AddParticipantOtpLoginRequests','8.0.7')`.

> **Tooling note (no repo impact):** `dotnet ef migrations add` could not run in this multi-SDK box
> (global `dotnet-ef` vs. default SDK 10.0.201 → `System.Runtime 8.0.0.0` host mismatch; bumping the
> global tool 9.0.0→9.0.9 and pinning SDK 9 via a *temporary* `global.json` both failed). The
> migration was therefore hand-authored by mirroring `20260729182024_AddAdminOtpLoginRequest`
> (Up/Down + Designer + snapshot) and **proven to apply cleanly** through the runtime migrator above.
> The temporary `global.json` was removed (confirmed absent).

---

## 2. Reuse confirmation (NOT duplicated)

The participant vertical reuses the shared building blocks exactly as the Admin variant does — no
second ticket service, notifier, options, or DTOs were created:

| Shared component | Reused as-is |
| --- | --- |
| `IProviderOtpLoginTicketService` (`MintAsync`/`ConsumeAsync`) | injected into the domain service; controller calls `ConsumeAsync` |
| `IProviderOtpLoginNotifier` (`Logging*`/`MessageBus*`) | injected; emits the shared `[DEV-ONLY]` OTP log |
| `OtpLoginOptions` (`"OtpLogin"`) / `OtpLoginTicketOptions` (`"OtpLoginTicket"`) | injected; no new sections |
| `PasswordRecoverySecurity` | OTP/opaque-token/masking/hashing |
| DTOs `RequestProviderOtpLoginRequest` / `VerifyProviderOtpLoginRequest` / `ResendProviderOtpLoginRequest` / `*Response` / `ConsumeTicketRequest{Jti}` | request/response contracts |
| `OtpLoginRequestResult` / `OtpLoginVerifyResult` / `OtpLoginResendResult` | domain results |

The **only** behavioral differences vs. Admin/Provider: gate on an active **Participant** context
(`GetActiveProfileIdAsync(userId, WorkshopRoleContext.Participant)`; missing → the same synthetic
anti-enumeration result), persist to `ParticipantOtpLoginRequestEntity`, and mint the ticket with
**`clientId = "inktavia-mobile"`**.

---

## 3. Dev seed used (verification enabler)

Added a DEV/Local-only block in `SeedIdentityBase` (mirrors the existing OTP-ready **admin** block):
ensures Identity user **`mobile.user@inktavia.com`** (config `Seed:Participant:Email`), `EmailConfirmed`,
`Consumer` role, an **active Participant profile**, and a `KeycloakSubjectId`
(config `Seed:Participant:KeycloakSubjectId`, else dev placeholder `00000000-0000-0000-0000-participant1`
with a `[WARN]`). Linking the **real** Keycloak subject is M2d provisioning; this minimal DEV link lets
the Identity-only log test run. Startup log confirmed:
`[WARN] SeedIdentityBase: Participant user assigned dev-placeholder KeycloakSubjectId. M2d provisioning must replace it…`

---

## 4. Acceptance transcript (identity-api on `http://localhost:7101`)

`request`/`verify` authorized with an `IdentityWrite` service token (Keycloak client-credentials for
`admin-panel-bff`, which carries `identity_write` / `identity.write` / `identity.admin`).

**(2) REQUEST** — valid participant → **200**, masked target, `loginRequestId`:
```json
{"header":{"isSuccess":true,"errorCode":0},
 "body":{"accepted":true,"loginRequestId":"QpKfy2FYwpuK_7WM9O_YJRyfvc3AwaT8vpDVsJ96Z6s",
         "maskedTarget":"m***@inktavia.com","otpLength":6,"expiresInSeconds":300,"resendAfterSeconds":60,
         "message":"If an account exists, a verification code has been sent."}}
```
Identity `[DEV-ONLY]` log (masked identifier):
```
[DEV-ONLY] OTP login code for m***@inktavia.com: 908738
```

**(3) VERIFY** `{loginRequestId, otpCode:"908738"}` → **200**:
```json
{"header":{"isSuccess":true,"errorCode":0},
 "body":{"verified":true,"nextAction":"redirect_to_keycloak_handoff",
         "loginTicket":"eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC1wYXJ0aWNpcGFudDEi…<masked>",
         "expiresInSeconds":120,"message":"Code verified. Redirecting to complete sign-in."}}
```
Decoded ticket payload → `sub=00000000-0000-0000-0000-participant1`, **`clientId=inktavia-mobile`**, `jti=3096da019c024fbbb22a57406c9df840`.

**(4) CONSUME-TICKET** (header `X-Otp-Login-Consume-Secret: <ConsumeSecret>`, `{jti}`):
```
first  → HTTP 200  {"consumed":true,"sub":"00000000-0000-0000-0000-participant1"}
second → HTTP 410  {"consumed":false}          # single-use enforced
```

**(5) ANTI-ENUMERATION** — `request` with unknown identifier → **same 200 synthetic response**, and the
`[DEV-ONLY]` OTP log count was unchanged (before=1, after=1 → **no OTP generated/logged**):
```json
{"header":{"isSuccess":true,"errorCode":0},
 "body":{"accepted":true,"loginRequestId":"4snbTit07C0s43_-GJ50cQGjmOfewNHOQQgd6JM1BWU",
         "maskedTarget":"d***@nowhere.test","otpLength":6,"expiresInSeconds":300,"resendAfterSeconds":60,
         "message":"If an account exists, a verification code has been sent."}}
```

**Auth negatives:** `request` without a token → **401**; `consume-ticket` with a wrong secret → **401**.

No secrets or full tokens were logged; OTP identifiers are masked; the login ticket is masked above.

---

## 5. Scoped `git status`

My M2a changes are entirely within `Modules/Identity/**` (the file list in §1). During this task the
working tree was auto-committed by the user into commit `31744ed "fix channel side"`, so
`git status --porcelain` now shows a clean tree for these files.

Verification of scope: every M2a path in that commit is under `Modules/Identity/`. The **Provider/Admin
OTP-login files are byte-for-byte unchanged** (not in the commit's participant paths), and **no BFF,
`infrastructure/keycloak/`, provider-web, admin-web, or CargoDry files were touched by me**.

> Transparency: commit `31744ed` also bundled unrelated **Notification-preference** work
> (`Modules/Notification/**`, `Bff/**/Notifications*`, some docs) that was already pending in the working
> tree at the start of this task. That work is **not mine** — I neither authored nor modified those
> files; the user's commit simply swept the whole tree. My edits are limited to the `Modules/Identity`
> participant-OTP files listed in §1.

---

## 6. Precise M2b handoff

The Keycloak "Participant OTP Login browser" flow (M2b) must, after the mobile app presents the
`login_ticket`, call Identity to redeem it:

- **Consume endpoint:** `POST /api/v1/identity/auth/participant-otp-login/consume-ticket`
- **Auth:** header `X-Otp-Login-Consume-Secret: <OtpLoginTicket:ConsumeSecret>` (env `OTP_LOGIN_CONSUME_SECRET`, shared with provider/admin)
- **Body:** `{ "jti": "<jti from the login_ticket payload>" }`
- **Success:** `200 { "consumed": true, "sub": "<participant keycloak subject>" }`; replay → `410 { "consumed": false }`
- **`allowedClientId` the flow must enforce:** **`inktavia-mobile`** — the ticket payload carries
  `clientId=inktavia-mobile` (HMAC-signed with `OtpLoginTicket:Secret` / `OTP_LOGIN_TICKET_SECRET`); the
  flow should mint/authorize only for `inktavia-mobile`. Ticket TTL = 120s (`OtpLoginTicket:TtlSeconds`).

M2c then wires the mobile BFF `/mobile/auth/otp/send|verify` + the native auth-code+PKCE handoff; M2d
replaces the dev-placeholder subject with the real Keycloak subject via `ProvisionParticipantFromKeycloak`.
