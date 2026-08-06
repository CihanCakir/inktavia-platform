# BE_M3a — Mobile participant profile read/update (BFF)

**Date:** 2026-08-06 · **Repos:** `addesso-project` (BFF) + `inktavia-marine-mobile` (RN). First M3 slice.
Build 0 errors, redeployed, HTTP-verified. No new Identity endpoint (reused existing).

## What
Exposed the participant profile on the mobile BFF, mirroring the MarineProvider provider-profile pattern, and
wired the RN ProfileScreen to it. Read resolves by Keycloak subject; update is an asserted call to the existing
Identity participant endpoint.

- `GET  /api/v1/mobile/profile/me` → `body { hasProfileLink, profile { participantProfileId, firstName, lastName, fullName, email, phone, avatarUrl, bio, gender, birthDate, nationalityId, approvalStatus, profileStatus }, message }`
- `PUT  /api/v1/mobile/profile/me` `{ firstName, lastName, bio, gender?, birthDate?, nationalityId? }` → the persisted profile (same body).
- Both `[Authorize]` `ParticipantAuthenticated` (mobile_user).

## How (mirrors MarineProvider `ProviderProfileResolver`)
- **Read:** `ParticipantProfileResolver` resolves the caller's Keycloak subject → Identity `GET /api/v1/identity/participant/profiles/by-subject/{sub}` (non-admin, service-token authorized; returns email/phone via `profile.User`). Maps `OrganizerProfileDetailDto` → `ParticipantProfileDto` (`FullName` = First + Last, `AvatarUrl` = ProfilePhotoUrl).
- **Update:** resolve-by-subject first — this populates `IParticipantIdentityHolder`, so the `MarineMobileBffAuthDelegatingHandler` attaches the trusted-BFF identity assertion (`X-Aizen-Bff-Assertion` + `X-Aizen-User-Id`) to the subsequent call → Identity `PUT /api/v1/identity/participant/profile` resolves `UserInfo.UserId` to the right participant. Then re-resolve and echo the fresh profile.
- **Keycloak declarative-profile gotcha — N/A here:** the existing Identity participant-update writes the Identity profile table only (DDD entity + message permissions), NOT Keycloak. So there's no GET→controlled-PUT and no managed-field wipe risk. Consequence: firstName/lastName changes are immediately visible via `/profile/me` (which reads the Identity table); the Keycloak JWT `name` claim is unchanged until the next login (expected).
- **Read-only fields (M3a):** `email` and `phone` are auth identifiers (phone is the OTP/login handle) — not editable via this endpoint; `avatarUrl` is M3c. The BFF update DTO deliberately omits them.

## Files
**BFF (`Bff/src/Marine.Participant.Mobile/**`) — all new except the two edits:**
- `Application/Common/Services/ParticipantProfileResolver.cs` (new) — by-subject resolve + holder set.
- `Application/Contracts/Profile/{ParticipantProfileDto,GetParticipantProfileResponse,UpdateParticipantProfileBffRequest}.cs` (new).
- `Application/Profile/Query/GetParticipantProfile/{GetParticipantProfileQuery,GetParticipantProfileQueryHandler}.cs` (new).
- `Application/Profile/Command/UpdateParticipantProfile/{UpdateParticipantProfileBffCommand,…Handler}.cs` (new).
- `Aizen.Bff.Marine.Participant.Mobile/Controllers/V1/ProfileController.cs` (new).
- `Application/Common/RemoteClients/IIdentityRemoteCall.cs` (edit) — add `PUT participant/profile` (`UpdateParticipantProfile`); by-subject GET already existed.
- `Application/DependencyInjection.cs` (edit) — register `IParticipantProfileResolver`.

**Config (`docker-compose.yaml`, identity-api):**
- Added `BffAssertion__AllowedClientIds__2: marine-mobile-bff`. **Required:** Identity's `AizenUserInfoMiddleware` only honors a BFF assertion whose service-token `azp` is allow-listed (previously only `provider-portal-bff`, `admin-panel-bff`). Without it the asserted PUT fails with business error **1102**. Env-only (no image rebuild). Later mobile write-phases to other modules will need the same line on those services.

**No Identity source changed** — the participant profile read/update endpoints already existed and are reused as-is.

## Verification (redeployed `bff-marine-mobile`; identity-api recreated for the env; `localhost:17003`; user `qa.owner.aug5@inktavia.com` / `+905551002026` / `QaReset2026!!`)

```
# GET (initial)
GET /api/v1/mobile/profile/me  (Bearer mobile_user)
 → 200 { header.isSuccess:true, body.profile: {
     participantProfileId:100030, firstName:"QA", lastName:"Owner Aug5", fullName:"QA Owner Aug5",
     email:"qa.owner.aug5@inktavia.com", phone:"+905551002026",
     approvalStatus:"Pending", profileStatus:"Inactive" } }

# PUT (firstName→Quinn, lastName→Overton, bio)
PUT /api/v1/mobile/profile/me { "firstName":"Quinn","lastName":"Overton","bio":"Yacht owner, Aegean." }
 → 200 body.profile.fullName:"Quinn Overton", bio:"Yacht owner, Aegean.", email+phone unchanged

# GET (fresh login) — persistence confirmed
GET /api/v1/mobile/profile/me → firstName:"Quinn" lastName:"Overton" fullName:"Quinn Overton"
     bio:"Yacht owner, Aegean." email:"qa.owner.aug5@inktavia.com" phone:"+905551002026"

# Negative / regression
- Before the allow-list fix: PUT → 400 (Identity AizenBusinessException 1102, assertion rejected). Fixed by adding marine-mobile-bff.
- Email/password login still 200 (auth unaffected).
- Test user restored to firstName "QA" / lastName "Owner Aug5" after verification.
```

## Scope (git status — M3a only)
```
 M Bff/.../Common/RemoteClients/IIdentityRemoteCall.cs
 M Bff/.../DependencyInjection.cs
 M docker-compose.yaml                          (identity-api BffAssertion allow-list +1 line)
?? Bff/.../Common/Services/ParticipantProfileResolver.cs
?? Bff/.../Contracts/Profile/
?? Bff/.../Profile/                              (Query + Command)
?? Bff/.../Controllers/V1/ProfileController.cs
```
Only the mobile BFF + one identity-api env line. No Identity source, no Provider/Admin, no in-tree
Notification/Payment/ServiceRequest/Travel work touched. Not committed.
