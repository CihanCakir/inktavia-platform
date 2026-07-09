# Phase 31B — Identity Provider-Link Extension + MarineProvider BFF Cleanup (Report)

> Build note: this environment has **no .NET SDK**, so nothing was compiled here. Code follows audited
> project conventions; **build + `dotnet ef migrations add` must be run locally** (see §H, §M).

## A. Scope

Two objectives: (1) close the Phase 31A identity-link gaps in the Identity module (lookup-by-subject,
provision-from-Keycloak, password-less user, phoneVerified), and (2) clean up the MarineProvider BFF
(Program.cs extraction, thin controllers, resolver-based profile resolution). Keycloak stays the full IdP;
Identity stays the domain/profile store. No federation/SPI, no `IOAuthProviderClient`, no legacy token auth,
no Phase 32 operational endpoints.

## B. MarineProvider BFF Cleanup Summary

- **Program.cs** reduced to service-registration + `app.Run()`. Inbound JWT/Keycloak setup moved to
  `Extensions/AuthenticationExtensions.cs` (`AddMarineProviderAuthentication`). Chained:
  `AddMarineProviderBffApplication → AddMarineProviderAuthentication → AddMarineProviderAuthorization`.
- **Controllers** remain thin (command/query dispatch + envelope only); no Keycloak/Identity/resolution logic in them.
- **Profile resolution** centralized in a new `IProviderProfileResolver` (claim → sub → by-subject lookup).
- **Partial:** the folder-per-feature rename (e.g. `Auth/RegisterProvider/`) and strict one-file-per-type split
  were **not** fully applied because this environment cannot delete/rename files (only create/modify). Doing it
  via overwrite+create at volume without a compiler risked stray/duplicate types. Feature files remain in their
  current folders with the handler as a separate top-level class. Target layout is documented in §D as a trivial
  local move.

## C. Program.cs Before/After

Before: ~90 lines with inline JWT bearer config, realm-role JSON flattening, and config parsing in Program.cs.
After: ~20 lines — `AizenApplicationBuilder` + a single fluent registration chain + `app.Run()`. All JWT logic
lives in `AuthenticationExtensions`.

## D. Application CQRS Folder Normalization

Applied: one handler class per handler (no inner handler/validator classes); Part C logic folded into the
existing handlers. Recommended target layout (local follow-up, blocked here by no file delete/rename):
```
Auth/RegisterProvider/{Command,CommandHandler,CommandValidator,Response}
Auth/EnsureProviderProfile/{Command,CommandHandler,Response}
Me/GetProviderMe|GetProviderProfile|GetProviderStatus/{Query,QueryHandler,Response}
Phone/SendProviderPhoneOtp|VerifyProviderPhoneOtp/{Command,CommandHandler,Response}
```
Currently these live under `Auth/Command`, `Me/Query`, `Phone/Command` with command+response+handler grouped
per feature file.

## E. Identity Lookup-by-Subject

- `IUserProfileRepository.GetOrganizerProfileByKeycloakSubjectAsync(sub, ct)` + impl (join UserProfiles→User,
  RoleContext=Organizer, `!IsDeleted`).
- `GetOrganizerProfileByKeycloakSubjectQuery` + handler (delegates to existing `GetOrganizerProfileDetailQuery`
  for the DTO projection; returns null when unlinked).
- Endpoint `GET /api/v1/identity/organizers/profiles/by-subject/{keycloakSubject}` (`IdentityRead` policy).

## F. Identity Provision-from-Keycloak

- `IOrganizerKeycloakProvisioningDomainService` + `OrganizerKeycloakProvisioningDomainService` (idempotent):
  find by subject → by email (link if unlinked / conflict if different subject) → else create **password-less**
  `UserEntity.CreateFromKeycloak` → ensure Organizer role → ensure single Organizer profile (Pending/Inactive) →
  set CompanyName when provided → `SaveChanges`. TaxNo has no profile field → returned as a warning (not faked).
- `ProvisionOrganizerFromKeycloakCommand` + handler + validator; result DTO
  `ProvisionOrganizerFromKeycloakResult` (Abstraction).
- Endpoint `POST /api/v1/identity/organizers/provision-from-keycloak` (`IdentityWrite` policy).
- Registered in `Aizen.Modules.Identity.Repository/DependencyInjection.cs`.

## G. Password-less Keycloak User Factory

- `UserEntity.CreateFromKeycloak(email, phone, keycloakSubjectId, emailVerified=false)` — no password hash / no
  password history; sets `KeycloakSubjectId` and `LoginType.Keycloak`.
- `LoginType.Keycloak = 6` added. Legacy login handlers are unaffected (they operate on password/OTP users).

## H. PhoneVerified Persistence + Migration

- `UserProfileEntity.PhoneVerified` (bool) + `PhoneVerifiedAt` (DateTime?) + `MarkPhoneVerified(utcNow)`.
- `MarkOrganizerPhoneVerifiedCommand` + handler + validator; endpoint
  `POST /api/v1/identity/organizers/profiles/{profileId:long}/phone/mark-verified` (`IdentityWrite`).
- **Migration:** the entity change is in place; generate the migration locally (the giant EF ModelSnapshot
  cannot be hand-edited safely without `dotnet ef`):
  ```
  dotnet ef migrations add AddOrganizerPhoneVerified \
    -p Modules/Identity/src/Aizen.Modules.Identity.Repository \
    -s Modules/Identity/src/Aizen.Modules.Identity
  ```
  Only `PhoneVerified` / `PhoneVerifiedAt` should appear. **Do not** create a migration for `KeycloakSubjectId`
  (already migrated in `20260520195317_AddKeycloakSubjectId`).

## I. MarineProvider BFF Integration Updates

- `IProviderIdentityRemoteCall`: added `GetOrganizerProfileByKeycloakSubject`, `ProvisionFromKeycloak`,
  `MarkPhoneVerified`.
- `RegisterProviderCommandHandler`: Keycloak user → **provision Identity profile** → write `provider_profile_id`
  attribute → assign `provider_pending` → Verify Email. Returns `ProviderProfileId`.
- `EnsureProviderProfileCommandHandler` (social): by-subject lookup → provision if missing → write attribute →
  ensure role → `Linked`.
- `IProviderProfileResolver`: claim → sub → by-subject; used by `GetProviderProfile`, `GetProviderStatus`.
- `VerifyProviderPhoneOtpCommandHandler`: on OTP success → resolve profile → `MarkPhoneVerified` →
  `PhoneVerifiedPersisted=true` (warning if not).

## J. Auth / Header Forwarding Confirmation

Unchanged and confirmed: inbound Keycloak RS256 (`Authorization: Bearer`, audience `provider-portal-bff`);
outbound to modules uses the Keycloak **service-account** token only. **No `X-Aizen-User-Token` is forwarded or
fabricated.** New Identity endpoints authorize via `IdentityRead`/`IdentityWrite` — the `provider-portal-bff`
service account must hold the `identity_read` and `identity_write` client roles on `identity-api`.

## K. Role Sync Status

Documented only (no consumer added). Audit finding: **no approval/suspend domain event is currently published**
by `ApproveOrganizerProfile` / suspend flows, so there is no event to attach a consumer to yet. Plan:
- Registration → `provider_pending` (done in BFF).
- Approval → emit a new `OrganizerApproved` domain event/outbox in Identity, then a consumer promotes Keycloak
  role `provider_pending → provider_user` via Admin API.
- Suspend → runtime status gate already blocks access; optionally emit `OrganizerSuspended` to revoke Keycloak
  sessions.
This is a Phase 32/33 follow-up; Admin approval flows are unchanged here.

## L. Endpoints Added / Updated

New (Identity): `GET organizers/profiles/by-subject/{sub}`, `POST organizers/provision-from-keycloak`,
`POST organizers/profiles/{id}/phone/mark-verified`.
Updated (BFF): `POST provider/auth/register`, `POST provider/auth/ensure-profile`, `GET provider/me/profile`,
`GET provider/me/status`, `POST provider/me/phone/verify-otp`.

## M. Build Results

Not built here (no SDK). Run locally:
```
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Aizen.Modules.Identity.Abstraction.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj
```
Then generate the `AddOrganizerPhoneVerified` migration (§H).

## N. Known Gaps

1. `AddOrganizerPhoneVerified` migration must be generated locally (entity ready; snapshot untouched by design).
2. Role-sync consumer not implemented; requires an approval/suspend event to be emitted first (none exists today).
3. Folder-per-feature rename / strict one-file-per-type split is partial (environment can't delete/rename files).
4. `provider-portal-bff` service account must be granted `identity_read` + `identity_write` client roles.
5. Realm objects (clients, roles, `provider_profile_id` mapper, audience mapper, Google IdP) must be created.
6. Not compiled here — verify locally.

## O. Next Phase Recommendation

**Phase 31C — Keycloak Realm Setup + Runtime Auth Smoke Test** (create clients/roles/mappers/Google IdP; run
register + social ensure-profile + `/provider/me/status` + phone OTP persistence end-to-end). Then Phase 32
operational read endpoints.
