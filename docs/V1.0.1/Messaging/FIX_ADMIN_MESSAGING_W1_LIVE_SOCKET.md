# FIX — Admin messaging Wave 1: live socket on the Aizen.Core.Realtime framework, hosted at the AdminPanel BFF

> **Repos:** `addesso-project` (AdminPanel BFF) and `inktavia-marine-admin-web` (FE). Wave 1 of admin `/app/messages`
> ("Communication Audit"): make the realtime feed connect so an admin observes user↔provider conversations live.
>
> **Follows the realtime standard** in `docs/V1.0.1/Architecture/ADR_REALTIME_EDGE_STANDARD.md`: the hub is **hosted on
> the BFF and built on `Aizen.Core.Realtime`** (NOT hand-rolled `AddSignalR`+`Hub`, and NOT browser→module). The browser
> talks only to the BFF; the Messaging module stays publish-only.
>
> **⚠️ Supersedes both earlier drafts of this file** (browser→module hub; and hand-rolled `AddSignalR` on the BFF). Use
> the framework approach here.

## Root cause recap
The FE UI, AdminPanel BFF `AdminMessagingController` (HTTP, proxying the module), the module controllers, and the module
hub are all real. The socket is dead only because there is **no BFF-hosted realtime edge** for admin messaging (the
AdminPanel BFF has no `AddAizenRealtime`/hub/consumer), and the FE waits on a `hubUrl` nobody supplies. The Messaging
module already publishes `MessagingMessageSentMessage` on the bus — we just need to bridge it to a BFF-hosted framework
hub.

## PART A — AdminPanel BFF: host a framework hub (layer-1 plumbing, reused)
In the AdminPanel BFF `Program.cs`, wire the shared framework (mirror how the Messaging module does it, but on the BFF):
- `builder.Services.AddAizenRealtime(builder.Configuration, o => { /* BFF hosts its own hub; keep module-mapper
  auto-registration off if it pulls module types */ });`
- `builder.Services.AddDomainHub<AdminMessagingHub>("admin-messaging");`
- Ensure the backplane + CORS come from config: `Realtime:SignalR:RedisConnectionString` (Redis backplane — **required**
  for k8s multi-replica) and `Realtime:SignalR:AllowedOrigins` (must include the admin-web origin; CORS must be applied
  **before** authentication in the BFF pipeline so the hub preflight isn't 401'd).
- `app.MapHub<AdminMessagingHub>("/hubs/admin-messaging");`

## PART B — `AdminMessagingHub : DomainHubBase` (layer-3 identity/group policy)
New hub in the BFF `Realtime/` dir, `[Authorize(Policy = "AdminPanelAccess")]`, `DomainName => "admin-messaging"`.
- **Server decides groups — never the client.** On connect, add the connection to a single `admin:messaging` group (an
  admin is authorized to observe all conversations). Fail closed (`Context.Abort()`) if the admin identity/role can't be
  resolved. **No** client-callable `JoinConversation`/`Subscribe` methods.

## PART C — event → group mapping + generic consumer (layer-2 declaration, the only per-surface code)
- **`AdminMessagingEventSocketMapper : IEventSocketMapper`**: for `MessagingMessageSentMessage`, `Map(evt)` returns a
  **thin** `RealtimeMessage` — event name `"messagingEvent"`, payload `{ type = "MessageAdded", conversationId }` (**no
  message content on the frame**, per the standard); `GetTargets(evt)` returns groups `["admin:messaging"]`, no user ids.
- Register the framework's **generic** consumer for the module event:
  `RealtimeEventConsumer<MessagingMessageSentMessage, …>` (do **not** hand-roll a consumer). Wire the mapper into the
  ingress so consumed events are broadcast via the framework path to the hub group. Ensure the BFF runs as a bus consumer
  (replicate whatever `AppType.Worker`/`AddConsumer` setup MarineProvider uses so its consumers actually run).
- **Coverage note (honest):** the module publishes `MessagingMessageSentMessage` only for non-internal-note messages
  aimed at participants — exactly the user↔provider traffic an admin wants to observe. Internal-note / flag / status live
  events are **not** on the bus yet; deferred to a later wave (add thin admin bus events then). This is a layer-2-only
  addition when needed — no plumbing changes.

## PART D — FE: connect to the BFF hub, refetch on frame
`inktavia-marine-admin-web/src/features/messages/hooks/useMessagingHub.ts` — mirror
`inktavia-marine-provider-web/src/shared/realtime/providerRealtime.ts`:
- Derive the hub URL from the **admin BFF origin**, not `listData.hubUrl`:
  `const origin = new URL(env.VITE_BFF_BASE_URL_RESOLVED, window.location.origin).origin;` →
  `${origin}/hubs/admin-messaging`. Drop the `hubUrl` field from `ConversationListResponse` usage + the FE type. **No
  backend HTTP response change is needed.**
- `accessTokenFactory` refreshes the token on each (re)connect (long-lived socket).
- **Remove** the client invokes `JoinConversation`/`LeaveConversation`/`SubscribeToModerationQueue` (server-side groups
  now). On the `"messagingEvent"` frame, **refetch**: invalidate the conversation list, and if
  `event.conversationId === selectedId`, invalidate that detail — do **not** append `event.message`.
- Keep the `HubStatusDot`.

## PART E — module stays publish-only
Do not delete the Messaging module's `MessagingHub`/`MapHub("/hubs/messaging")` in this wave, but the FE must not target
it. (Its full retirement + the MarineProvider migration onto the framework are the separate standardization task in the
ADR.)

## Verification (on-screen)
Fresh admin login on `/app/messages`:
1. Status dot **connecting → Live**; WebSocket 101 to **`{admin-bff}/hubs/admin-messaging`** (not a module), authorized
   with the admin token.
2. A real user↔provider message (or a seeded `MessagingMessageSentMessage` on the bus) updates the admin list/detail
   **live without refresh** (refetch on the thin frame).
3. Reconnect refreshes the token. Confirm `Realtime:SignalR:RedisConnectionString` is wired (backplane).
4. BFF builds clean; FE typecheck/lint clean. Waves 2–4 untouched. **No hand-rolled `AddSignalR` / per-event consumer**
   was added — only `AddAizenRealtime` + `AddDomainHub` + one mapper + the generic consumer registration.

## Report
`docs/V1.0.1/Messaging/REPORT_FIX_ADMIN_MESSAGING_W1.md`: the framework wiring on the BFF (AddAizenRealtime + AddDomainHub
+ mapper + generic consumer), the Redis/CORS config, the FE rewrite, confirmation the browser hits only the BFF hub and
no hand-rolled SignalR was introduced, and the on-screen transcript. Note the deferred internal-note/flag/status
coverage.
