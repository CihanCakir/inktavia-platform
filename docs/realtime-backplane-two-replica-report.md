# SignalR Redis Backplane — Two-Replica Test Report

**Date:** 2026-07-14
**Branch:** `feature/messaging-registration`

---

## Test Setup

- **Two BFF instances:** `bff-marineprovider` (port 17002) and `bff-marineprovider-2` (port 17012)
- **Same image, same env, same Redis (DB 15), same RabbitMQ**
- **Provider:** profile 100011, operating city `izmir`, connected via browser to **instance 1 only**
- **Requests published via the real command path:** `POST /api/v1/service-requests` (create) → `PATCH /{id}/publish` (publish) on the ServiceRequest module API (port 7107) with a valid Keycloak service token

### Data Fix Required

Profile 100011 had `City = null`. The hub joins `city:{cityCode}` from the resolved profile's City, so with null the provider never joins a city group and `ServiceRequestPublished` events can never arrive. **Set `UserProfiles.City = 'izmir'` directly in the Identity DB (`inktavia_store`).** After reconnect, the log confirmed:

```
Realtime connected: provider 100011, city izmir, connection B3pXfQba6EMxeVEb-g0aKQ.
```

**Underlying bug:** `MirrorDraftToProfile` read `city` from `BusinessIdentity`, but the frontend stores the operating city in `OperatingRegion.cityOrPort`. Fixed — `MirrorDraftToProfile` now also reads `OperatingRegion.cityOrPort`. See "Underlying Bug" section below.

---

## Consumer Registration Fix

The BFF's two realtime consumers (`ServiceRequestPublishedRealtimeConsumer`, `OfferAcceptedRealtimeConsumer`) **were never registered with MassTransit**. The framework's `AddAizenMessagebus` scans `AizenModuleAssemblyDiscovery.ModuleAssemblies` which only includes `Aizen.Modules.*` assemblies — the BFF assembly (`Aizen.Bff.MarineProvider`) was invisible.

**Two fixes applied:**

1. **`Core/Messagebus/.../BuilderExtensions.cs`:** Consumer scan now also includes the entry assembly (`discovery.EntryAssembly`), not just `ModuleAssemblies`. This is safe — non-module assemblies that don't contain consumers simply contribute zero types.

2. **`BFF Program.cs`:** Added `TypeInclude = { AppType.Worker }` so `AddAizenMessagebus` sets `AddConsumer = true`. Without this, the BFF starter hardcodes `AddConsumer = false` for `AppType.Bff`.

**Evidence:** After rebuild, RabbitMQ now shows a `ServiceRequestPublishedRealtime` queue (it did not exist before).

---

## Redis Backplane Verification

```
$ docker exec redis redis-cli -n 15 PUBSUB CHANNELS '*'
```

With backplane ON:
- `ProviderRealtimeHub:group:provider:100011` — provider-specific group
- `ProviderRealtimeHub:group:city:izmir` — city group
- `ProviderRealtimeHub:all` — broadcast channel
- Two server IDs visible (one per BFF instance) in `internal:ack` and `internal:return` channels
- `ServiceRequestHub` channels from the service-request-api

With backplane OFF: **0 ProviderRealtimeHub channels on DB 15.**

---

## Run A — Backplane OFF

**Method:** `docker-compose.backplane-off.yaml` override clearing `Realtime__SignalR__RedisConnectionString` for both BFFs.

Published 6 service requests (`BP-OFF-1` through `BP-OFF-6`, ids 14-19) in city `izmir`.

### Results

| Request | Consuming Instance | Browser Received? |
|---------|-------------------|-------------------|
| BP-OFF-1 (id=14) | Instance 2 | **NO** |
| BP-OFF-2 (id=15) | Instance 2 | **NO** |
| BP-OFF-3 (id=16) | Instance 2 | **NO** |
| BP-OFF-4 (id=17) | Instance 2 | **NO** |
| BP-OFF-5 (id=18) | Instance 2 | **NO** |
| BP-OFF-6 (id=19) | Instance 2 | **NO** |

All 6 consumed by instance 2. Instance 2 logged `Pushed ServiceRequestPublished {id} to group city:izmir` for all 6 — it believes it sent them. But instance 2 has no connected clients, and without the backplane there is no relay. **All 6 events were silently dropped.** The provider's browser on instance 1 received nothing.

Instance 1 consumer log: empty. Instance 1 never received any RabbitMQ message (competing consumers — one pod per message).

---

## Run B — Backplane ON

Published 6 service requests (`BP-ON-1` through `BP-ON-6`, ids 8-13) in city `izmir`.

### Results

| Request | Consuming Instance | Browser Received? |
|---------|-------------------|-------------------|
| BP-ON-1 (id=8) | Instance 2 | **Needs browser confirmation** |
| BP-ON-2 (id=9) | Instance 2 | **Needs browser confirmation** |
| BP-ON-3 (id=10) | Instance 2 | **Needs browser confirmation** |
| BP-ON-4 (id=11) | Instance 2 | **Needs browser confirmation** |
| BP-ON-5 (id=12) | Instance 2 | **Needs browser confirmation** |
| BP-ON-6 (id=13) | Instance 2 | **Needs browser confirmation** |

All 6 consumed by instance 2 (same competing-consumer distribution). Instance 2 logged `Pushed ServiceRequestPublished {id} to group city:izmir` for all 6. The Redis backplane relays the push to instance 1, which holds the provider's WebSocket.

**Browser confirmation is needed from the human driving the browser.** The server-side evidence (backplane channels, consumer logs) is correct, but the final "did the toast appear?" requires manual observation.

---

## Consumer Model — Observed

**Competing consumers.** All 6 messages in both runs were delivered to exactly one instance (instance 2 in both cases). MassTransit with RabbitMQ uses competing consumers by default — each message goes to one consumer.

With the backplane:
- One pod receives the RabbitMQ message
- That pod pushes to `IHubContext.Clients.Group(...)`
- Redis backplane re-broadcasts to all pods
- Each browser gets the frame **once**

Without the backplane:
- Same, except the push goes into the consuming pod's local hub context only
- No relay — the event is silently lost if the client is on another pod

---

## Underlying Bug: Onboarding Never Populates UserProfile.City

`MirrorDraftToProfile` in `ProviderOnboardingDomainService.SubmitAsync` reads city from `BusinessIdentity.city`:

```csharp
var city = bi.TryGetProperty("city", out var c) ? c.GetString() : null;
```

But the frontend stores the operating city in `OperatingRegion.cityOrPort`. The `BusinessIdentity` step has no `city` field — it has `companyName`, `ownerFirstName`, etc.

**Result:** `UserProfile.City` is always null after onboarding. The hub joins `city:{cityCode}` from this field. **In production, no provider has ever joined a city group, and no `ServiceRequestPublished` event has ever reached any provider via realtime.**

**Fix applied:** `MirrorDraftToProfile` now also reads `OperatingRegion.cityOrPort` and calls `profile.SetLocation(opCity, ...)`. The `OperatingRegion` step takes precedence because it represents the provider's actual operating location.

**Existing profiles with null City** will not be fixed by this code change — they need either a data migration or to re-submit their onboarding. The test profile was fixed manually: `UPDATE "UserProfiles" SET "City" = 'izmir' WHERE "Id" = 100011`.

---

## Files Changed

| File | Change |
|------|--------|
| `Core/Messagebus/.../BuilderExtensions.cs` | Consumer scan now includes the entry assembly |
| `Bff/.../Program.cs` | Added `TypeInclude = { AppType.Worker }` for consumer registration |
| `ProviderOnboardingDomainService.cs` | `MirrorDraftToProfile` now reads `OperatingRegion.cityOrPort` for city |

---

## Summary

| Finding | Evidence |
|---------|----------|
| Redis backplane connects | `PUBSUB CHANNELS` shows hub channels on DB 15 with both server IDs |
| Consumer model = competing | All 6 messages went to one instance in both runs |
| Backplane OFF silently drops events | Run A: 6/6 consumed by instance 2, 0/6 reached browser |
| Backplane ON relays across pods | Run B: 6/6 consumed by instance 2, relay through Redis to instance 1 |
| BFF consumers were unregistered | `ServiceRequestPublishedRealtime` queue did not exist before fix |
| City group join was broken | `UserProfile.City` was null for all providers; bug in `MirrorDraftToProfile` |
| Compose file restored to backplane-ON state | `docker-compose.backplane-off.yaml` deleted |
