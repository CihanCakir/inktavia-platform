# REPORT — MarineProvider realtime migrated onto Aizen.Core.Realtime (behavior-preserving) + Messaging module publish-only

**Task:** `docs/V1.0.1/Architecture/MIGRATE_PROVIDER_REALTIME_TO_FRAMEWORK.md` (standardization debt from
`ADR_REALTIME_EDGE_STANDARD.md`). Move the MarineProvider BFF's hand-rolled SignalR realtime onto the shared
framework so there is **one** realtime style, mirroring admin messaging W1
(`AdminMessagingHub` + `AdminMessagingEventSocketMapper` + generic `RealtimeEventConsumer`).

**Status:** Part A + Part B **done, built, and live-verified at the connection/wiring layer**. Part C
(module publish-only) **deferred** as an intentionally separable follow-up (grep prerequisite completed — see §6).

> Consumer count: the task doc says "8 hand-rolled consumers"; the repo actually had **7**
> (`OfferViewedByCustomer` / `OfferRevisionRequested` have `ProviderRealtimeEventTypes` constants but never had
> consumers). All 7 were migrated. The two unused constants are left untouched in `ProviderRealtimeEvent.cs`.

---

## 1. What changed (Part A — provider BFF onto the framework)

| File | Change |
|---|---|
| `Aizen.Bff.MarineProvider.csproj` | Added project refs to `Aizen.Core.Realtime` + `Aizen.Core.Realtime.Abstraction` (mirrors admin csproj). |
| `Program.cs` | Replaced `AddSignalR()` + `AddStackExchangeRedis(...)` with `AddAizenRealtime(config, o => o.RegisterModuleMappers = false)` + `AddDomainHub<ProviderRealtimeHub>("provider")` + `AddDomainHub<ProviderRealtimeHub>("city")` + `AddSingleton<IEventSocketMapper, ProviderEventSocketMapper>()`. `MapHub<ProviderRealtimeHub>("/hubs/provider")`, the `AppType.Worker` consumer host, the shared BFF CORS-before-auth, and forwarded-headers/rate-limiter all **unchanged**. |
| `Realtime/ProviderRealtimeHub.cs` | Base changed `Hub` → `DomainHubBase`; added `IRealtimePublisher` to the ctor (`: base(publisher)`) and `public override string DomainName => "provider";`. **`OnConnectedAsync` and the `ProviderGroup`/`CityGroup` helpers are byte-for-byte unchanged** — the identity/group security boundary is identical. |
| `Realtime/ProviderEventSocketMapper.cs` | **New.** The single per-surface routing declaration; ports all 7 consumers' routing + filters (see §4). |
| `Realtime/ProviderRealtimeConsumers.cs` | **New.** 7 zero-logic `RealtimeEventConsumer<TMessage, AizenMessageResult>` closing subclasses so the messagebus consumer scan discovers/hosts each (same pattern as `AdminMessagingRealtimeConsumer`). |
| `Realtime/{MessageAdded,OfferAccepted,OfferRejected,ServiceRequestPublished,ServiceRequestUpdated,ServiceRequestCancelled,ServiceRequestUrgencyChanged}RealtimeConsumer.cs` | **Deleted** (7 hand-rolled consumers). |
| `Realtime/ProviderRealtimeEvent.cs` | **Unchanged** — payload contract preserved exactly. |

No config change was needed: the compose/k8s env already sets `Realtime__SignalR__UseRedisBackplane=true` +
`Realtime__SignalR__RedisConnectionString` (DB 15) for both provider BFF replicas, which `AddAizenRealtime` reads.

---

## 2. Two framework subtleties that would have caused silent regressions (and how they were handled)

### 2a. Group-prefix routing → the hub is registered under **two** domain keys (`"provider"` and `"city"`)
The framework's `SignalRSocketManager` resolves the target hub for a group broadcast by parsing the group name's
**prefix up to the first `:`** as the domain key (`ParseDomainFromStream`). The provider broadcasts to two prefixes:
`provider:{id}` (offers, messages) and `city:{code}` (SR published/updated/cancelled/urgency). Registering only
`"provider"` would leave `"city"` unresolved → **every city-targeted event silently dropped** ("a provider stops
seeing new city requests" — exactly the regression the doc warns about). Fix: `AddDomainHub<ProviderRealtimeHub>("provider")`
**and** `AddDomainHub<ProviderRealtimeHub>("city")` — both keys map to the same hub, a supported use of the existing
registry (no framework change). This is the provider-specific reason admin (single `admin-messaging:all` prefix)
needed only one registration. `DomainName` is informational and does **not** drive routing.

### 2b. Skipping a filtered event is done by returning **`null` from `Map`**, not empty targets
`RealtimeIngressService`: if a mapper returns a non-null `Map` **but empty `GetTargets`**, it **falls back to
broadcasting `msg.Stream` as a channel** — it does *not* skip. So "return empty targets to skip" (as the task doc
phrased it) would actually re-broadcast filtered events (e.g. a provider's own message self-notifying). The mapper
therefore drives both `Map` and `GetTargets` from one `Resolve(evt)` method: a filtered-out event makes `Resolve`
return `null` → `Map` returns `null` → the ingress early-returns (`if (msg == null) return;`) → no broadcast. When
`Resolve` is non-null, `GetTargets` returns exactly the one target group, so the fallback path is never reached.

---

## 3. Client contract preserved
- Old: module method `"providerEvent"` carrying a `ProviderRealtimeEvent`.
- New: framework method `"ReceiveEvent"` carrying `RealtimeMessage { Type = "providerEvent", Payload = ProviderRealtimeEvent }`.
- The FE reads `env.payload` when `env.type === 'providerEvent'` → receives the **same `ProviderRealtimeEvent` object,
  same camelCase fields** (`eventType`, `serviceRequestId`, `title`, `marinaName`, `messageSenderType`, …). Hub URL
  `/hubs/provider`, group names, and payload shape are unchanged.

---

## 4. Behavior-preservation matrix (verified against the deleted consumers)

| Bus message | EventType | Target group | Filter → **skip** when… | Payload fields set |
|---|---|---|---|---|
| `ServiceRequestMessageSentMessage` | `MessageAdded` | `provider:{ProviderProfileId}` | `SenderType ∉ {Owner, System}` **or** `ProviderProfileId` null/≤0 | `ServiceRequestId`, `MessageSenderType = SenderType.ToString()` |
| `ServiceRequestOfferAcceptedMessage` | `OfferAccepted` | `provider:{ProviderProfileId}` | `ProviderProfileId ≤ 0` | `ServiceRequestId`, `OfferId` |
| `ServiceRequestOfferRejectedMessage` | `OfferRejected` | `provider:{ProviderProfileId}` | `ProviderProfileId ≤ 0` | `ServiceRequestId`, `OfferId` |
| `ServiceRequestPublishedMessage` | `ServiceRequestPublished` | `city:{LocationCityCode}` | `LocationCityCode` blank | `ServiceRequestId`, `RequestCode`, `Title`, `CityCode`, `MarinaName`, `OccurredAt = PublishedAt` |
| `ServiceRequestUpdatedMessage` | `ServiceRequestUpdated` | `city:{LocationCityCode}` | `LocationCityCode` blank | `ServiceRequestId`, `RequestCode`, `CityCode` |
| `ServiceRequestCancelledMessage` | `ServiceRequestCancelled` | `city:{LocationCityCode}` | `LocationCityCode` blank | `ServiceRequestId`, `RequestCode`, `CityCode` |
| `ServiceRequestUrgencyChangedMessage` | `ServiceRequestUrgencyChanged` | `city:{LocationCityCode}` | `LocationCityCode` blank | `ServiceRequestId`, `RequestCode`, `CityCode` |

`ProviderGroup`/`CityGroup` (incl. the `city` uppercase-trim normalisation) come from the unchanged
`ProviderRealtimeHub` static helpers, so group naming is identical to the old consumers. Each row was checked
one-to-one against the corresponding deleted `*RealtimeConsumer.cs` `ExecuteCommitMessage`. No drift.

---

## 5. Part B — provider FE (`useProviderRealtime.tsx`)
The event body was extracted into `const handle = (event: ProviderRealtimeEvent) => { … }` (dedupe + the same
`switch (event.eventType)` — toasts, cache invalidations, self-message rules all unchanged). The registration
changed from:
```ts
conn.on('providerEvent', (event) => { … })
```
to the framework envelope (casing-tolerant, mirrors admin):
```ts
conn.on('ReceiveEvent', (...args) => {
  const env = args[0] as { type?: string; Type?: string; payload?: unknown; Payload?: unknown }
  if ((env.type ?? env.Type) !== 'providerEvent') return
  handle((env.payload ?? env.Payload) as ProviderRealtimeEvent)
})
```
Hub URL, `accessTokenFactory` token refresh, StrictMode deferred-teardown, and all downstream handling are unchanged.

---

## 6. Part C — Messaging module publish-only (DEFERRED, prerequisite done)
**Grep prerequisite completed — nothing connects to the module hub:**
- admin-web `useMessagingHub.ts` → `${adminBff}/hubs/admin-messaging` (admin BFF), **not** the module.
- provider-web → `${providerBff}/hubs/provider` (provider BFF).
- No other FE/service references `/hubs/messaging`.

**Decision: DONE** (executed after Part A/B were fully verified — §7). Two edits to
`Modules/Messaging/src/Aizen.Modules.Messaging/Program.cs`:
- Removed `builder.Services.AddDomainHub<MessagingHub>("messaging")`.
- Removed `app.MapHub<MessagingHub>("/hubs/messaging")`.

**Kept** (module stays publish-only, not silent): `AddAizenRealtime`, the event registry
(`MessagingRealtimeDomainRegistrar`), and — untouched — the bus publishing (`SendMessageCommandHandler` →
`MessagingMessageSentMessage`) that both the admin and provider surfaces consume. `MessagingRealtimePublisher`'s
SignalR broadcasts now no-op (no `"messaging"` domain hub resolves); left inert for a later cleanup — this deploy
only retires the browser edge. The `MessagingHub` class file is left in place (harmless, unreferenced by Program).

**Verified:** module builds clean (0 errors); `messaging-api` rebuilt + recreated, boots clean (no DI break from the
removed `AddDomainHub`); the `hubs/messaging` route string is **gone from the published DLL** and `/hubs/messaging`
now behaves exactly like any unmapped path (this API returns 401 for every unmatched route — confirmed against a
random control path). Admin's bus path is intact: the `MessagingMessageSentMessage.Aizen{Prepare,Commit,Rollback}`
exchanges still exist and the **`AdminMessagingRealtime` queue still has its consumer bound** — so admin messaging
realtime is unaffected. Nothing connected to `/hubs/messaging` (grep across all web repos + services was clean).

---

## 7. Verification

### Build / typecheck — clean
- `dotnet build Aizen.Bff.MarineProvider.csproj` → **Build succeeded, 0 errors** (217 pre-existing nullable warnings).
- provider-web `tsc --noEmit` → **clean**. `eslint` on the two realtime files → the only error (`react-hooks/refs` at
  `useProviderRealtime.tsx:40`, `tRef.current = t`) is **pre-existing at HEAD** (confirmed via git-stash) and untouched
  by this change; the diff is +16/-1, all in the `ReceiveEvent` handler.

### Runtime — live (docker stack, both replicas rebuilt on the new image)
- **Consumer discovery:** both `bff-marineprovider` and `bff-marineprovider-2` boot clean and MassTransit configures
  all 7 endpoints: `MessageAddedRealtime`, `OfferAcceptedRealtime`, `OfferRejectedRealtime`,
  `ServiceRequestPublishedRealtime`, `ServiceRequestUpdatedRealtime`, `ServiceRequestCancelledRealtime`,
  `ServiceRequestUrgencyChangedRealtime`. No DI/startup errors (the singleton `IEventSocketMapper` → ingress resolved).
  *(Both replicas were rebuilt to avoid a split-brain where the old replica would deliver on the retired `providerEvent`
  method that the new FE no longer listens to.)*
- **Hub reachable & secured on the migrated BFF:** unauthenticated `POST /hubs/provider/negotiate` → **401**
  (`[Authorize]` enforced); the live provider portal's `OPTIONS` → **204** (CORS before auth) and authenticated
  `POST negotiate` → **200**.
- **FE running the new code:** Vite serves the transformed `useProviderRealtime.tsx` containing the `ReceiveEvent`
  listener; the reloaded portal connects successfully.
- **Group membership preserved (server-side):** BFF log on connect —
  `Realtime connected: provider 100011, city 35, connection …` — the unchanged `OnConnectedAsync` resolved the
  provider profile server-side and joined **both** `provider:100011` and `city:35`. Fail-closed path intact.

### Per-event delivery transcript (multi-actor, live) — ALL ROWS PASS
Provider-under-test: **provider 100011 ("PROVIDER 2 AS"), city 35**, connected to `/hubs/provider` on the migrated
BFF (joined `provider:100011` + `city:35`). Method used: a browser-side tap registered an extra handler on the
**same** live SignalR connection for both `ReceiveEvent` (framework) and the legacy `providerEvent` (old method),
recording every inbound frame's `type` + `stream` + `payload`.

Owner-side domain events were **injected as the exact module bus messages** (`AizenPrepareMessage<T>` published to the
same RabbitMQ exchanges the consumers bind, via the real contracts + `CustomMessageNameFormatter`). This targets
precisely the layer the refactor changed (consumer → mapper → `ReceiveEvent`); the owner-action→bus-publish path is
unchanged by the migration. (There is no owner web app running, and the seeded owner had no password — the user
approved bus injection over mutating stack state.)

| # | Injected bus message | EventType (frame) | Delivered on | `stream` (group) | Result |
|---|---|---|---|---|---|
| 1a | `ServiceRequestMessageSentMessage` SenderType=**Owner**, pid 100011 | `MessageAdded` | `ReceiveEvent` | `provider:100011` | ✅ arrived; payload `serviceRequestId:9011, messageSenderType:"Owner"` |
| 1b | same, SenderType=**Provider** | — | — | — | ✅ **skipped** (no frame) — self-notify filter preserved |
| 1c | real UI: provider sends message in the thread | — | — | — | ✅ **no self-notify** (message sent, zero frames) |
| 2a | `ServiceRequestPublishedMessage` city **35** | `ServiceRequestPublished` | `ReceiveEvent` | `city:35` | ✅ arrived; `cityCode:35, requestCode, title, marinaName` present; **on-screen toast "Yeni servis talebi"** |
| 2b | `ServiceRequestPublishedMessage` city **34** | — | — | — | ✅ **not delivered** — city isolation |
| 3a | `ServiceRequestOfferAcceptedMessage` pid **100011** | `OfferAccepted` | `ReceiveEvent` | `provider:100011` | ✅ arrived; `offerId:90001` |
| 3b | `ServiceRequestOfferAcceptedMessage` pid **100022** | — | — | — | ✅ **not delivered** — cross-provider isolation |
| 4a | `ServiceRequestOfferRejectedMessage` pid **100011** | `OfferRejected` | `ReceiveEvent` | `provider:100011` | ✅ arrived; `offerId:90001` |
| 4b | `ServiceRequestOfferRejectedMessage` pid **100022** | — | — | — | ✅ **not delivered** — cross-provider isolation |
| 5a | `ServiceRequestUpdatedMessage` city **35** | `ServiceRequestUpdated` | `ReceiveEvent` | `city:35` | ✅ arrived; `cityCode:35, requestCode` |
| 5b | `ServiceRequestCancelledMessage` city **35** | `ServiceRequestCancelled` | `ReceiveEvent` | `city:35` | ✅ arrived |
| 5c | `ServiceRequestUrgencyChangedMessage` city **35** | `ServiceRequestUrgencyChanged` | `ReceiveEvent` | `city:35` | ✅ arrived |
| 5d | Updated/Cancelled/Urgency city **34** | — | — | — | ✅ **none delivered** — city isolation |

**Outcome:** every one of the 7 EventTypes arrived on the framework's single `ReceiveEvent` method with the
`ProviderRealtimeEvent` payload shape **unchanged** (camelCase `eventType`/`serviceRequestId`/`cityCode`/`offerId`/
`messageSenderType`/… identical to pre-migration); every negative case (self-send, wrong city, wrong provider) was
correctly filtered; `provider:{id}` routing for messages/offers and `city:{code}` routing for SR lifecycle both
confirmed live. **Zero frames arrived on the legacy `providerEvent` method** after the split-brain fix below.

### ⚠️ Split-brain caught during verification (deploy note, now fixed)
The two provider BFF replicas build **separate images** (`bff-marineprovider` and `bff-marineprovider-2` have their own
`build:` sections). Rebuilding only the first left replica 2 on the **pre-migration image**; because the two replicas
are MassTransit competing consumers on a shared queue, RabbitMQ round-robined events between them — new-code events
arrived on `ReceiveEvent`, old-code events arrived on the retired `providerEvent`. The frame tap caught it (a
`ServiceRequestPublished` frame on `providerEvent`). Fixed by rebuilding **both** images and recreating both replicas;
re-run showed 100% `ReceiveEvent`, zero legacy. **Deploy implication:** a rolling deploy of the provider BFF must
ship the FE `ReceiveEvent` change and the BFF change together, and update **all** replicas — a mixed old/new BFF fleet
delivers ~half of events on a method the new FE no longer listens to. (In k8s this is one Deployment/one image, so the
split cannot happen there the way it does with the two hand-pinned compose services.)

_Side effect note:_ injecting real domain bus messages (SR 9011 / offer 90001) means other modules' consumers
(notifications, read-models) may have processed them too — an accepted consequence of the bus-injection method.

---

## 8. Checklist
- [x] No hand-rolled `AddSignalR` / `AddStackExchangeRedis` / `AizenBaseMessageConsumer` / `IHubContext<ProviderRealtimeHub>` / `SendAsync("providerEvent")` remain in the provider BFF (only a descriptive comment mentions the old approach).
- [x] One realtime style: framework `DomainHubBase` hub + one `IEventSocketMapper` + generic consumers.
- [x] Hub URL `/hubs/provider`, group names, `ProviderRealtimeEvent` payload unchanged.
- [x] Redis backplane preserved (DB 15, both replicas).
- [x] Provider build clean; FE typecheck clean; no new lint errors.
- [x] Admin messaging (already on the framework) untouched.
- [x] Per-event on-screen transcript (all 7 EventTypes + negatives) — PASS (see §7).
- [x] Both provider BFF replicas rebuilt on the migrated image (split-brain fixed).
- [x] Part C: module `/hubs/messaging` retired (publish-only); admin bus path intact; verified (§6).
