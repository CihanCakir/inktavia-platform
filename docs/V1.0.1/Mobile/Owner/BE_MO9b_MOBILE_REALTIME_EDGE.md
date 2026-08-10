# BE_MO9b — mobile realtime edge (owner live notification bell)

> **Repos:** `addesso-project` (`Bff/src/Marine.Participant.Mobile` only). MO9 **phase b** per `MO9_PLAN.md`: stand up
> the BFF-hosted realtime edge for the owner app so the notification bell updates live. **Model it on the
> `AdminNotification` pair** (per-recipient group, single canonical event) — **not** the heavier provider SR/city
> fan-out. Mobile BFF only; modules already publish. Additive; identity from token; cost-free. **Do not commit.**

## ADR (unchanged) — reuse the framework, don't build a new one
Realtime edge lives on the **BFF**, on `Aizen.Core.Realtime`; modules publish-only. The blessed pieces:
`DomainHubBase` (connection/identity boundary, server-decides-group), `IEventSocketMapper` (one per-surface routing
declaration: bus event → frame + target group), a zero-logic `RealtimeEventConsumer<TMessage, AizenMessageResult>`
closing subclass per consumed bus event (so the messagebus scan hosts it), Redis backplane (mandatory multi-replica).
References in this repo: `AdminNotificationHub` + `AdminNotificationRealtimeConsumer` (**the closest match** — per-user
notification badge) and `ProviderRealtimeHub`/`ProviderEventSocketMapper`/`ProviderRealtimeConsumers` (the fuller
example). MO9b mirrors the **admin-notification** shape.

## Why the single-event (NotificationSentMessage) design — not per-domain fan-out
The Notification module already fans **every** owner notification through **one** canonical bus event,
`NotificationSentMessage` (`Aizen.Modules.Notification.Abstraction.Message`), carrying exactly what a live bell needs:
`RecipientUserId`, `Type`, `Channel`, `Status`, `Title`, `SentAt`, and the deep-link `ReferenceType`/`ReferenceId`.
Its own XML doc says it is "carried on the thin realtime frame so a BFF-hosted notification hub can drive a deep-link
without a second lookup" — i.e. it was designed for exactly this. So the owner bell subscribes to **one** event and
inherits the whole N-A..N-E event set (offers, payment, completion, dispute, change-order, maintenance N2,
price-change) for free — no re-mapping of each domain message, and **no economics** on the wire (Title + type + ref
only). This is the admin-notification approach; MO9b ports it to the mobile BFF.

## Baseline (investigated)
- Mobile BFF is currently **`AppType.Bff` only** — its Program.cs comment flags "realtime/SignalR is a later phase,
  so AppType.Worker is NOT [added]". **MO9b is that later phase.** Redis **cache** is registered, but there is **no
  realtime backplane** yet.
- `ParticipantProfileResolver` + `ParticipantIdentityHolder` already exist (mirror the provider's resolver/holder) —
  they resolve the participant's numeric Identity user id from the verified token (BffAssertion
  `X-Aizen-Provider-Profile-Id` → `KeycloakTokenInfo.ProviderProfileId`). The hub uses these for **server-side** group
  membership.

## BE — the mobile realtime edge (new `Realtime/` folder in `Aizen.Bff.Marine.Participant.Mobile`)
1. **`MobileRealtimeHub : DomainHubBase`** (`[Authorize]`, mobile participant token):
   - `DomainName => "mobile-notification"`; group key `UserGroup(long recipientUserId) => $"mobile-notification:{id}"`
     (the prefix before `:` **must** equal the registered domain key — the socket manager routes by prefix).
   - `OnConnectedAsync`: resolve the participant's numeric Identity user id via `ParticipantProfileResolver` /
     `ParticipantIdentityHolder`; join **only** `UserGroup(userId)`. **Fail closed** (`Context.Abort()`) if it can't
     be resolved — never a shared/"public" group. **No client-callable join/subscribe method.**
2. **`MobileNotificationEventSocketMapper : IEventSocketMapper`** (single BFF mapper, admin-mapper shape):
   - `Resolve(NotificationSentMessage)`: fire **only** when `Channel == InApp` (Push is MO9a's job; the bell is the
     in-app surface) **and** `RecipientUserId > 0`; else return `null` (skip at `Map`, per the framework rule — an
     empty `GetTargets` means "broadcast", not "skip").
   - Frame: `Type = "mobileNotification"`, `Stream`/`Group = MobileRealtimeHub.UserGroup(RecipientUserId)`,
     `AggregateId = NotificationId.ToString()`, `Payload = MobileRealtimeNotification` — a **thin, cost-free** record:
     `{ NotificationId, Type (string), Title, ReferenceType, ReferenceId, SentAt }`. **No amounts / commission / net /
     provider ids** — Title + type + deep-link only. `Unwrap` both the `EventDto`-wrapped and raw forms (like the
     admin/provider mappers).
   - `MobileRealtimeNotification` + a `MobileRealtimeNotificationTypes` constants holder (mirror
     `ProviderRealtimeEvent`).
3. **`MobileNotificationRealtimeConsumer : RealtimeEventConsumer<NotificationSentMessage, AizenMessageResult>`** —
   zero-logic closing subclass (ctor `(IServiceProvider sp) : base(sp)`), so the messagebus scan discovers + hosts it.
4. **Program.cs wiring** (mirror MarineProvider/AdminPanel):
   - Add **`AppType.Worker`** to the `AizenAppInfo` TypeInclude (enables bus consumption — without it
     `AddAizenMessagebus` sets `AddConsumer=false` and the consumer never runs). Keep `AppType.Bff`.
   - `builder.Services.AddAizenRealtime(builder.Configuration, o => o.RegisterModuleMappers = false);`
   - `builder.Services.AddDomainHub<MobileRealtimeHub>("mobile-notification");`
   - `builder.Services.AddSingleton<IEventSocketMapper, MobileNotificationEventSocketMapper>();`
   - `app.MapHub<MobileRealtimeHub>("/hubs/notification");`
   - **Redis backplane REQUIRED** (K8s multi-replica): `Realtime:SignalR:UseRedisBackplane=true` +
     `RedisConnectionString` (a **separate** Redis DB from the cache, so a cache FLUSHDB never drops sockets) — set in
     compose/k8s env exactly as MarineProvider does. Document the env keys.
5. **Project reference:** add `Aizen.Core.Realtime` (+ `.Abstraction`) and the Notification `.Abstraction` (for
   `NotificationSentMessage`) to the mobile BFF, if not already referenced.

## Don't-break / QA
- Additive: a new `Realtime/` folder + the Worker flag + realtime registrations. **No module changes** (modules
  already publish `NotificationSentMessage`); no existing BFF endpoint touched. Enabling `AppType.Worker` means the
  BFF now consumes the bus — confirm it only hosts the **realtime** consumer (no unintended command consumers pulled
  in) and that existing HTTP passthroughs are unaffected.
- **Cost-free by construction:** grep the frame payload — no amount/commission/net/margin/provider-id fields; Title +
  type + deep-link ref only.
- **Security:** group decided server-side from the resolved participant id; fail-closed on unresolved; no
  client-callable join. A participant can only ever join their own `mobile-notification:{ownId}` group.
- Tests / verification: (1) mobile BFF + solution build 0 errors; (2) mapper unit tests — `NotificationSentMessage`
  (InApp, recipient>0) → correct group + thin cost-free frame; **Push channel → skip**; recipient≤0 → skip;
  `EventDto`-wrapped + raw both resolve; (3) hub resolves the participant id and joins only their group, aborts when
  unresolved (by construction / a focused test if the harness allows); (4) the consumer is discovered/hosted (Worker
  enabled). Live socket smoke (connect → publish a test notification → frame received) is a manual step (needs the
  stack + Redis); document it.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO9b_REALTIME.md`: the `MobileRealtimeHub` (per-recipient group, server-side
resolve, fail-closed), the single `MobileNotificationEventSocketMapper` (NotificationSentMessage → InApp-only,
cost-free thin frame), the closing consumer, the Program.cs wiring (AppType.Worker + AddAizenRealtime + AddDomainHub +
MapHub `/hubs/notification` + Redis backplane env keys), and the tests. Then **MO9c** (owner notification BFF surface:
inbox `GetUserNotifications` + mark-read/mark-all + preferences + `RegisterDeviceToken` Fcm, owner-scoped, cost-free,
+ template/category/default-preference seed for the owner event set).
