# MarineProvider — Runtime Auth Smoke (Phase 31C Report)

> Environment note: no .NET SDK and **no reachable Keycloak** in the authoring environment. Code + artifacts
> were **statically validated** (bash `bash -n` OK, JSON valid); build and runtime smoke must be executed locally.

## 1. What was implemented
Identity Organizer **suspend/reactivate** lifecycle (domain methods + commands + validators + admin endpoints),
wired to the existing Keycloak role-sync service (`OnSuspendedAsync`/`OnReactivatedAsync`). Approval role sync
(`OnApprovedAsync`) unchanged and still wired. Keycloak realm artifacts unchanged (prepared).

## 2. Suspend/reactivate commands & endpoints
- `SuspendOrganizerProfileCommand` → `POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/suspend`
- `ReactivateOrganizerProfileCommand` → `POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reactivate`
- Admin-only (`[Authorize(Roles = Admin)]`), thin controller methods, typed responses.

## 3. Domain methods added
`UserProfileEntity.Suspend(reason?)`, `UserProfileEntity.Reactivate()` (see `provider-identity-suspend-reactivate-phase31c.md`).

## 4. Role-sync wiring
Approve→`OnApprovedAsync`; Suspend→`OnSuspendedAsync`; Reactivate→`OnReactivatedAsync`. All best-effort
(try/catch, log-only, no Identity rollback).

## 5. Session revoke behavior
Implemented and **invoked** by `OnSuspendedAsync` when `IdentityKeycloak:RevokeSessionsOnSuspend=true`
(`POST /admin/realms/{realm}/users/{id}/logout`). Default false.

## 6. Build results
**Not built here (no SDK).** Run Part F commands locally. Static checks passed: realm script `bash -n` OK,
partial-import JSON valid, no duplicate CQRS types, no stale namespaces.

## 7. Keycloak setup execution / validation
**Prepared, statically validated, not executed** (Keycloak unreachable). `setup-provider-realm.sh` passes
`bash -n`. Run it (or partial import + manual grants) per `infrastructure/keycloak/provider-realm/README.md`.

## 8–15. Runtime smoke (prepared — execute locally)

| # | Scenario | Steps | Expected |
|---|---|---|---|
| H1 | Register (email/pw) | `POST /provider/auth/register` | Keycloak user created; `provider_pending` role; Identity Organizer profile provisioned; `provider_profile_id` attribute written; Verify Email sent |
| H2 | Token / status gate | login → inspect token → `GET /provider/me/status` | token `aud=[provider-portal-bff]`, `realm_access.roles=[provider_pending]`, `provider_profile_id` claim; status `AwaitApproval`, `CanEnterWorkspace=false` |
| H3 | Approval role sync | `POST admin/.../approve` | Identity `ApprovalStatus=Approved`; Keycloak −pending +user −restricted; after re-login token has `provider_user`; `/me/status` `EnterWorkspace` once `ProfileStatus=Active` |
| H4 | Suspend role sync | `POST admin/.../suspend` | Identity `ProfileStatus=Suspended`; Keycloak −user +restricted; sessions revoked if configured; `/me/status` `Suspended`; `ProviderActive` policy denies operational access |
| H5 | Reactivate role sync | `POST admin/.../reactivate` | Identity `ProfileStatus=Active`; Keycloak +user −restricted; `/me/status` `EnterWorkspace` |
| H6 | Google ensure-profile | Google login → `POST /provider/auth/ensure-profile` | Organizer profile exists/provisioned; `provider_profile_id` attribute written; status `Linked` |
| H7 | Phone OTP | `POST /me/phone/send-otp` → `verify-otp` | `PhoneVerifiedPersisted=true`; Identity `PhoneVerified=true`, `PhoneVerifiedAt` set |

Note (H3): approval sets `ApprovalStatus=Approved` but not `ProfileStatus`; use **Reactivate** to set `Active`
(or extend Approve if the product wants immediate activation — out of scope).

## 16. Remaining gaps
- Local build + runtime smoke not executed here.
- Keycloak realm objects must be created by running the artifacts.
- Suspend `reason` reuses `InternalNote` (no dedicated column).
- Role-sync failures are log-only (no outbox/retry yet).
- AdminPanel BFF suspend/reactivate passthrough not added (documented as optional follow-up).

## 17. Phase 31C complete?
**Code-complete.** Suspend/reactivate lifecycle + role-sync wiring done; realm artifacts prepared + statically
validated. Closure requires the local **build + Keycloak setup run + smoke execution** (H1–H7).

## 18. Phase 32 can start?
After the local build is green and the H1–H7 smoke passes against a configured Keycloak. Until then, hold Phase 32.
