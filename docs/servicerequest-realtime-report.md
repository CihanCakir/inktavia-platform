# ServiceRequest: Realtime Bridge & Fixes — Report

**Date:** 2026-07-13
**Branch:** `feature/messaging-registration`

---

## 1. Realtime Infrastructure Study

### Core/Realtime Framework

The `AddAizenRealtime` extension registers:
- `SignalRRealtimePublisher` — wraps `IHubContext` for channel/group/user delivery
- `SignalRSocketManager` — low-level socket operations with dynamic hub resolution
- `RealtimeIngressService` — bridges domain events to realtime channels
- `DomainHubBase` — abstract base for domain hubs with group helper methods
- `RealtimeHubFilter` — rate limiting (120/min default) and message size guard (1000 chars)
- `InMemoryRateLimitGuard` — per-connection sliding-window rate limiter

### Redis Backplane

`SignalRSettings.UseRedisBackplane` (bool) + `RedisConnectionString` control scale-out. When enabled, `AddStackExchangeRedis()` is called on the SignalR builder.

**Current state: NOT configured.** No appsettings entry sets `UseRedisBackplane = true`. This means:
- **The BFF is single-instance today.** A connection on instance A will not receive events published by a consumer on instance B.
- This is a **known scale limit**, not a bug — the BFF is deployed as a single instance. When scaling horizontally, the Redis backplane must be enabled or events will be silently lost.

### ServiceRequestHub

Located at `/hubs/servicerequest` on the ServiceRequest module. Groups:
- `user:{userId}` — auto-joined on connect
- `servicerequest:{id}` — manual subscription
- `provider:{profileId}` — manual subscription
- `admin:operations` — requires Admin role
- `conv:{id}` — conversation messages

`Context.UserIdentifier` is populated via `ClaimUserIdProvider` mapping JWT `sub` → `ClaimTypes.NameIdentifier`.

### ServiceRequestRealtimePublisher

**CONFIRMED: pushes to THIS process's own SignalR hub only.** It calls `IRealtimePublisher.PublishToGroupAsync` and `PublishToUserAsync` — these resolve to `SignalRSocketManager.SendToGroupAsync` which uses `IHubContext<ServiceRequestHub>`. **Nothing leaves the process.** Nothing goes on the message bus.

### Bus Contracts — Were Never Published

**9 message contracts** existed in `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Abstraction/Message/`:

| Contract | Was Published Before This Phase? |
|----------|--------------------------------|
| `ServiceRequestCreatedMessage` | NO |
| `ServiceRequestPublishedMessage` | YES (added in working tree) |
| `ServiceRequestStatusChangedMessage` | NO |
| `ServiceRequestOfferCreatedMessage` | NO |
| `ServiceRequestOfferAcceptedMessage` | YES (added in working tree) |
| `ServiceRequestAssignmentCreatedMessage` | NO |
| `ServiceRequestCompletionSubmittedMessage` | NO |
| `ServiceRequestDisputeOpenedMessage` | NO |
| `ServiceRequestMessageSentMessage` | NO |

**Consequence:** Notification module's ServiceRequest consumers (if any exist) have been dead code — they had nothing to consume. The provider BFF consumers (`ServiceRequestPublishedRealtimeConsumer`, `OfferAcceptedRealtimeConsumer`) only work because the two in-tree changes added the publishing.

---

## 2. Build Fixes

### Realtime Consumers Missing ExecuteRollbackMessage

Both BFF consumers (`OfferAcceptedRealtimeConsumer`, `ServiceRequestPublishedRealtimeConsumer`) inherited `AizenBaseMessageConsumer<T>` but did not implement the required `ExecuteRollbackMessage` abstract method. Added standard logging rollback handlers to both.

### CreateServiceRequestOfferCommandHandler — EF Fix (Pre-existing, Verified)

The handler was already fixed in the working tree:
- **Old code:** `AddAsync(offer)` then `Update(offer)` on the same instance → EF threw "temporary value while attempting to change state to Modified" → **every offer submission returned 500**
- **Fix:** Build the aggregate fully (add items via `offer.AddItem()`), then `AddAsync` once. Item's `OfferId` is set by EF relationship fix-up on save.
- **Verified:** The fix is correct. Items are attached to the offer entity before persistence, and `Submit()` is called before `AddAsync`.

### Provider Identity on CreateOffer (Pre-existing, Verified)

`ProviderProfileId` now comes from `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId` instead of the request body. This was an auth hole: a caller could file an offer under another provider's profile.

---

## 3. Bus Bridge — Completed

All command handlers that raise realtime events now also publish typed bus messages:

| Handler | Bus Message | Status |
|---------|-------------|--------|
| `PublishServiceRequestCommandHandler` | `ServiceRequestPublishedMessage` | Already done (working tree) |
| `AcceptServiceRequestOfferCommandHandler` | `ServiceRequestOfferAcceptedMessage` | Already done (working tree) |
| `CreateServiceRequestOfferCommandHandler` | `ServiceRequestOfferCreatedMessage` | **Added** |
| `RejectServiceRequestOfferCommandHandler` | `ServiceRequestOfferRejectedMessage` | **Added** (new contract) |
| `CreateServiceRequestAssignmentCommandHandler` | `ServiceRequestAssignmentCreatedMessage` | **Added** |
| `SubmitServiceRequestCompletionCommandHandler` | `ServiceRequestCompletionSubmittedMessage` | **Added** |
| `ApproveServiceRequestCompletionCommandHandler` | `ServiceRequestCompletionApprovedMessage` | **Added** (new contract) |
| `RejectServiceRequestCompletionCommandHandler` | `ServiceRequestCompletionRejectedMessage` | **Added** (new contract) |
| `OpenServiceRequestDisputeCommandHandler` | `ServiceRequestDisputeOpenedMessage` | **Added** |

### New Message Contracts Created

- `ServiceRequestOfferRejectedMessage` — ServiceRequestId, OfferId, OwnerUserId, ProviderProfileId, Reason
- `ServiceRequestCompletionApprovedMessage` — ServiceRequestId, CompletionId, ProviderUserId, OwnerUserId
- `ServiceRequestCompletionRejectedMessage` — ServiceRequestId, CompletionId, ProviderUserId, OwnerUserId, ReviewNotes

### Not Published (Intentionally)

- `ServiceRequestCreatedMessage` — Draft creation is not a provider-relevant event. `ServiceRequestPublishedMessage` is the one that matters.
- `ServiceRequestStatusChangedMessage` — Generic status changes are covered by the specific event messages above. The `UpdateServiceRequestStatus` handler is admin-only and the status change it produces is already captured by the typed messages from the handlers that actually cause transitions.
- `ServiceRequestMessageSentMessage` — Messaging is Phase P4 (separate decision pending on Messaging vs ServiceRequest message controller).

---

## 4. Provider Hub — Security Properties Verified

### ProviderRealtimeHub (BFF)

`Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Realtime/ProviderRealtimeHub.cs`

**Group membership is server-decided:**
- `OnConnectedAsync` calls `IProviderProfileResolver.ResolveAsync()` — the same identity resolution used by every REST handler
- Joins `provider:{profileId}` from the resolved profile
- Joins `city:{cityCode}` from `resolution.Profile?.City` (the provider's operating city, from their Identity profile)
- **No "subscribe to group X" hub method exists** — the client cannot choose its groups

**Unresolved connections are aborted:**
- If `profileId <= 0` or `UserId` is not resolved → `Context.Abort()` is called
- The connection is dropped, not given a default group

**Fan-out is targeted:**
- `ServiceRequestPublished` → goes to `city:{locationCityCode}` group only (not broadcast)
- `OfferAccepted` → goes to `provider:{providerProfileId}` group only (a competitor never learns)

**Token in query string:**
- `AuthenticationExtensions.cs` handles `OnMessageReceived` to extract `access_token` from the query string
- This is done **only on `/hubs` paths** — on all other paths, a token in the URL would leak into logs/referrers

**CORS:**
- BFF `Program.cs` configures `AllowCredentials()` for the SignalR handshake
- Only configured origins are allowed (defaults to `http://localhost:3002`)

---

## 5. Redis Backplane Decision

**The BFF is single-instance today.** Redis backplane is NOT configured.

This is acceptable for the current deployment. When scaling to multiple BFF instances:
1. Set `SignalR:UseRedisBackplane = true` and `SignalR:RedisConnectionString` in configuration
2. The `AddAizenRealtime` extension already supports this — it calls `AddStackExchangeRedis()` conditionally
3. Without it, a bus consumer on instance A will push to A's hub, but connections on instance B will not receive the event

**This is stated explicitly, not left silently broken.**

---

## 6. Notification Module — ServiceRequest Consumers

The Notification module has consumers for:
- `ServiceRequestOfferCreatedMessage` (OfferCreated → notify owner)
- `ServiceRequestAssignmentCreatedMessage` (Assignment → notify provider)
- `ServiceRequestCompletionSubmittedMessage` (Completion → notify owner)
- `ServiceRequestDisputeOpenedMessage` (Dispute → notify both parties)

**These consumers were dead code** because nothing published the bus messages. They are now enabled by the bus bridge additions above. The existing template and channel routing in Notification should work as-is.

**Not verified at runtime** — requires `docker compose up` with RabbitMQ running. If any consumer fails due to missing templates, the seeding must be extended (separate task).

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Changed / Created

### New Files
| File | Purpose |
|------|---------|
| `Abstraction/Message/ServiceRequestOfferRejectedMessage.cs` | Bus contract |
| `Abstraction/Message/ServiceRequestCompletionApprovedMessage.cs` | Bus contract |
| `Abstraction/Message/ServiceRequestCompletionRejectedMessage.cs` | Bus contract |

### Modified Files
| File | Change |
|------|--------|
| `OfferAcceptedRealtimeConsumer.cs` | Added missing `ExecuteRollbackMessage` |
| `ServiceRequestPublishedRealtimeConsumer.cs` | Added missing `ExecuteRollbackMessage` |
| `CreateServiceRequestOfferCommandHandler.cs` | Added bus publishing (`ServiceRequestOfferCreatedMessage`) |
| `RejectServiceRequestOfferCommandHandler.cs` | Added bus publishing (`ServiceRequestOfferRejectedMessage`) |
| `CreateServiceRequestAssignmentCommandHandler.cs` | Added bus publishing (`ServiceRequestAssignmentCreatedMessage`) |
| `SubmitServiceRequestCompletionCommandHandler.cs` | Added bus publishing (`ServiceRequestCompletionSubmittedMessage`) |
| `ApproveServiceRequestCompletionCommandHandler.cs` | Added bus publishing (`ServiceRequestCompletionApprovedMessage`) |
| `RejectServiceRequestCompletionCommandHandler.cs` | Added bus publishing (`ServiceRequestCompletionRejectedMessage`) |
| `OpenServiceRequestDisputeCommandHandler.cs` | Added bus publishing (`ServiceRequestDisputeOpenedMessage`) |

### Pre-existing Changes (Verified, Not Modified)
| File | Status |
|------|--------|
| `CreateServiceRequestOfferCommandHandler.cs` | EF fix verified correct |
| `PublishServiceRequestCommandHandler.cs` | Bus publishing already present |
| `AcceptServiceRequestOfferCommandHandler.cs` | Bus publishing already present |
| `ProviderRealtimeHub.cs` | Security properties verified correct |
| `ProviderRealtimeEvent.cs` | Payload is appropriately thin |
| `AuthenticationExtensions.cs` | Token path restriction verified correct |
| `GetProviderServiceRequestDetailQuery/Handler` | Access control verified correct |
| `GetServiceRequestDetailBffQuery/Handler` | Identity resolution verified correct |

---

## Open Items

| Item | Status |
|------|--------|
| Redis backplane | Not configured — single instance today, documented |
| `ServiceRequestStatusChangedMessage` publishing | Not added — covered by typed event messages |
| `ServiceRequestMessageSentMessage` publishing | Deferred to Phase P4 (messaging decision pending) |
| Runtime verification with RabbitMQ | Not done — requires docker compose |
| Notification consumer template seeding for SR events | May need new templates if missing |
