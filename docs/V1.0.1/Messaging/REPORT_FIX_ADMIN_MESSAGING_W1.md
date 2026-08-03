# REPORT — Admin messaging Wave 1: live socket on Aizen.Core.Realtime, hosted at the AdminPanel BFF

**Specs:** `docs/V1.0.1/Architecture/ADR_REALTIME_EDGE_STANDARD.md` + `docs/V1.0.1/Messaging/FIX_ADMIN_MESSAGING_W1_LIVE_SOCKET.md`
**Repos:** `addesso-project` (AdminPanel BFF, Core.Realtime), `inktavia-marine-admin-web` (FE).
**Outcome:** the admin `/app/messages` realtime feed connects to a **BFF-hosted framework hub** and updates live via
refetch on a thin bus-driven frame. Built entirely on `Aizen.Core.Realtime` — no hand-rolled `AddSignalR`/`Hub`/
per-event consumer, and the browser never targets a module hub.

> This report supersedes an earlier draft that implemented the (now-rejected) browser→module-hub approach; those
> changes were reverted (see "Reverts").

---

## Architecture built (per the ADR: BFF-hosted edge, modules publish-only)

### PART A — AdminPanel BFF hosts one framework hub
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Program.cs`:
- `AizenAppInfo` gained `TypeInclude = { AppType.Worker }` so the shared BFF config hosts the Aizen messagebus
  **consumer** (same mechanism as MarineProvider). Confirmed at startup:
  `Configured endpoint AdminMessagingRealtime, Consumer: …AdminMessagingRealtimeConsumer`.
- `AddAizenRealtime(config, o => o.RegisterModuleMappers = false)` — shared SignalR runtime + Redis backplane +
  ingress + socket manager. Module-mapper auto-discovery is OFF (the BFF supplies its own mapper).
- `AddDomainHub<AdminMessagingHub>("admin-messaging")` + `app.MapHub<AdminMessagingHub>("/hubs/admin-messaging")`.
- `AddSingleton<IEventSocketMapper, AdminMessagingEventSocketMapper>()` (singleton: the framework's
  `RealtimeIngressService` consumes a single `IEventSocketMapper` and is itself a singleton).
- CORS: registered under the shared `AizenBffCors.PolicyName` from `Realtime:SignalR:AllowedOrigins`, so the shared
  BFF pipeline applies it **before** authentication (otherwise the credential-less hub OPTIONS preflight 401s).
- Auth: added the `OnMessageReceived` `?access_token=`→`/hubs` handler to `AddAdminPanelAuthentication`
  (`Extensions/AuthenticationExtensions.cs`) — a browser WebSocket cannot send an Authorization header.

### PART B — `AdminMessagingHub : DomainHubBase` (server-decided groups, fail-closed)
`Realtime/AdminMessagingHub.cs`: `[Authorize(Policy = "AdminPanelAccess")]`, `DomainName => "admin-messaging"`.
- On connect, the **server** adds the connection to the single `AdminGroup` and fails closed (`Context.Abort()`) if
  the connection isn't an authenticated **Admin**. **No** client-callable join/subscribe methods.
- `AdminGroup = "admin-messaging:all"` — the group prefix (up to the first `:`) must equal the `AddDomainHub` key,
  because the framework's socket manager routes a group broadcast to its hub by parsing that prefix. (Hence
  `admin-messaging:all`, not `admin:messaging` which would parse to the unregistered domain `admin`.)

### PART C — one mapper + the framework's generic consumer (the only per-surface code)
- `Realtime/AdminMessagingEventSocketMapper.cs : IEventSocketMapper` — for `MessagingMessageSentMessage`,
  `Map` returns a **thin** `RealtimeMessage` (`Type = "messagingEvent"`, payload `{ type = "MessageAdded",
  conversationId }` — **no message content**); `GetTargets` returns groups `["admin-messaging:all"]`, no user ids.
- `Realtime/AdminMessagingRealtimeConsumer.cs` — a **zero-logic** subclass of the framework's generic
  `RealtimeEventConsumer<MessagingMessageSentMessage, AizenMessageResult>`. Its only purpose is to be a non-generic
  type in the BFF entry assembly so the messagebus consumer scan discovers and hosts it (open generics aren't
  auto-closed). It adds no broadcast code — the framework consumer forwards the event to `IRealtimeEventIngress`,
  which applies the mapper and broadcasts via the shared socket path.
- **Coverage (honest):** only `MessagingMessageSentMessage` is on the bus in W1 (published for non-internal-note
  participant messages — the user↔provider traffic an admin observes). Internal-note / flag / status live events are
  **not on the bus yet** → deferred to a later wave (add thin admin bus events then; layer-2-only, no plumbing change).

### PART D — FE connects to the BFF hub, refetches on the frame
`inktavia-marine-admin-web/src/features/messages/hooks/useMessagingHub.ts` (rewritten):
- Hub URL derived from the **admin BFF origin**: `new URL(env.VITE_BFF_BASE_URL_RESOLVED, location.origin).origin +
  "/hubs/admin-messaging"`. `listData.hubUrl` and the `hubUrl` field are dropped (removed from
  `ConversationListResponse` + `ConversationDetail` FE types and `MessagesPage`). No backend HTTP response change.
- `accessTokenFactory` reads the current token from the auth store on each (re)connect (long-lived socket → each
  reconnect picks up the latest/refreshed token).
- **Removed** all client invokes (`JoinConversation`/`LeaveConversation`/`SubscribeToModerationQueue`) — groups are
  server-side. On a `"messagingEvent"` frame it **refetches** (invalidate list; invalidate the open detail if
  `conversationId` matches) — it never appends message content. `HubStatusDot` kept.
- Dev transport: added a Vite `/hubs` proxy (`ws: true`) so the same-origin dev URL reaches the BFF.

### PART E — module stays publish-only
The Messaging module's `MessagingHub` / `/hubs/messaging` is untouched; the FE no longer targets it. Its full
retirement + the MarineProvider migration are the separate ADR standardization task.

---

## Shared-framework fixes (kept — the framework path could not deliver without them)
The framework's broadcast path (`RealtimeEventConsumer` → `IRealtimeEventIngress` → `SignalRRealtimePublisher` →
`SignalRSocketManager`) had latent bugs that silently dropped **every** delivery (the ingress path was unused before
this work). Fixed in `Core/Realtime/.../Services/SignalRSocketManager.cs`:
- `RunFiltersAsync` did `ConcurrentDictionary.TryGetValue(tenantId=null)` → `ArgumentNullException` (null keys are
  forbidden), swallowed by the caller's `catch{}`. Now guards the null key.
- `InvokeGroup/User/ClientSendAsync` called SignalR's `SendAsync` via `dynamic`, but those are extension methods and
  the concrete `HubContext<THub>` is internal → `RuntimeBinderException`, swallowed. Now resolves the public
  `IHubClients` via the interface and calls the instance method `SendCoreAsync`.

Also `Core/Cache/.../BuilderExtensions.cs`: `AddAizenCache` never registered `IAizenCache` (only the memory/
distributed variants), so the Messaging module's `MessageContentPolicyService` — on the message-send path that
publishes the bus event — failed to activate. Added the missing `IAizenCache → AizenDistributedCache` binding.

## Reverts (the earlier browser→module draft)
Removed: `GetConversationListResponse.HubUrl` (Abstraction) + the BFF controller's HubUrl injection + the
`AdminMessaging:HubUrl` appsettings/compose keys; the module-side CORS (`AllowedOrigins` on messaging-api) and the
`AizenOperationApplicationConfiguration` CORS block; the module-side `OnMessageReceived` in `Core.Auth`; and the
Messaging module's internal-note broadcast guard. All belonged to the rejected browser→module design.

## Incidental FE fixes needed to make verification visible
- `endpoints.ts`: every `MESSAGING_*` endpoint was doubled (`/admin-panel/messaging/…` on a baseURL that already
  includes `/api/v1/admin-panel`) → the whole admin-messaging HTTP surface 404'd. Removed the duplicate prefix.
- `useMessagingHub`: the list invalidation used `MESSAGING_KEYS.list()` (`['messaging','list',undefined]`) which does
  not partial-match the active query key `['messaging','list',{status:…}]`; switched to the 2-element prefix so the
  frame actually triggers a refetch.

## Config (docker-compose, bff-adminpanel)
`Realtime__SignalR__UseRedisBackplane: "true"`, `Realtime__SignalR__RedisConnectionString: redis:6379,…defaultDatabase=14`
(backplane — required for k8s multi-replica), `Realtime__SignalR__AllowedOrigins__0: http://localhost:3000` (CORS).

---

## Verification (on-screen)
Fresh admin session on `/app/messages` (Docker: bff-adminpanel :17001, messaging-api :7109, admin-web :3000):
1. **Status dot connecting → Live** (green) — screenshot confirmed. Negotiate `POST /hubs/admin-messaging/negotiate`
   → **200** (same-origin via the Vite `/hubs` proxy → BFF); the dot only flips to Live after the WebSocket connects.
   The browser hits **only** the BFF hub — never a module hub.
2. **Bus event → thin frame → live refetch.** A same-origin BFF send published `MessagingMessageSentMessage`; a raw
   WS in the admin group received method **`ReceiveEvent`**, envelope type **`messagingEvent`**, payload
   **`{ type:"MessageAdded", conversationId:9900000001 }`** (no content). In the app, the frame triggered
   conversation-list **refetch GETs (200)** — live, no manual refresh.
3. **Reconnect.** Restarting the BFF dropped the socket → negotiate **502** while down → **200** when back → dot
   returned to **Live** (auto-reconnect re-invokes `accessTokenFactory`, re-reading the current token).
4. **Redis backplane** wired (env confirmed). **No hand-rolled `AddSignalR`/`IHubContext` consumer** in the BFF —
   only `AddAizenRealtime` + `AddDomainHub` + one `IEventSocketMapper` + the generic `RealtimeEventConsumer`.
5. **Builds:** AdminPanel BFF `dotnet build` clean; messaging module clean; FE `npm run typecheck` clean + `eslint`
   clean on all changed files.

## Known / out of scope (pre-existing, separate from the realtime edge)
- **Admin conversation list shows 0 / empty:** the BFF→module list call uses the BFF **service token** and the module
  returns `total:0` for it (the same query returns rows for a user token). This service-token list scoping is a
  pre-existing HTTP-layer issue, unrelated to realtime; it blocks the *visible* list content but not the frame→refetch
  mechanism (proven above). Recommend a follow-up.
- The `RealtimeEventConsumer` (with-result saga) delivered the frame more than once per send in testing; the FE
  refetch is idempotent so there is no user-visible effect. Worth revisiting if event volume grows.
- Verification used a seeded conversation (`9900000001`) with a `UserId=0` participant so the dev send path (which
  resolves the acting user to `0` without the BFF trusted-assertion secret) could publish the bus event.

Waves 2–4 (flag/moderate UI, moderation-queue/reports panels, i18n) untouched. MarineProvider migration NOT done
(separate ADR task).
