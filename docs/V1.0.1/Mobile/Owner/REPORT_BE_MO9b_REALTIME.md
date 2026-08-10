# REPORT — BE_MO9b mobile realtime edge (owner live notification bell)

> MO9 **phase b**: stand up the BFF-hosted realtime edge for the owner app so the notification bell updates live.
> Modeled on the **`AdminNotification` pair** (per-recipient group, single canonical event) on `Aizen.Core.Realtime`;
> modules publish-only. Mobile BFF only (`Bff/src/Marine.Participant.Mobile`). Additive; identity from token;
> cost-free. **NOT committed.**

## Outcome
- **Builds clean** — the mobile BFF host: 0 errors (its csproj already referenced `Aizen.Core.Realtime`,
  `.Abstraction`, `Notification.Abstraction`, and `SignalR.StackExchangeRedis` — no csproj change needed).
- **Tests: 8/8 green** — a new `Aizen.Bff.Marine.Participant.Mobile.UnitTests` (the mapper).
- **No module changes** (modules already publish `NotificationSentMessage`); no existing BFF endpoint touched.

---

## Why one event, not per-domain fan-out
The Notification module already fans **every** owner notification through **one** canonical bus event,
`NotificationSentMessage`, carrying exactly what a live bell needs (`RecipientUserId`, `Type`, `Channel`, `Title`,
`SentAt`, deep-link `ReferenceType`/`ReferenceId`). So the owner bell subscribes to **one** event and inherits the
whole N-A..N-E owner event set (offers, payment, completion, dispute, change-order, maintenance-N2, price-change) for
free — no per-domain re-mapping, and **no economics on the wire**. This is the admin-notification approach ported to
the mobile BFF.

## BE — the mobile realtime edge (new `Realtime/` folder in `Aizen.Bff.Marine.Participant.Mobile`)
1. **`MobileRealtimeHub : DomainHubBase`** — `[Authorize(ParticipantAuthenticated)]`, `DomainName =>
   "mobile-notification"`, `UserGroup(id) => "mobile-notification:{id}"` (the prefix equals the registered domain key
   — the socket manager routes by prefix). `OnConnectedAsync` resolves the participant's numeric Identity user id
   server-side (`IParticipantProfileResolver` → `IParticipantIdentityHolder.UserId`) and joins **only** that user's
   group; **fails closed** (`Context.Abort()`) if unresolved. **No client-callable join** — a participant can only ever
   observe their own `mobile-notification:{ownId}` group.
2. **`MobileNotificationEventSocketMapper : IEventSocketMapper`** — the single per-surface routing declaration:
   `Resolve` (one place feeding both `Map` and `GetTargets`) unwraps the raw **and** `EventDto`-wrapped forms and fires
   **only** when `Channel == InApp` (the bell is the in-app surface; Push is MO9a) **and** `RecipientUserId > 0`; else
   returns null → **dropped at `Map`** (an empty `GetTargets` would BROADCAST, not skip — the framework gotcha). Frame:
   `Type = "mobileNotification"`, `Stream/Group = UserGroup(RecipientUserId)`, `AggregateId = NotificationId`,
   `Payload = MobileRealtimeNotification` — a **thin, cost-free** record `{ NotificationId, Type, Title, ReferenceType,
   ReferenceId, SentAt }` (Title + type + deep-link only; **no amounts/commission/net/margin/provider ids**). +
   `MobileRealtimeNotificationTypes.FrameType`.
3. **`MobileNotificationRealtimeConsumer : RealtimeEventConsumer<NotificationSentMessage, AizenMessageResult>`** —
   zero-logic closing subclass (ctor `(IServiceProvider sp) : base(sp)`) so the messagebus scan discovers + hosts it.
4. **Program.cs wiring** (mirrors MarineProvider/AdminPanel):
   - `AizenAppInfo.TypeInclude = { AppType.Worker }` added (enables bus consumption — without it `AddAizenMessagebus`
     sets `AddConsumer=false` and the consumer never runs). `AppType.Bff` kept.
   - `AddAizenRealtime(configuration, o => o.RegisterModuleMappers = false)`.
   - `AddDomainHub<MobileRealtimeHub>("mobile-notification")` (one group prefix → one key).
   - `AddSingleton<IEventSocketMapper, MobileNotificationEventSocketMapper>()`.
   - `app.MapHub<MobileRealtimeHub>("/hubs/notification")`.
   - CORS unchanged — the existing shared BFF policy (`Cors:AllowedOrigins`, applied before auth) already answers the
     hub's credentialed preflight.

### Redis backplane (K8s multi-replica — REQUIRED)
Added to the `bff-marine-mobile` docker-compose service:
```
Realtime__SignalR__UseRedisBackplane: "true"
Realtime__SignalR__RedisConnectionString: redis:6379,abortConnect=false,defaultDatabase=15
Realtime__SignalR__AllowedOrigins__0: ${MARINE_MOBILE_BASE:-http://localhost:19006}
```
Realtime uses a **separate Redis DB (15)** from the mobile cache (**16**) so a cache `FLUSHDB` never drops live
sockets. Sharing DB 15 with the other realtime services is safe — every app's groups are prefix-isolated
(`mobile-notification:{id}` vs `provider:{id}`), so a cross-broadcast lands in a group with no local connections and is
dropped. For K8s, set the same three keys on the mobile BFF pod (a realtime DB ≠ the cache DB).

---

## Don't-break / QA
- Additive: a new `Realtime/` folder + the Worker flag + the realtime registrations + the compose env. No module
  changes; no existing HTTP passthrough touched.
- **Worker enabled ⇒ the BFF now consumes the bus** — it hosts **only** the realtime consumer: the sole concrete
  `IAizenMessageConsumer` in the BFF assembly (and its Abstraction-only module refs) is
  `MobileNotificationRealtimeConsumer` (no command consumers — those live in module `.Application` assemblies the BFF
  doesn't reference). Build-verified.
- **Cost-free by construction** — grep the frame payload (`MobileRealtimeNotification`): no amount/commission/net/
  margin/provider-id fields (a reflection test guards it).
- **Security** — the group is decided server-side from the resolved participant id; fail-closed on unresolved; no
  client-callable join; a participant can only join their own group.

### Tests (`MobileNotificationEventSocketMapperTests` — 8/8)
- **InApp + recipient>0** → frame `Type="mobileNotification"`, `Stream="mobile-notification:{id}"`,
  `AggregateId=NotificationId`, thin `MobileRealtimeNotification` payload; `GetTargets` → the one recipient group.
- **Push / Email / Sms → skip** (null `Map` + empty targets = SKIP, not broadcast).
- **recipient ≤ 0 → skip.**
- **raw + `EventDto`-wrapped** both resolve identically; **unknown / wrapped-unknown → skip.**
- **frame payload cost-free** (reflection guard).
- The hub's server-side resolve + fail-closed abort + the consumer's Worker-gated discovery are structural (mirrors the
  live-verified admin/provider hubs); a **live socket smoke** (connect `/hubs/notification` → publish a test
  `NotificationSentMessage` → frame received) is a manual step needing the running stack + Redis — documented.

### Live smoke steps (manual, env-gated)
1. Deploy the mobile BFF with the Worker flag + the three `Realtime__SignalR__*` env keys.
2. Connect a participant client to `wss://…/hubs/notification` (bearer participant token) — it joins
   `mobile-notification:{ownUserId}` server-side.
3. Publish a `NotificationSentMessage` (Channel=InApp, RecipientUserId = that user) on the bus (or trigger any owner
   notification) → the client receives a `"mobileNotification"` frame with `{ notificationId, type, title,
   referenceType, referenceId, sentAt }` → the bell refetches/deep-links. A Push-channel message must NOT arrive here.

## Next
**MO9c** — owner notification BFF surface: inbox `GetUserNotifications` + mark-read/mark-all + preferences +
`RegisterDeviceToken` (Fcm), owner-scoped, cost-free, + the template/category/default-preference seed for the owner
event set.
