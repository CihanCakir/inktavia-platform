# Notifications: Approval Events & Web Push — Report

**Date:** 2026-07-13
**Branch:** `feature/messaging-registration`

---

## Part A — Lifecycle Events

### A1. Message Contracts

Created 4 message contracts in `Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Message/`:

| Message | Payload |
|---------|---------|
| `ProviderProfileApprovedMessage` | ProfileId, UserId, Email, ProfileType, ApprovedAtUtc |
| `ProviderProfileRejectedMessage` | ProfileId, UserId, Email, ProfileType, Reason, ReasonCategory, RejectedAtUtc |
| `ProviderProfileSuspendedMessage` | ProfileId, UserId, Email, Reason, SuspendedAtUtc |
| `ProviderOnboardingRevisionRequestedMessage` | ProfileId, UserId, Email, Steps[], Note, RequestedAtUtc |

All extend `AizenBaseMessage`. No secrets, no signed URLs, no documents in payloads.

### A2. NotificationType Enum

Added to `Modules/Notification/src/Aizen.Modules.Notification.Abstraction/Enum/NotificationType.cs`:

```
ProfileApproved             = 401
ProfileRejected             = 402
ProfileSuspended            = 403
OnboardingRevisionRequested = 404
```

### A3. Publishing from Command Handlers

**Decision: publish from the command handlers**, not the domain service. This is consistent across all handlers and keeps the domain service focused on domain logic.

All 6 handlers now inject `IAizenMessagePublisher` and publish after the state change:

| Handler | File | Message |
|---------|------|---------|
| `ApproveOrganizerProfileCommandHandler` | `.../Organizer/Command/ApproveOrganizerProfile/` | `ProviderProfileApprovedMessage` (profileType: "organizer") |
| `RejectOrganizerProfileCommandHandler` | `.../Organizer/Command/RejectOrganizerProfile/` | `ProviderProfileRejectedMessage` (profileType: "organizer") |
| `SuspendOrganizerProfileCommandHandler` | `.../Organizer/Command/SuspendOrganizerProfile/` | `ProviderProfileSuspendedMessage` |
| `RequestProviderOnboardingRevisionCommandHandler` | `.../Organizer/Command/Onboarding/RequestProviderOnboardingRevision/` | `ProviderOnboardingRevisionRequestedMessage` |
| `ApproveVenueProfileCommandHandler` | `.../Venue/Command/ApproveVenueProfile/` | `ProviderProfileApprovedMessage` (profileType: "venue") |
| `RejectVenueProfileCommandHandler` | `.../Venue/Command/RejectVenueProfile/` | `ProviderProfileRejectedMessage` (profileType: "venue") |

**Note on timing:** Publishing happens inside the handler, after all repository updates but before the transactional decorator commits. The existing codebase (Payment, CargoDry) uses the same pattern. If the publish fails, the transaction rolls back — better than notifying about a rolled-back approval.

**Note on suspend:** `SuspendOrganizerProfileCommandHandler` only publishes when the profile was not already suspended (idempotent — no duplicate notifications).

**Note on revision:** `RequestProviderOnboardingRevisionCommandHandler` looks up the profile to obtain `UserId` and `Email` (not on the command). Only publishes if the profile is found.

**Venue suspension:** No `SuspendVenueProfileCommandHandler` exists in the codebase. Not created — out of scope.

### A4. Notification Consumers

Created 4 consumers in `Modules/Notification/src/Aizen.Modules.Notification/Consumers/Identity/`:

| Consumer | Message | Channels |
|----------|---------|----------|
| `ProviderProfileApprovedConsumer` | `ProviderProfileApprovedMessage` | InApp + Push |
| `ProviderProfileRejectedConsumer` | `ProviderProfileRejectedMessage` | InApp + Push |
| `ProviderProfileSuspendedConsumer` | `ProviderProfileSuspendedMessage` | InApp + Push |
| `ProviderOnboardingRevisionRequestedConsumer` | `ProviderOnboardingRevisionRequestedMessage` | InApp + Push |

Each consumer:
1. Follows the existing `AizenBaseMessageConsumer<T>` pattern
2. Sends `SendNotificationCommand` via MediatR for both InApp and Push channels
3. Includes metadata JSON with profileId and relevant context
4. Logs at information level on success, warning on rollback

### A5. Notification Templates

Added 8 templates to `NotificationTemplateSeed` (idempotent seed):

| Code | Type | Channel | Language |
|------|------|---------|----------|
| `PROFILE_APPROVED_INAPP` | ProfileApproved | InApp | TR |
| `PROFILE_APPROVED_PUSH` | ProfileApproved | Push | TR |
| `PROFILE_REJECTED_INAPP` | ProfileRejected | InApp | TR |
| `PROFILE_REJECTED_PUSH` | ProfileRejected | Push | TR |
| `PROFILE_SUSPENDED_INAPP` | ProfileSuspended | InApp | TR |
| `PROFILE_SUSPENDED_PUSH` | ProfileSuspended | Push | TR |
| `ONBOARDING_REVISION_INAPP` | OnboardingRevisionRequested | InApp | TR |
| `ONBOARDING_REVISION_PUSH` | OnboardingRevisionRequested | Push | TR |

Templates use Turkish text (target audience). Variables: `{{reason}}`, `{{steps}}`, `{{note}}`, `{{profileType}}`.

---

## Part B — Web Push Delivery

### B2. Schema Changes

**PushPlatform enum:** Added `WebPush = 3` (keeps `Fcm = 1`, `Apns = 2`).

**UserDeviceTokenEntity** (`Modules/Notification/src/Aizen.Modules.Notification.Domain/Entities/`):

New nullable fields:
- `Endpoint` (string?, max 2048) — Web Push subscription endpoint URL
- `P256dhKey` (string?, max 256) — P-256 Diffie-Hellman public key
- `AuthKey` (string?, max 256) — authentication secret

Factory methods:
- `CreateDeviceToken(userId, token, platform)` — for FCM/APNs (throws if platform is WebPush)
- `CreateWebPush(userId, endpoint, p256dhKey, authKey)` — for Web Push (sets both DeviceToken and Endpoint to the endpoint URL)
- `Create(userId, token, platform)` — backwards-compatible, routes to `CreateDeviceToken`

Parameterless constructor is `protected` (not `private`) per constraint.

**EF Configuration:**
- Endpoint: max 2048, nullable
- P256dhKey: max 256, nullable
- AuthKey: max 256, nullable
- Unique filtered index on Endpoint (`WHERE Endpoint IS NOT NULL`)

**Repository:** Added `UpsertWebPushAsync(userId, endpoint, p256dhKey, authKey)` and `DeactivateByEndpointAsync(endpoint)` to both interface and implementation.

**TODO:** EF migration not generated. Run `dotnet ef migrations add AddWebPushColumns -p Modules/Notification/src/Aizen.Modules.Notification.Repository -s Modules/Notification/src/Aizen.Modules.Notification` and inspect.

### B3. Sending

**IPushSender** — new interface in `Modules/Notification/src/Aizen.Modules.Notification.Domain/Interface/Service/`:
```csharp
Task<string> SendAsync(UserDeviceTokenEntity subscription, string title, string body, string? dataJson, CancellationToken ct);
```

**WebPushSender** — implementation using the `WebPush` NuGet package (v2.0.4):
- Uses VAPID authentication (`VapidOptions` from configuration)
- Sends JSON payload: `{ title, body, data }`
- On `404 Not Found` or `410 Gone`: deactivates the subscription (dead endpoint reaping)
- On other errors: exception propagates (transient — retry later)
- No PII in payload (title + body from template, data is metadata JSON with IDs only)

**VapidOptions** — configuration class (`Vapid` section):
- `Subject` (mailto: URI or HTTPS URL)
- `PublicKey` (VAPID public key — share with frontend)
- `PrivateKey` (VAPID private key — **must come from secret store, never appsettings.json**)

Key generation:
```bash
dotnet tool install --global dotnet-webpush
dotnet webpush generate-vapid-keys
# or: npx web-push generate-vapid-keys
```

**PushNotificationDispatcher** — updated to route by platform:
- `WebPush` → `IPushSender` (WebPushSender)
- `Fcm` → `IFcmSender` (existing stub)
- `Apns` → throws `NotImplementedException` (explicit, not silent)
- Unknown → throws `NotImplementedException`

**DI registration:** `IPushSender` → `WebPushSender` (scoped), `VapidOptions` bound from configuration.

### B4. BFF Endpoint

**ProviderNotificationsController** at `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Controllers/V1/`:

Route: `api/v1/provider/notifications`

| Method | Path | Body | Action |
|--------|------|------|--------|
| POST | `/push-subscriptions` | `{ endpoint, keys: { p256dh, auth } }` | Upsert subscription |
| DELETE | `/push-subscriptions` | `{ endpoint }` | Deactivate subscription |

Both handlers:
1. Require `[Authorize(Policy = ProviderAuthenticated)]`
2. Call `IProviderProfileResolver.ResolveAsync` first (fail-closed)
3. Reject if `UserId` is null or 0
4. Use typed string properties (no `JsonElement` — safe from the Newtonsoft/STJ serializer trap)

**Remote call:** `IProviderNotificationRemoteCall` (Refit) registered in `DependencyInjection.cs` with the standard `MarineProviderBffAuthDelegatingHandler`.

**Notification module endpoints:** Added `POST/DELETE push-subscriptions` to `NotificationsController.cs` with corresponding command handlers.

### B5. Frontend (SPA)

**Not implemented** — the frontend is in a separate repository (`inktavia-marine-provider-web`). The prompt specifies:

- Service worker (`public/sw.js`): handle `push` → `showNotification`, `notificationclick` → focus/open
- `usePushSubscription` hook: register SW, subscribe with VAPID public key, POST to BFF
- **Permission prompt**: never on page load; show soft prompt after onboarding submit; respect "Block"
- Handle `permission === 'denied'` honestly
- Keep SignalR in-app channel as-is (complementary)

### Scope limits

- HTTPS required (localhost exempt for dev)
- iOS/Safari: web push only on iOS 16.4+ with home screen PWA; desktop Safari 16+ works
- No mobile app for providers in this phase

---

## Files Changed / Created

### New Files

| File | Purpose |
|------|---------|
| `Identity.Abstraction/Message/ProviderProfileApprovedMessage.cs` | Message contract |
| `Identity.Abstraction/Message/ProviderProfileRejectedMessage.cs` | Message contract |
| `Identity.Abstraction/Message/ProviderProfileSuspendedMessage.cs` | Message contract |
| `Identity.Abstraction/Message/ProviderOnboardingRevisionRequestedMessage.cs` | Message contract |
| `Notification/Consumers/Identity/ProviderProfileApprovedConsumer.cs` | Consumer |
| `Notification/Consumers/Identity/ProviderProfileRejectedConsumer.cs` | Consumer |
| `Notification/Consumers/Identity/ProviderProfileSuspendedConsumer.cs` | Consumer |
| `Notification/Consumers/Identity/ProviderOnboardingRevisionRequestedConsumer.cs` | Consumer |
| `Notification.Domain/Interface/Service/IPushSender.cs` | Push sender interface |
| `Notification.Application/Services/WebPushSender.cs` | VAPID Web Push sender |
| `Notification.Application/Services/VapidOptions.cs` | Configuration class |
| `Notification.Application/Command/RegisterWebPushSubscription/` | Command + handler |
| `Notification.Application/Command/DeactivateWebPushSubscription/` | Command + handler |
| `MarineProvider.Application/Common/RemoteClients/IProviderNotificationRemoteCall.cs` | Refit client |
| `MarineProvider.Application/Notifications/SubscribePushCommand*.cs` | BFF commands |
| `MarineProvider.Application/Notifications/UnsubscribePushCommand*.cs` | BFF commands |
| `MarineProvider/Controllers/V1/ProviderNotificationsController.cs` | BFF controller |

### Modified Files

| File | Change |
|------|--------|
| `NotificationType.cs` | Added 4 enum values (401-404) |
| `PushPlatform.cs` | Added `WebPush = 3` |
| `UserDeviceTokenEntity.cs` | Added WebPush fields, factory methods, protected ctor |
| `UserDeviceTokenEntityConfiguration.cs` | Added Endpoint/P256dhKey/AuthKey columns + filtered unique index |
| `IUserDeviceTokenRepository.cs` | Added UpsertWebPushAsync, DeactivateByEndpointAsync |
| `UserDeviceTokenRepository.cs` | Implemented new methods |
| `PushNotificationDispatcher.cs` | Routes by platform (WebPush/FCM/APNs) |
| `DependencyInjection.cs` (Notification.Application) | Register IPushSender, VapidOptions |
| `DependencyInjection.cs` (MarineProvider.Application) | Register IProviderNotificationRemoteCall |
| `NotificationsController.cs` (Notification module) | Added push-subscriptions endpoints |
| `NotificationTemplateSeed.cs` | Added 8 lifecycle templates |
| `ApproveOrganizerProfileCommandHandler.cs` | Publish ProviderProfileApprovedMessage |
| `RejectOrganizerProfileCommandHandler.cs` | Publish ProviderProfileRejectedMessage |
| `SuspendOrganizerProfileCommandHandler.cs` | Publish ProviderProfileSuspendedMessage |
| `RequestProviderOnboardingRevisionCommandHandler.cs` | Publish ProviderOnboardingRevisionRequestedMessage |
| `ApproveVenueProfileCommandHandler.cs` | Publish ProviderProfileApprovedMessage |
| `RejectVenueProfileCommandHandler.cs` | Publish ProviderProfileRejectedMessage |
| `Aizen.Modules.Notification.Application.csproj` | Added WebPush NuGet package |

---

## Open Items

| Item | Status |
|------|--------|
| EF migration for WebPush columns (Endpoint, P256dhKey, AuthKey) | Not generated |
| EF migration for Identity module (ReviewStatus, ResolutionNote on VerificationDocuments) | Not generated |
| VAPID key pair generation and secret store configuration | Not done |
| RemoteCall configuration for IProviderNotificationRemoteCall (base URL) | Needs appsettings entry |
| Frontend service worker + usePushSubscription hook | Separate repo |
| Frontend permission flow (soft prompt, not on page load) | Separate repo |
| Browser verification of full notification flow | Not done |
| SuspendVenueProfile handler | Does not exist — out of scope |
