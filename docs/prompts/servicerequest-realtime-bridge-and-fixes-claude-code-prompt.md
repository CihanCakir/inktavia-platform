# Claude Code Prompt — ServiceRequest: realtime bridge to the provider BFF, plus the fixes that are sitting uncompiled

Read `docs/provider-service-request-roadmap.md` first. This prompt implements **P0 fixes + P3 (realtime)**. Do not
start P1/P2 here.

---

## ⚠️ Read this before touching anything

**There are changes in the working tree that have never been compiled.** They were written without a build
available. Your first job is to make them build, verify they are correct, and fix them — not to rewrite them from
scratch and not to assume they work.

Uncompiled changes, by file:

**ServiceRequest module**
- `Application/Command/Offer/CreateServiceRequestOffer/CreateServiceRequestOfferCommandHandler.cs`
  - `ProviderProfileId` now comes from `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`
    instead of the request body (it was an authorisation hole: a caller could file an offer under another
    provider's profile).
  - The aggregate is now built fully and added **once**. The old code did `AddAsync(offer)` and then `Update(offer)`
    on the same instance, which threw
    `The property 'ServiceRequestOfferEntity.Id' has a temporary value while attempting to change the entity's state to 'Modified'.`
    **Every offer submission returned 500.** Verify the fix actually persists the offer *and its items*.
- `Application/Query/Provider/GetProviderServiceRequestDetail/…Query.cs` + `…QueryHandler.cs` — NEW. Provider-scoped
  detail with an access check (biddable | has an offer | assigned to them). A refusal returns the same "not found"
  as a missing request, on purpose.
- `Controller/V1/Jobs/ProviderJobsController.cs` — new `GET provider/service-requests/{id}` endpoint.
- `Abstraction/Message/ServiceRequestPublishedMessage.cs` — NEW bus contract.
- `Abstraction/Message/ServiceRequestCreatedMessage.cs` — added `Title`, `LocationCityCode`, `LocationCountryCode`,
  `LocationMarinaName`.
- `Application/Command/ServiceRequest/PublishServiceRequest/PublishServiceRequestCommandHandler.cs` — now publishes
  `ServiceRequestPublishedMessage` on the bus.
- `Application/Command/Offer/AcceptServiceRequestOffer/AcceptServiceRequestOfferCommandHandler.cs` — now publishes
  `ServiceRequestOfferAcceptedMessage` on the bus.

**MarineProvider BFF**
- `Realtime/ProviderRealtimeHub.cs`, `Realtime/ProviderRealtimeEvent.cs`,
  `Realtime/ServiceRequestPublishedRealtimeConsumer.cs`, `Realtime/OfferAcceptedRealtimeConsumer.cs` — NEW.
- `Program.cs` — `AddSignalR()`, `AllowCredentials()` on CORS, `MapHub<ProviderRealtimeHub>("/hubs/provider")`.
- `Extensions/AuthenticationExtensions.cs` — `OnMessageReceived` reads `?access_token=` **only** on `/hubs` paths.
- `Common/RemoteClients/IProviderServiceRequestRemoteCall.cs` — detail call repointed at the new provider endpoint
  (it used to call the customer's unprotected `/api/v1/service-requests/{id}`).
- `ServiceRequests/GetServiceRequestDetailBffQuery.cs` + handler, `Controllers/V1/ProviderServiceRequestsController.cs`
  — NEW detail endpoint.

Some of this will not compile. Fix it. If a design decision in it is wrong, say so and change it — but say so.

---

## 1. Study the existing realtime infrastructure FIRST

The ServiceRequest module already uses the framework's realtime stack:

```csharp
builder.Services.AddAizenRealtime(builder.Configuration);   // Program.cs:42
app.MapHub<ServiceRequestHub>("/hubs/servicerequest");      // Program.cs:51
```

Before writing anything, read and report on:

- `Core/Realtime` — `AddAizenRealtime`, `SignalRRealtimePublisher`, `RealtimeIngressService`, `DomainHubBase`,
  `IEventSocketMapper`, the guards/filters, and the **Redis backplane** settings (`SignalRSettings.UseRedisBackplane`
  — nothing configures it today; say what that means for multi-instance deployments).
- `ServiceRequestHub` — its groups (`user:{id}`, `servicerequest:{id}`, `provider:{profileId}`, `admin:operations`)
  and how `Context.UserIdentifier` is populated.
- `ServiceRequestRealtimePublisher` — what it does and, crucially, what it does **not**.

**The finding that drives this whole phase:** `SignalRRealtimePublisher` pushes to **this process's own hub only**.
Nothing leaves the module. And the module's eight bus contracts
(`ServiceRequestCreated/OfferCreated/OfferAccepted/AssignmentCreated/CompletionSubmitted/StatusChanged/
MessageSent/DisputeOpened`) were **never published by anyone** — which means Notification's ServiceRequest consumers
have been dead code since they were written. No provider has ever been notified of anything.

Confirm or refute this in the report, with the code. If you find a publisher I missed, say so.

## 2. Complete the bus bridge

Every realtime event the module raises that another module needs must also go on the bus. Do **not** invent a
generic "realtime envelope" message — use the typed contracts that already exist, and add one only where a contract
is missing (`ServiceRequestPublishedMessage` was missing; that is why it is new).

At minimum, publish on the bus at these points (some are already done in the tree — verify, do not duplicate):

| Command | Bus message |
|---|---|
| PublishServiceRequest | `ServiceRequestPublishedMessage` ✅ (in tree) |
| CreateServiceRequestOffer | `ServiceRequestOfferCreatedMessage` (owner needs to know) |
| AcceptServiceRequestOffer | `ServiceRequestOfferAcceptedMessage` ✅ (in tree) |
| RejectServiceRequestOffer | needs a contract — add `ServiceRequestOfferRejectedMessage` |
| CreateAssignment | `ServiceRequestAssignmentCreatedMessage` |
| SubmitCompletion | `ServiceRequestCompletionSubmittedMessage` |
| Approve/RejectCompletion | needs contracts — add them |
| OpenDispute | `ServiceRequestDisputeOpenedMessage` |
| Status change | `ServiceRequestStatusChangedMessage` |

**Publish after the state change is committed**, never before. A notification for a transaction that then rolls back
is worse than none.

Keep the in-process realtime push as-is; it serves the module's own hub (the owner's web app). The bus is how the
event reaches *other* processes.

## 3. The provider hub lives on the BFF — verify the security properties

The decision is made: the hub is on `Aizen.Bff.MarineProvider`, not exposed from the module. Reason: the browser
only ever authenticates against the BFF; modules are reached with the BFF's service token + identity assertion.
Exposing a module hub to the browser would make the `sub → profileId` mapping a security boundary, and getting it
wrong once means a provider joins another provider's group and watches their bids.

Verify, and fix if the tree gets it wrong:

- **Group membership is decided by the server.** There is no "subscribe to group X" hub method, and there must not
  be one. On connect, the BFF resolves the provider profile (`IProviderProfileResolver`) and joins
  `provider:{profileId}` and `city:{cityCode}` itself. An unresolved connection is **aborted**, not given a default
  group.
- **Fan-out is targeted.** `ServiceRequestPublished` goes to the **city group**, never to all connected providers
  with client-side filtering — that would hand every provider the national demand feed. `OfferAccepted` goes to the
  single provider group; a competitor must never learn that a bid was accepted.
- **The token in the query string is accepted only on `/hubs`.** A browser WebSocket cannot send an Authorization
  header, so SignalR passes `?access_token=`. On any other path a token in the URL leaks into logs, referrers and
  history.
- **CORS** must allow credentials for the SignalR handshake, and only for the configured origins.
- **Redis backplane is MANDATORY.** See §3a — this is not a "nice to have" and not a scale-phase TODO.

## 3a. Kubernetes: multiple replicas. This is the deployment target, not a future concern.

**Every service (modules and BFFs) runs on Kubernetes and will be scaled to many replicas under load.** The
stateful infrastructure — PostgreSQL, RabbitMQ, Redis, MinIO/S3, Keycloak — lives **outside** the cluster
(managed/external). Design for that, and say in the report where the design depends on it.

This makes several things non-negotiable:

**Redis backplane is REQUIRED for every SignalR hub — the BFF hub and `ServiceRequestHub` alike.**
`SignalRSettings.UseRedisBackplane` exists and nothing sets it. Without it, with N replicas:

- a provider's WebSocket is held by **pod A**;
- the RabbitMQ message is delivered to **pod B** (competing consumers — only one pod gets each message);
- pod B calls `IHubContext.Clients.Group(...)`, which only knows about **pod B's** connections;
- the event is silently dropped. Not an error, not a retry — nothing. The provider simply never hears.

With ~1/N delivery probability, this is worse than having no realtime: it works in testing with one pod, then
loses most events in production, intermittently, with no error anywhere. Configure the backplane against the
external Redis, verify it, and show the verification (two replicas, connection on one, event published from the
other, frame arrives).

**Never hold user state in process memory.** No in-memory connection maps, no static dictionaries of "which
provider is online". Group membership must live in the backplane. If you need presence, derive it from the
backplane — do not invent a local cache that is wrong on every other pod.

**Consumer semantics.** Confirm what the framework does with `AizenBaseMessageConsumer` in a multi-replica
deployment: is each message delivered to exactly one replica (competing consumers, correct for commands) or to
every replica (fan-out)? A realtime *fan-out to browsers* only works correctly under the first model **because**
the backplane re-broadcasts. If the framework fans a message out to every replica instead, every provider gets
each event N times. **Test with two replicas and say which model it is.** Do not guess.

**WebSocket routing.** SignalR falls back to long polling when WebSockets are unavailable, and long polling
requires sticky sessions at the ingress. Either ensure the ingress allows WebSocket upgrade (preferred, and then
sticky sessions are unnecessary) or enable session affinity. State which, and what the ingress needs.

**Graceful shutdown.** On a rolling deploy pods are killed; connections must drain and the client must reconnect
(`withAutomaticReconnect` handles the client side). Make sure a terminating pod does not black-hole in-flight
messages.

**Idempotency.** With retries and at-least-once delivery across replicas, a consumer can see the same message
twice. Pushing a duplicate realtime frame is harmless; **writing a duplicate domain record is not.** Any consumer
that mutates state must be idempotent.

## 4. Payload discipline

`ProviderRealtimeEvent` is deliberately thin: event type, ids, and enough text for a toast. **A realtime frame is a
hint, not a record.** The client invalidates its cache and refetches from the API. Never push domain state, prices,
customer details, or anything a provider is not otherwise allowed to read down the socket.

---

## Acceptance

Write `docs/servicerequest-realtime-report.md`. Paste real evidence.

1. Everything builds: ServiceRequest, MarineProvider BFF, Notification, Admin BFF.
2. **Offer submission works.** A multi-line offer is created and persisted with its items. (Today it 500s — this is
   the regression test for the EF fix.)
3. Offer create/update/withdraw all take the provider identity from the assertion; a request body carrying a
   different `ProviderProfileId` is ignored, not honoured.
4. Provider A cannot read, edit or withdraw provider B's offer — by raw API call, not just via the UI.
5. A provider cannot read a service request they have no relationship with; the refusal is indistinguishable from
   "not found".
6. `PublishServiceRequest` puts a message on RabbitMQ (show it), the BFF consumer receives it, and it is pushed to
   `city:{code}` — and to **nobody else**.
7. `AcceptOffer` reaches only `provider:{profileId}`.
8. An unauthenticated or unresolvable hub connection is rejected.
9. Notification's ServiceRequest consumers now actually fire (they have been dead). Say which ones you enabled and
   which you left alone.
10. **Two replicas.** Scale the BFF to 2 (`docker compose up --scale`, or run two instances). Connect a provider
    to one, publish an event so the *other* one consumes it, and show the frame arriving. Without the Redis
    backplane this test fails — that is exactly the point of it. Paste the result.
11. Confirm the consumer model (one replica per message vs all replicas) with evidence, and state what the ingress
    needs for WebSockets.

**If you cannot verify something, say so.** Do not describe an unverified item as done — three reports in this
codebase have already claimed work that the code contradicted.

---

## Constraints

- Provider identity comes from the BFF assertion. Never from a body, never from a query param, never from a hub
  method argument.
- Fail closed. A missing `UserId`/`ProfileId` is a rejection, not `0`, and not a default group.
- Publish on the bus only after the state change is committed.
- No `System.Text.Json.JsonElement` on wire contracts (MVC binds with Newtonsoft; the data vanishes silently).
- No silent no-ops. An unimplemented path throws; it does not pretend to succeed.
- Totals and prices are computed server-side.
- Geo/radius belongs to GeoDiscovery, not here. City equality only.
- **Kubernetes, many replicas.** No in-process state, no in-memory connection maps. Redis backplane on every hub.
  Postgres/RabbitMQ/Redis/MinIO/Keycloak are external to the cluster.
- Consumers that mutate state are idempotent — at-least-once delivery is the norm, not the exception.
- If something cannot be finished, leave the TODO **and say so in the summary**.
