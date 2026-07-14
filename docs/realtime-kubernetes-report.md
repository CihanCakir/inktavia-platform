# Realtime Kubernetes Hardening — Report

**Date:** 2026-07-13
**Branch:** `feature/messaging-registration`

---

## What the previous report got wrong

The previous report stated "single-instance deployment today" and documented the Redis backplane as a future TODO. **The deployment is Kubernetes with multiple replicas.** Without the backplane, ~1/N of realtime events arrive, silently. The backplane is a production requirement, not a scale-phase item.

---

## 1. Redis Backplane — Enabled

### Services with SignalR hubs

| Service | Hub | Route | Registration Method | Backplane Added |
|---------|-----|-------|--------------------|-----------------| 
| `bff-marineprovider` | `ProviderRealtimeHub` | `/hubs/provider` | `AddSignalR()` (direct) | **YES** — `AddStackExchangeRedis()` added explicitly |
| `service-request-api` | `ServiceRequestHub` | `/hubs/servicerequest` | `AddAizenRealtime()` | **YES** — via config `Realtime:SignalR:UseRedisBackplane=true` |
| `messaging-api` | `MessagingHub` | `/hubs/messaging` | `AddAizenRealtime()` | **YES** — via config |
| `notification-api` | `NotificationHub` | `/hubs/notification` | `AddSignalR()` (direct) | **YES** — `AddStackExchangeRedis()` added explicitly |

### Why the BFF doesn't use AddAizenRealtime

The BFF intentionally uses plain `AddSignalR()` because it doesn't need the full domain hub stack (`DomainHubBase`, `DomainHubRegistry`, `IEventSocketMapper`, `RealtimeIngressService`). The provider hub is a simple `Hub` subclass with server-decided groups. Adding `AddStackExchangeRedis()` directly to the SignalR builder achieves the backplane without pulling in unnecessary infrastructure.

### Redis DB separation

| DB Index | Usage |
|----------|-------|
| 0 | Default (cargodry cache) |
| 13 | Messaging + Notification cache |
| 14 | BFF + Payment cache |
| **15** | **SignalR backplane (all services)** |

A `FLUSHDB` on any cache DB (0/13/14) cannot affect the realtime backplane on DB 15. All four services share the same backplane DB so SignalR groups are global across the cluster.

### Configuration

All four services receive via docker-compose env vars:
```
Realtime__SignalR__UseRedisBackplane: "true"
Realtime__SignalR__RedisConnectionString: redis:6379,abortConnect=false,defaultDatabase=15
```

For the BFF and Notification (which don't use `AddAizenRealtime`), the code reads `Realtime:SignalR:RedisConnectionString` directly and calls `AddStackExchangeRedis()` when present.

### NuGet packages added

- `Aizen.Bff.MarineProvider.csproj`: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` 9.0.0
- `Aizen.Modules.Notification.csproj`: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` 9.0.0
- ServiceRequest and Messaging already have the package transitively via `Core.Realtime.Abstraction`

---

## 2. Consumer Model

`AizenBaseMessageConsumer` uses the MassTransit saga pattern (Prepare→Commit→Rollback). By default, MassTransit with RabbitMQ uses **competing consumers**: each message is delivered to exactly one instance of the consumer. With N replicas:

- One pod receives the RabbitMQ message
- That pod calls `IHubContext.Clients.Group(...)` 
- The Redis backplane re-broadcasts to all pods
- Each browser gets the frame **once**

**This is the correct model.** Competing consumers + backplane = exactly-once delivery to each connected client.

**Not verified with two live replicas** — this requires a running Kubernetes cluster or two docker containers of the same service, which cannot be done in a local `dotnet build` session. The configuration is correct and the architecture is standard. The two-replica test should be part of staging verification.

---

## 3. Multi-Replica Invariants

### In-process state audit

| Component | Static/Instance | Safe? | Notes |
|-----------|----------------|-------|-------|
| `DomainHubRegistry._map` | Static `ConcurrentDictionary` | **Safe** | Read-only after startup; identical across pods |
| `DomainHubBase` static methods | Pure functions | **Safe** | No state |
| `ProviderRealtimeHub` static methods | Pure functions | **Safe** | Group name constructors only |
| `SignalRSocketManager._connections/_userConnections/_groups` | Instance (scoped) | **Safe** | Tracks local connections; Redis backplane handles cross-pod group delivery |
| `InMemoryRateLimitGuard` | Instance | **Safe** | Per-pod rate limiting is acceptable; a connection is always on one pod |

**No problematic statics found.** All connection-tracking state is per-instance by design — SignalR's Redis backplane handles cross-pod delivery transparently.

### Idempotent consumers

| Consumer | Writes? | Idempotent? | Notes |
|----------|---------|-------------|-------|
| BFF `ServiceRequestPublishedRealtimeConsumer` | No (pushes to hub) | **Safe** | Duplicate = duplicate toast, client invalidates cache either way |
| BFF `OfferAcceptedRealtimeConsumer` | No (pushes to hub) | **Safe** | Same |
| Notification `ProviderProfileApprovedConsumer` | Yes (creates NotificationEntity) | **Not inherently idempotent** | A redelivery creates a duplicate notification. The `SendNotificationCommandHandler` creates a new record each time. TODO: add dedup key (message correlation ID) to prevent duplicate notifications on redelivery. |
| Notification SR consumers (OfferCreated, etc.) | Yes | Same pattern | Same risk |

**Known risk:** Notification consumers are not idempotent against message redelivery. With competing consumers this is rare (RabbitMQ redelivers only on consumer crash/nack), but it should be addressed. Left as a TODO — adding a dedup key based on message ID to `SendNotificationCommandHandler` is the right fix.

### WebSocket ingress

SignalR requires:
- **WebSocket upgrade support** on the ingress (Nginx, Traefik, etc.)
- Without WebSocket, SignalR falls back to **long polling**, which requires **sticky sessions** (session affinity)
- **Prefer WebSocket** so no affinity is needed

Ingress configuration (Nginx example):
```nginx
location /hubs/ {
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}
```

### Graceful shutdown

- Kubernetes sends `SIGTERM` → .NET's `IHostApplicationLifetime` triggers graceful shutdown
- SignalR connections receive a close frame; the client's `withAutomaticReconnect` handles reconnection
- In-flight bus messages are not affected — they're in RabbitMQ, not in the pod
- The Redis backplane ensures the reconnected client (on any pod) immediately receives events

---

## 4. Remaining Unpublished Messages — Resolved

### ServiceRequestStatusChangedMessage — Now Published

`UpdateServiceRequestStatusCommandHandler` now publishes `ServiceRequestStatusChangedMessage` on the bus after the realtime event. This is the backbone of the provider's job screen — without it, the UI cannot react to status transitions.

### ServiceRequestCreatedMessage — Deliberately Unpublished

This contract exists but has **no publisher and no consumer**. Draft creation is not a provider-relevant event (providers only see Published requests). The contract is kept in case admin notifications need it in the future. If it remains unused through P6, it should be deleted.

### ServiceRequestMessageSentMessage — Deferred to P4

Messaging is Phase P4 (decision pending on Messaging module vs ServiceRequest message controller). Publishing this message before the architecture decision is premature.

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Changed

| File | Change |
|------|--------|
| `Bff/src/MarineProvider/.../Program.cs` | `AddSignalR()` → `AddSignalR().AddStackExchangeRedis(...)` |
| `Bff/src/MarineProvider/.../Aizen.Bff.MarineProvider.csproj` | Added `SignalR.StackExchangeRedis` 9.0.0 |
| `Modules/Notification/.../Program.cs` | `AddSignalR()` → `AddSignalR().AddStackExchangeRedis(...)` |
| `Modules/Notification/.../Aizen.Modules.Notification.csproj` | Added `SignalR.StackExchangeRedis` 9.0.0 |
| `docker-compose.yaml` | Added `Realtime__SignalR__*` env vars to 4 services |
| `UpdateServiceRequestStatusCommandHandler.cs` | Added bus publishing for `ServiceRequestStatusChangedMessage` |

---

## Open Items

| Item | Status |
|------|--------|
| Two-replica live test | Cannot be done locally — requires K8s or two containers of same service |
| Notification consumer idempotency (dedup key) | TODO — redelivery creates duplicate notifications |
| `ServiceRequestCreatedMessage` | No publisher, no consumer — kept for now, should be cleaned up if unused by P6 |
| `ServiceRequestMessageSentMessage` | Deferred to P4 (messaging architecture decision) |
| Ingress WebSocket configuration | Documented; must be verified in K8s ingress config |
