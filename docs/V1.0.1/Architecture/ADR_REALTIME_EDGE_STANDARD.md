# ADR — Realtime (SignalR) standard: BFF-hosted edge on Aizen.Core.Realtime, modules publish-only

**Status:** Proposed · **Scope:** all realtime/socket surfaces (provider, admin, future) · **Supersedes:** the ad-hoc
per-BFF hand-rolled SignalR pattern.

## Context
We have two divergent realtime styles today, which is the source of duplicated code:
- **Aizen.Core.Realtime** is a complete, reusable framework: `AddAizenRealtime` (SignalR runtime + optional Redis
  backplane + CORS from `Realtime:SignalR:*`), `AddDomainHub<T>`, `DomainHubBase`, a **generic**
  `RealtimeEventConsumer<TMessage>` (bus → `IRealtimeEventIngress`), declarative `IEventSocketMapper`
  (`Map(evt)` + `GetTargets(evt) → (userIds, groups)`), `IUserInfoResolver`, `IRealtimeDomainRegistrar` +
  `RealtimeEventRegistry`, and the ingress→`SignalRSocketManager`/`SignalRRealtimePublisher` broadcast path. The
  **Messaging module** uses it.
- **MarineProvider BFF** bypassed the framework and hand-rolled `AddSignalR` + `ProviderRealtimeHub : Hub` + one
  `AizenBaseMessageConsumer` per event (`MessageAddedRealtimeConsumer`, `OfferAccepted/RejectedRealtimeConsumer`) that
  re-broadcast to hub groups. This re-implements, by hand, exactly what the framework's generic consumer + mapper
  already do.

Additionally, the hub tier is inconsistent with our security model: the Messaging module hosts a **browser-facing** hub
(`MapHub<MessagingHub>("/hubs/messaging")`), but our rule is **modules are internal (ClusterIP) and the BFF is the only
public edge** — the browser authenticates against the BFF; modules are called with the BFF service assertion.

## Decision
**The realtime edge lives on the BFF and is built on Aizen.Core.Realtime. Modules are publish-only.** Concretely, three
layers, each written once:

1. **Shared plumbing — Aizen.Core.Realtime (write-once, no per-surface copies):** SignalR runtime, Redis backplane, CORS,
   `DomainHubBase`, the **generic** `RealtimeEventConsumer<TMessage>`, ingress → socket manager → broadcast, event
   registry, connection/identity middleware. No BFF hand-rolls SignalR or per-event consumers.

2. **Per-surface declaration (the only legitimately per-BFF code):** each BFF hosts **one** framework hub
   (`AddAizenRealtime` + `AddDomainHub<TSurfaceHub>` where `TSurfaceHub : DomainHubBase` + `MapHub` in Program.cs) and
   supplies:
   - an **`IEventSocketMapper`** — "this module bus event → this payload + these target groups/users" (the single home
     of routing logic), and
   - a registration of **`RealtimeEventConsumer<TMessage>`** for each module event this surface consumes.
   That is the entire per-surface footprint. No hand-rolled consumers, no per-event broadcast blocks.

3. **Server-side identity/group policy (the security boundary, once per surface):** target groups/users are derived on
   the BFF from the **BFF-resolved identity** (provider profile id, admin role, etc.) via `IEventSocketMapper.GetTargets`
   / `IUserInfoResolver` — **never** from anything the client sends. There is no client-driven "subscribe to group X".
   Fail closed when identity can't be resolved.

4. **Modules: publish-only.** Modules publish domain integration events to the bus (they already do) and **do not host
   browser-facing hubs**. The Messaging module's `MessagingHub` / `MapHub("/hubs/messaging")` is retired from browser
   exposure; its event-name registry + any mapper move to / are reused by the BFF surface that broadcasts them.

## Why this is DRY and stable
Adding a realtime surface = write **one** `IEventSocketMapper` + register the generic consumer for the events that
surface cares about + host the framework hub. The plumbing, backplane, auth filter, and broadcast path are shared. Two
surfaces (provider, admin) differ only in their mapper and identity resolution — which is exactly the part that *should*
differ. No duplicated consumers, no second SignalR style.

## Consequences / debt to pay
- **Admin messaging (now):** build its realtime on the framework at the AdminPanel BFF (see
  `docs/V1.0.1/Messaging/FIX_ADMIN_MESSAGING_W1_LIVE_SOCKET.md`). Do **not** hand-roll `AddSignalR`+`Hub` (that would add a
  third style).
- **MarineProvider BFF (migration, separate task):** replace the hand-rolled `AddSignalR` + `ProviderRealtimeHub` +
  per-event consumers with `AddAizenRealtime` + `AddDomainHub<ProviderRealtimeHub : DomainHubBase>` + an
  `IEventSocketMapper` + generic consumers. Behavior-preserving; one realtime style afterward.
- **Messaging module:** make it publish-only — retire the browser-facing `/hubs/messaging` (keep the module's bus
  publishing + event registry).
- **Framework capability:** the BFF identity→group case is supported via `IEventSocketMapper.GetTargets` +
  `IUserInfoResolver` + `DomainHubBase` channel helpers. If a concrete surface finds a gap (e.g. connect-time group join
  from a resolved profile), **extend Aizen.Core.Realtime once** — never hand-roll per surface.
- **Config standard:** every hub uses `Realtime:SignalR:RedisConnectionString` (backplane, required for k8s
  multi-replica) + `Realtime:SignalR:AllowedOrigins` (CORS, before auth in the pipeline).

## Sequencing
1. This ADR (the standard).
2. Admin messaging Wave 1 rebuilt on the framework (immediate).
3. MarineProvider realtime migration onto the framework + Messaging module made publish-only (follow-up standardization
   task).
4. All later realtime surfaces follow layer-2 only (mapper + consumer registration).
