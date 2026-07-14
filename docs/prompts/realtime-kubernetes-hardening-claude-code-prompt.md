# Claude Code Prompt — Realtime hardening for Kubernetes (multi-replica)

**Follow-up to `docs/servicerequest-realtime-report.md`. Do NOT redo the work in that report** — the bus bridge and
the BFF hub are done and verified (9 message contracts now have publishers; they had none). This prompt closes the
one thing that report got wrong.

## What the previous report got wrong

> *"Redis backplane: Not configured. Single-instance deployment today. Documented as a known scale limit."*

That is not the deployment. **Every service — modules and BFFs — runs on Kubernetes and is scaled to many replicas
under load.** PostgreSQL, RabbitMQ, Redis, MinIO/S3 and Keycloak run **outside** the cluster (managed/external).

So the backplane is not a scale-phase TODO. Without it, on N replicas:

1. the provider's WebSocket is held by **pod A**;
2. the RabbitMQ message is delivered to **pod B** (competing consumers — one pod gets each message);
3. pod B calls `IHubContext.Clients.Group(...)`, which only knows **pod B's** connections;
4. the event is **silently dropped**. No error, no retry, no log line. The provider simply never hears.

Roughly 1/N of events arrive. It passes every single-pod test and then loses most events in production,
intermittently, with nothing to grep for. **That is worse than having no realtime at all**, because people start
relying on it.

## Current state (verified in the code, not assumed)

- `Core/Realtime/Extensions/ServiceCollectionExtensions.cs` **supports** the backplane:
  `if (signalrSettings.UseRedisBackplane && !string.IsNullOrWhiteSpace(RedisConnectionString))
   → signalRBuilder.AddStackExchangeRedis(...)`.
- `UseRedisBackplane` defaults to **false** and **nothing sets it anywhere** — not in appsettings, not in
  docker-compose, not in any chart.
- `Aizen.Bff.MarineProvider/Program.cs` calls plain **`builder.Services.AddSignalR()`** — it does not go through
  `AddAizenRealtime` at all, so it cannot even reach the backplane code path.
- `Aizen.Modules.ServiceRequest/Program.cs` calls `AddAizenRealtime(...)` but with the default settings, so
  `ServiceRequestHub` has no backplane either.

**Both hubs are affected.** Fix both.

---

## 1. Turn the backplane on — everywhere a hub is mapped

- Route the BFF's SignalR registration through the same path the modules use (`AddAizenRealtime`), or, if the BFF
  genuinely should not take the whole realtime stack, add `AddStackExchangeRedis(...)` explicitly to its
  `AddSignalR()` builder. **Say which you chose and why.**
- Configure `UseRedisBackplane = true` and the Redis connection for **every service that maps a hub** — today that
  is `bff-marineprovider` and `service-request-api`; check for others (`Notification` has a hub, `Messaging` has a
  hub — do they map them? If they do, they need it too).
- Wire the settings through configuration/env the way the rest of the stack does (`DistributedCache__Configuration`
  is the existing precedent for the Redis connection string). Use a **separate Redis database index** from the cache
  so a `FLUSHDB` on the cache cannot take the realtime plane down with it.
- Add the env vars to `docker-compose.yaml` for local dev so the local setup matches production topology. Redis is
  already there as an external service.

## 2. Establish the consumer model — measure it, do not guess

With `AizenBaseMessageConsumer` and N replicas, is each message delivered to **exactly one** replica (competing
consumers) or to **every** replica (fan-out)?

- Competing consumers + backplane → correct: one pod receives, the backplane re-broadcasts to all pods, each
  browser gets the frame **once**.
- Fan-out to every replica + backplane → **every provider receives every event N times**. Duplicate toasts,
  duplicate refetches, and — for any consumer that writes — duplicate records.

**Run two replicas and observe.** Report which model it is, with the evidence. If it is fan-out, make the realtime
consumers deduplicate (or bind them to a shared queue), and check whether Notification's consumers are affected the
same way — they mutate state and would be sending duplicate notifications.

## 3. The other multi-replica invariants

- **No in-process state.** No connection maps, no static "who is online" dictionaries, no per-pod caches of user
  state. Grep for statics in the realtime path and report what you find.
- **Idempotent consumers.** At-least-once delivery is the norm; a redelivered message must not create a second
  domain record. A duplicate realtime frame is harmless; a duplicate notification, assignment or payment is not.
  Audit the consumers that mutate state (Notification's, and any new ones) and say which are safe.
- **Ingress / WebSockets.** SignalR falls back to long polling when the WebSocket upgrade is unavailable, and long
  polling requires sticky sessions. State what the ingress must allow, and prefer WebSockets so affinity is not
  needed.
- **Graceful shutdown.** Rolling deploys kill pods. Connections must drain; the client already has
  `withAutomaticReconnect`. Make sure a terminating pod does not swallow in-flight messages.

## 4. While you are here — three events still have no publisher

The previous phase left `ServiceRequestCreatedMessage`, `ServiceRequestStatusChangedMessage` and
`ServiceRequestMessageSentMessage` unpublished ("deferred"). Decide deliberately:

- `StatusChanged` is the backbone of every provider's job screen — without it the UI cannot react to the request
  moving through its lifecycle. Publish it, or explain what replaces it.
- `Created` vs `Published`: providers must only ever hear about **published** requests (a Draft is not offered to
  anyone). If `Created` has no consumer, say so and consider deleting the contract rather than leaving a dead one —
  this codebase is full of contracts nobody publishes and consumers nobody feeds.

---

## Acceptance — two replicas, or it does not count

Write `docs/realtime-kubernetes-report.md`.

1. **Two replicas of the BFF.** A provider connects (their socket lands on replica 1). Publish a service request so
   that **replica 2** consumes the RabbitMQ message. The frame must arrive at the browser. Paste the evidence:
   which replica held the connection, which consumed the message, and the frame the client received.
   **Without the backplane this test fails — that is the whole point.** Run it once with the backplane off to show
   the failure, then on to show the fix.
2. Same test for `ServiceRequestHub` (module side), or state clearly that no browser connects to it today and why
   it still needs the backplane (or does not).
3. The consumer model is reported with evidence, and any duplicate delivery is handled.
4. A restart of the pod holding the socket → the client reconnects and keeps receiving events.
5. No statics / in-process state in the realtime path.
6. Redis DB index for the backplane is separate from the cache.

**Report failures plainly.** If the two-replica test cannot be run locally, say so explicitly and do not claim the
backplane works — a config flag that has never delivered a message across two processes is not verified.

## Constraints

- Kubernetes, many replicas. Postgres/RabbitMQ/Redis/MinIO/Keycloak are external to the cluster.
- No in-process user state; no silent no-ops; consumers that write must be idempotent.
- Group membership stays server-decided; do not add a client-callable "subscribe" method to any hub.
- If something cannot be finished, leave the TODO **and say so in the summary**.
