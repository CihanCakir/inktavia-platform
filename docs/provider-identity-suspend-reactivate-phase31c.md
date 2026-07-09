# Provider Identity — Suspend / Reactivate Lifecycle (Phase 31C)

> Build note: no .NET SDK in the authoring environment — verify with `dotnet build` locally.

## Domain methods (`UserProfileEntity`)
- `Suspend(string? reason = null)` — sets `ProfileStatus = Suspended`; **does not** change `ApprovalStatus`;
  idempotent; optional `reason` stored in the existing `InternalNote` field when provided.
- `Reactivate()` — sets `ProfileStatus = Active`; **requires** `ApprovalStatus == Approved` (throws
  `AizenBusinessException(ProfileStatusInvalidForAction)` otherwise); idempotent; never approves implicitly.

## Commands (folder-per-feature, Organizer)
```
Organizer/Command/SuspendOrganizerProfile/{Command, CommandHandler, CommandValidator}.cs
Organizer/Command/ReactivateOrganizerProfile/{Command, CommandHandler, CommandValidator}.cs
```
- **Suspend** input: `UserId`, `ProfileId`, `Reason?` → `SuspendOrganizerProfileResponse`.
- **Reactivate** input: `UserId`, `ProfileId` → `ReactivateOrganizerProfileResponse`.

Handler flow (both): load profile by `UserId + ProfileId + RoleContext==Organizer + !IsDeleted` (via
`IdentityDbContext`, `Include(User)`); if not already in target state, apply the domain method + `SaveChanges`;
then best-effort Keycloak role sync (try/catch, log-only, no rollback). Idempotent when already in target state.

## Endpoints (AdminController — `[Authorize(Roles = Admin)]`, thin)
```
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/suspend      body: { "reason"?: "..." }
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reactivate    body: {}
```
Not anonymous; not provider self-service; controllers only dispatch + return the envelope.

## Keycloak role sync wiring
| Command | Service call | Roles |
|---|---|---|
| Approve (existing) | `OnApprovedAsync` | −provider_pending, +provider_user, −provider_restricted |
| **Suspend (new)** | `OnSuspendedAsync` | −provider_user, +provider_restricted, (optional) revoke sessions |
| **Reactivate (new)** | `OnReactivatedAsync` | +provider_user, −provider_restricted |

Session revoke on suspend is **invoked** by `OnSuspendedAsync` when `IdentityKeycloak:RevokeSessionsOnSuspend=true`
(Keycloak `POST /users/{id}/logout`).

## Failure handling
Keycloak sync failures are logged (safe, no secrets/tokens) and **never roll back** the Identity status change.
The typed responses carry status but no warnings field, so sync failures are **log-only**. Future: outbox + retry
job for guaranteed delivery.

## Authoritative status
Runtime Identity `ApprovalStatus`/`ProfileStatus` remains authoritative and is checked on every provider request
(BFF `ProviderActive`/`ProviderPendingOrActive` policies + resolver). Keycloak roles are a coarse helper only.

## Notes / gaps
- No suspend-specific column; `reason` reuses `InternalNote`. A dedicated `SuspendReason`/`SuspendedAt` column is a
  future option (would need a migration).
- `Reactivate` is the path to `ProfileStatus = Active`. If the product wants approval to also activate immediately,
  that is a separate change to `ApproveOrganizerProfile` (out of scope here).
