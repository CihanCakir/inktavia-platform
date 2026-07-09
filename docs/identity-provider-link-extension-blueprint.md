# Identity Provider-Link Extension — Blueprint (for the next controlled phase)

> **No Identity code was changed in Phase 31** (BFF-first, shared module untouched, no build tooling in the
> authoring environment). This blueprint specifies exactly what the Identity module must add so the
> MarineProvider BFF can fully provision and link providers by Keycloak subject.

## 1. KeycloakSubject field — ALREADY EXISTS ✅

`UserEntity.KeycloakSubjectId` (nullable) + `SetKeycloakSubjectId(...)` already exist, with:
- EF config: `UserEntityConfiguration` maps it with a filtered unique index (`HasIndex … HasFilter("\"KeycloakSubjectId\" IS NOT NULL")`).
- Migration: `20260520195317_AddKeycloakSubjectId` is already applied.

**No new field or migration is required for the subject link itself.** Recommended canonical name stays
`KeycloakSubjectId` (do not rename).

## 2. Required: lookup by Keycloak subject

Add a provider-safe query + repository method + HTTP endpoint:

- Repository (`IUserProfileRepository` / `IUserRepository`): `Task<UserProfileEntity?> GetOrganizerProfileByKeycloakSubjectAsync(string keycloakSubjectId)` — join `UserProfileEntity` (RoleContext = Organizer) to `UserEntity` by `KeycloakSubjectId`.
- Query: `GetOrganizerProfileByKeycloakSubjectQuery(string keycloakSubject)` → `OrganizerProfileDetailDto?`.
- Controller: `GET /api/v1/identity/organizers/profiles/by-subject/{keycloakSubject}` (service-token authorized; returns 404 when unlinked).

BFF consumer: add to `IProviderIdentityRemoteCall` and use it in `EnsureProviderProfileCommandHandler` and the
`ProviderContext` fallback (claim → sub → this lookup).

## 3. Required: idempotent provisioning from Keycloak

Add `ProvisionOrganizerFromKeycloakCommand`:
```
Input:  KeycloakSubject (required), Email, FirstName, LastName, CompanyName?, ContactPhone?, TaxNo?
Output: ProviderProfileId, Created (bool)
Rules:
  - find UserEntity by KeycloakSubjectId; else by Email; else create a user WITHOUT a local password
    (new UserEntity factory e.g. CreateFromKeycloak(email, phone, LoginType.Keycloak) — no password hash,
     SetKeycloakSubjectId(sub));  DO NOT use CreateLocal + password, DO NOT call IOAuthProviderClient.
  - ensure single Organizer profile (HasProfileForContextAsync); create UserProfileEntity.Create(...) with
    RoleContext = Organizer, ApprovalStatus = Pending, ProfileStatus = Inactive (defaults already do this).
  - set CompanyName / City / Country when provided (SetCompanyName / SetLocation).
  - idempotent: repeat calls return the existing profile id, never duplicate.
Controller: POST /api/v1/identity/organizers/provision-from-keycloak (service-token authorized).
```
Model on the existing `OrganizerRegistrationDomainService.RegisterOrAttachAsync` but **omit password + agreements-by-password path**. Consider a sibling `IOrganizerKeycloakProvisioningDomainService`.

BFF consumer: call this from `RegisterProviderCommandHandler` (after Keycloak user create) and
`EnsureProviderProfileCommandHandler` (social first login). After it returns `ProviderProfileId`, the BFF writes
the `provider_profile_id` Keycloak user attribute.

## 4. Required: LoginType / auth-provider marker (optional but recommended)

Add `LoginType.Keycloak` (enum currently: Email, etc.) or an `AuthProvider` marker on `UserEntity` so
Keycloak-provisioned users are not treated as legacy-password users anywhere.

## 5. phoneVerified persistence — GAP

`UserProfileEntity` has **no** `PhoneVerified` field. Phase 31 BFF endpoints `POST /me/phone/send-otp` /
`verify-otp` call the existing Identity OTP (`/api/v1/auth/otp/send` + `otp/check`) but **cannot persist**
verification. Required change:
- Add `bool PhoneVerified` (+ `DateTime? PhoneVerifiedAt`) to `UserProfileEntity` with a domain method `MarkPhoneVerified()`.
- **Migration required** (single column add; low risk).
- Add a command `MarkOrganizerPhoneVerifiedCommand(profileId)` + endpoint, called by the BFF verify handler.

## 6. Role synchronization follow-up (Phase 32/33)

Identity is authoritative for approval/suspension; Keycloak roles must follow:
- On approval (`ApprovalStatus → Approved`): move Keycloak role `provider_pending → provider_user`.
- On suspend (`ProfileStatus → Suspended`): optionally revoke Keycloak sessions; keep runtime status as the gate.
- Implement as an **event consumer / outbox** on Identity approval/suspend domain events → calls Keycloak Admin API
  (reuse the BFF Admin client pattern or a small Identity-side Keycloak admin client). **Do not** change Admin
  approval flows beyond emitting/consuming the event.

## 7. Exact files to change later

```
Modules/Identity/src/Aizen.Modules.Identity.Domain/Entities/User/UserEntity.cs            # CreateFromKeycloak factory (+ LoginType.Keycloak)
Modules/Identity/src/Aizen.Modules.Identity.Domain/Entities/UserProfile/UserProfileEntity.cs  # PhoneVerified (+ MarkPhoneVerified)
Modules/Identity/src/Aizen.Modules.Identity.Domain/Interface/Repository/IUserProfileRepository.cs  # GetOrganizerProfileByKeycloakSubjectAsync
Modules/Identity/src/Aizen.Modules.Identity.Repository/Repository/UserProfileRepository.cs # impl
Modules/Identity/src/Aizen.Modules.Identity.Repository/Service/OrganizerKeycloakProvisioningDomainService.cs # new
Modules/Identity/src/Aizen.Modules.Identity.Application/Organizer/Query/GetOrganizerProfileByKeycloakSubject/*  # new query
Modules/Identity/src/Aizen.Modules.Identity.Application/Organizer/Command/ProvisionOrganizerFromKeycloak/*      # new command
Modules/Identity/src/Aizen.Modules.Identity.Application/Organizer/Command/MarkOrganizerPhoneVerified/*          # new command
Modules/Identity/src/Aizen.Modules.Identity/Controller/V1/Identity/RegistrationController.cs (or new)           # expose provision + by-subject + phone-verified
Modules/Identity/src/Aizen.Modules.Identity.Repository/Migrations/*  # PhoneVerified migration (KeycloakSubjectId already migrated)
```

## 8. Migration plan

1. `KeycloakSubjectId` — **already migrated**, no action.
2. `PhoneVerified` (+ `PhoneVerifiedAt`) on `UserProfileEntity` — add `dotnet ef migrations add AddOrganizerPhoneVerified` in `Aizen.Modules.Identity.Repository`; review; apply.
3. `LoginType.Keycloak` enum value — no schema change (stored as int); ensure seed/consistency.

## 9. Risks

- **User store duplication:** provisioning must dedupe by `KeycloakSubjectId` then `Email` to avoid duplicate users/profiles.
- **Password-less users:** ensure no legacy login path assumes a password hash for Keycloak-provisioned users.
- **Module auth:** the `provider-portal-bff` service account needs `identity_read` (and write, for provisioning) client roles or the new endpoints will 401/403.
- **Idempotency + compensation:** provisioning and attribute-write must be safe to retry; the BFF already treats them as best-effort with warnings.

## 10. Build verification (when implemented)

```
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Aizen.Modules.Identity.Abstraction.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
```
