# Provider Keycloak Role Sync (Phase 31B/C)

## Strategy

**Direct service-call** (not event/outbox). Audit finding: no approval/suspend domain event is published
today, so there is nothing to attach a consumer to. A small Identity-side Keycloak Admin service is called
explicitly from the approval command handler after the status change succeeds. This can be moved to an
outbox/retry consumer later without changing the role logic.

## Components
- `IProviderKeycloakRoleSyncService` (`Identity.Domain/Interface/Service`) — `OnApprovedAsync`,
  `OnSuspendedAsync`, `OnReactivatedAsync`.
- `ProviderKeycloakRoleSyncService` (`Identity.Repository/Service`) — client_credentials admin token,
  realm role add/remove, optional session logout. Never logs secrets/tokens.
- `IdentityKeycloakOptions` (`Identity.Abstraction/Options`, section `IdentityKeycloak`).
- Registered in `Identity.Repository/DependencyInjection.AddInktaviaService`.

## Wired handlers
- **`ApproveOrganizerProfileCommandHandler`** → `OnApprovedAsync(user.KeycloakSubjectId)` after approve+save.
  Best-effort: wrapped in try/catch, logs on failure, **does not roll back approval**.

## Role transitions
| Trigger | Roles |
|---|---|
| Approved | remove `provider_pending`, add `provider_user`, remove `provider_restricted` |
| Suspended | remove `provider_user`, add `provider_restricted`, (optional) revoke sessions if `RevokeSessionsOnSuspend=true` |
| Reactivated (Approved+Active) | add `provider_user`, remove `provider_restricted` |

## Suspend / reactivate status — WIRED (Phase 31C)
`OnSuspendedAsync` and `OnReactivatedAsync` are now **wired** to the new Identity commands
`SuspendOrganizerProfileCommandHandler` / `ReactivateOrganizerProfileCommandHandler` (admin endpoints
`.../suspend` and `.../reactivate`). `UserProfileEntity.Suspend()` / `Reactivate()` domain methods were added.
Session revoke is implemented (`/users/{id}/logout`) and invoked by `OnSuspendedAsync` when
`RevokeSessionsOnSuspend=true`. See `provider-identity-suspend-reactivate-phase31c.md`.

## Failure handling
No rollback of Identity status on Keycloak failure. Errors are logged (safe, no secrets). The approval
response DTO (`VenueOrganizationRegistrationResponse`) has no warnings field, so failures surface only in
logs. Future: outbox + retry job for guaranteed eventual consistency.

## Critical rule
Role sync is a **coarse** Keycloak authorization helper. **Runtime Identity ApprovalStatus/ProfileStatus
remains authoritative** and is checked on every provider request by the BFF policies/resolver.

## Configuration (`IdentityKeycloak` — identity-api appsettings/env)
```
IdentityKeycloak:Enabled=true
IdentityKeycloak:BaseUrl=http://keycloak:8080
IdentityKeycloak:Realm=inktavia-realm
IdentityKeycloak:AdminClientId=provider-portal-bff
IdentityKeycloak:AdminClientSecret=${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}
IdentityKeycloak:ProviderPendingRole=provider_pending
IdentityKeycloak:ProviderUserRole=provider_user
IdentityKeycloak:ProviderRestrictedRole=provider_restricted
IdentityKeycloak:RevokeSessionsOnSuspend=false
```
When `Enabled=false` or `BaseUrl` empty, the service is a safe no-op — existing approval flows are unaffected
in environments without Keycloak. The service account needs realm-management `manage-users`.
