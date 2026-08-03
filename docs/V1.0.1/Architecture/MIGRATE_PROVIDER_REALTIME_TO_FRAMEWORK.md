# MIGRATE — MarineProvider realtime onto Aizen.Core.Realtime (behavior-preserving) + Messaging module publish-only

> **Repos:** `addesso-project` (MarineProvider BFF + Messaging module) and `inktavia-marine-provider-web` (FE). This is
> the standardization debt from `docs/V1.0.1/Architecture/ADR_REALTIME_EDGE_STANDARD.md`: move the provider BFF's
> **hand-rolled** SignalR realtime onto the shared framework (as admin messaging W1 already did), so there is **one**
> realtime style. **Refactor of working, live code — must be behavior-preserving.** The reference implementation is the
> admin W1 (`AdminMessagingRealtimeHub` + `AdminMessagingEventSocketMapper` + generic `RealtimeEventConsumer`).
>
> The framework client contract (confirmed): the framework sends **every** event through the single SignalR method
> **`ReceiveEvent`** with a `RealtimeMessage` envelope `{ Type, Stream, AggregateId, Payload }`. The FE dispatches on
> `Type`. (Admin FE: `connection.on('ReceiveEvent', env => if (env.type==='messagingEvent') …)`.)

## Current state (what must be preserved)
- **Hub:** `ProviderRealtimeHub : Hub`, `[Authorize]`, at `{PROVIDER_BFF}/hubs/provider`. `OnConnectedAsync` resolves the
  provider profile **server-side** (`IProviderProfileResolver`/`IProviderIdentityHolder`) and joins
  `provider:{profileId}` + `city:{cityCode}`; **fails closed** if unresolved. Group helpers `ProviderGroup`, `CityGroup`.
- **8 hand-rolled consumers** (`AizenBaseMessageConsumer<T>`), each `_hub.Clients.Group(group).SendAsync("providerEvent",
  new ProviderRealtimeEvent { EventType = …, … })`:
  MessageAdded, OfferAccepted, OfferRejected, ServiceRequestPublished, ServiceRequestUpdated, ServiceRequestCancelled,
  ServiceRequestUrgencyChanged (verify the exact set + each one's **group** and **filters**, e.g. MessageAdded only fires
  for `Owner`/`System` sender; each skips when it carries no provider profile id; ServiceRequestPublished targets
  `city:{code}` while the rest target `provider:{id}`).
- **Client contract:** `ProviderRealtimeEvent` (`EventType` + fields) delivered on method `"providerEvent"`. The FE
  (`providerRealtime.ts`) does `connection.on('providerEvent', ev => …)`.

## PART A — provider BFF onto the framework

### A1. Program.cs
- Replace the raw `AddSignalR()` (+ `AddStackExchangeRedis(...)`) block with **`AddAizenRealtime(builder.Configuration)`**
  + **`AddDomainHub<ProviderRealtimeHub>("provider")`**. Keep `app.MapHub<ProviderRealtimeHub>("/hubs/provider")`
  **unchanged** (same URL → **no FE URL change**). Keep the `AppType.Worker`/consumer-host setup so bus consumers run.
- Backplane: keep the existing `Realtime:SignalR:RedisConnectionString`; ensure `AddAizenRealtime` picks it up (set
  `Realtime:SignalR:UseRedisBackplane=true` + `RedisConnectionString` per `SignalRSettings`). CORS stays before auth.

### A2. `ProviderRealtimeHub : DomainHubBase`
- Change the base to `DomainHubBase`; add `public override string DomainName => "provider";` and the base ctor dependency
  `IRealtimePublisher` (alongside the existing resolver/identity/logger injections).
- **Keep `OnConnectedAsync` exactly as-is** (the provider:{id} + city:{code} resolution + fail-closed + `await
  base.OnConnectedAsync()`). This is the security boundary and stays identical. Keep `ProviderGroup`/`CityGroup` helpers.

### A3. One `ProviderEventSocketMapper : IEventSocketMapper` (replaces all 8 consumers' routing)
Port each consumer's routing + filter into a single mapper (mirror `AdminMessagingEventSocketMapper`):
- `Map(evt)` → `new RealtimeMessage { Type = "providerEvent", Stream = <target group>, AggregateId = <sr/offer id>,
  Payload = new ProviderRealtimeEvent { EventType = …, … } }` — **payload shape unchanged** so the FE gets the same
  object. Switch over the bus message type to set `EventType` + fields exactly as the old consumer did.
- `GetTargets(evt)` → the **same group(s)** the old consumer used, computed from the message
  (`ProviderGroup(message.ProviderProfileId)` for most; `CityGroup(message.CityCode)` for ServiceRequestPublished).
  **Apply the same filters** by returning **empty targets** to skip (e.g. MessageAdded when sender ∉ {Owner, System};
  any message missing a provider profile id / city). Empty targets = no broadcast, preserving today's behavior.
- `Extract(evt)` helper accepting both the raw message and `EventDto { Data = message }` (as the admin mapper does).
- Register it in DI as `IEventSocketMapper` (mirror how the admin mapper is registered).
- **Preserve `ProviderRealtimeEvent` + `ProviderRealtimeEventTypes` unchanged.**

### A4. Register generic consumers, delete hand-rolled ones
- For **each** of the module bus messages the provider surface consumes, register the framework's generic
  `RealtimeEventConsumer<TMessage, …>` (mirror the admin `AdminMessagingRealtimeConsumer` zero-logic subclass so the
  consumer scan discovers each type). These feed the ingress → mapper → `ReceiveEvent`.
- **Delete** the 8 hand-rolled `*RealtimeConsumer.cs` files once their logic lives in the mapper. No `IHubContext...
  SendAsync("providerEvent", …)` should remain.

### A5. Behavior-preservation matrix (fill in + verify — the risk area)
Build a table of every event: **bus message → EventType → target group → filter**. Confirm the mapper reproduces each row
exactly against the deleted consumers. Any drift = a live provider regression (e.g. a provider stops seeing new
city requests, or gets messages they shouldn't).

## PART B — provider FE adapt to the framework frame (`providerRealtime.ts`)
Change the listener from the direct method to the framework envelope (mirror admin):
```ts
// was: connection.on('providerEvent', (ev: ProviderRealtimeEvent) => handle(ev))
connection.on('ReceiveEvent', (...args) => {
  const env = args[0] as { type?: string; Type?: string; payload?: unknown; Payload?: unknown }
  if ((env.type ?? env.Type) !== 'providerEvent') return
  handle((env.payload ?? env.Payload) as ProviderRealtimeEvent)
})
```
Everything downstream (the `handle`/dispatch on `EventType`, the hub URL `{PROVIDER_BFF}/hubs/provider`, the
`accessTokenFactory` refresh) stays. **UX unchanged** — same events, same effects.

## PART C — Messaging module publish-only (retire the browser hub)
Now that no browser targets the module hub (admin → admin BFF hub, provider → provider BFF hub), retire the Messaging
module's browser-facing realtime:
- Remove `app.MapHub<MessagingHub>("/hubs/messaging")` + the `AddDomainHub<MessagingHub>` on the **module**; keep the
  module's **bus publishing** (`MessagingMessageSentMessage`) + its event registry.
- **Before removing, grep** that nothing (no FE, no other service) still connects to the module `/hubs/messaging`.
- This can be a **separate deploy after Part A/B** are verified (lower coupling). If uncertain, leave the module hub
  mapped but unused and remove in a follow-up — do not block Part A/B on it.

## Don't-break / QA
- No change to the provider **HTTP** endpoints, the hub URL, group names, `ProviderRealtimeEvent` payload, or the
  provider's UX. `AddAizenRealtime` already carries the W1 framework fixes (socket-manager null-key / `SendCoreAsync` /
  `IAizenCache`), so the ingress path works. Redis backplane preserved.
- Provider builds clean; FE typecheck/lint clean. Admin messaging (already on the framework) unaffected.

## Verification (on-screen, live)
1. Provider web connects to `/hubs/provider` → Live; the connection joins `provider:{id}` (+ `city:{code}`) server-side
   exactly as before.
2. Exercise **each** event end-to-end and confirm the provider still receives it with the same effect: a new message
   (Owner→provider) toasts/refetches; a new service request **in the provider's city** appears; offer accepted/rejected;
   SR updated/cancelled/urgency-changed. A message from the provider itself does **not** self-notify (filter preserved).
3. Nothing is delivered cross-provider (group isolation preserved).
4. (Part C) module `/hubs/messaging` no longer needed; admin + provider realtime both work.
5. Backend build + FE typecheck/lint clean; one realtime style remains (no hand-rolled `AddSignalR`/consumers in the BFF).

## Report
`docs/V1.0.1/Architecture/REPORT_MIGRATE_PROVIDER_REALTIME.md`: the Program/hub/mapper/consumer changes, the
behavior-preservation matrix (all events verified), the FE listener change, the module publish-only decision (done or
deferred), and the on-screen per-event transcript confirming no regression.
