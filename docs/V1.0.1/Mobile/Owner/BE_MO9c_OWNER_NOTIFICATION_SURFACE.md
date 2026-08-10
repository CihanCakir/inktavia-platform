# BE_MO9c — owner notification BFF surface (inbox + read + preferences + device-token)

> **Repos:** `addesso-project` (`Bff/src/Marine.Participant.Mobile` + a small Notification-module **seed** gap-fill).
> MO9 **phase c** per `MO9_PLAN.md`: expose the existing Notification module surface to the owner app through the
> mobile BFF — inbox, mark-read/mark-all, preferences, and **FCM device-token registration** (feeds MO9a). All
> passthroughs over queries/commands that already exist and are already identity-correct. Additive; identity from
> token; cost-free. **Do not commit.**

## Baseline (investigated — the module surface is already there and BffAssertion-aware)
`NotificationsController` at **`api/v1/notification/notifications`** (`[Authorize]`) already exposes everything:
- `GET` → `GetUserNotifications` (`Skip`/`Take`, default 20) → `NotificationListResponse { Items: NotificationDto[],
  Total, UnreadCount }`.
- `PATCH {id:long}/read` → `MarkNotificationAsRead`.
- `POST mark-all-read` → `BulkMarkAsRead`.
- `POST device-token` → `RegisterDeviceToken` (`{ DeviceToken, Platform }`, `PushPlatform` enum → **Fcm** for the
  Expo client).
- `GET preferences` / `PUT preferences` → `GetNotificationPreferences` / `UpdateNotificationPreference`
  (`NotificationPreferencesResponse` matrix: per-category `InApp`/`Push`/`Email` `{ Enabled, Locked }`; update takes
  `{ Category, Channel, Enabled }` — only Push/Email user-changeable, InApp + Account locked).
- `POST push-subscriptions` / `GET vapid-public-key` → WebPush (browser only — **the mobile app does not use these**).

**Identity is already correct for the mobile BFF.** Every handler resolves the recipient the same way MO7's
membership controller does: `KeycloakTokenInfo.ProviderProfileId` (the BffAssertion the mobile BFF sends) → else
`UserInfo.UserId` (verified `RegisterDeviceTokenCommandHandler` + `GetUserNotificationsQueryHandler`
`ResolveEffectiveRecipientId()`). So these endpoints are callable by the mobile-bff SA via BffAssertion and scope to
the token's participant **without any module change** — no owner-gate to add here (unlike the SR by-id endpoints).

## BE — mobile BFF passthroughs (new, thin; reuse the module verbatim)
1. **`INotificationRemoteCall`** (Refit) → the module's `api/v1/notification/notifications`: `GetUserNotifications`
   (skip/take), `MarkAsRead(id)`, `MarkAllAsRead`, `RegisterDeviceToken(body)`, `GetPreferences`,
   `UpdatePreference(body)`. Typed concrete DTOs (never `object`/`JsonElement` — the BFF typed-body rule). DI + config
   (`notification-api` base url + `depends_on`); add the Notification audience to the mobile-bff BffAssertion
   allowlist / audience mapper if a new one is needed (mirror the SR wiring from MO1).
2. **Cost-free mobile DTOs + mappers:**
   - `MobileNotificationDto` — `{ Id, Type (string), Title, Body, ReferenceType, ReferenceId, IsRead, CreatedAt,
     ReadAt }`. **Drop `MetadataJson`** unless it is confirmed to carry no economics; if any owner notification's
     metadata includes amounts/commission, strip those keys (cost-free discipline — the owner inbox shows the human
     message + a deep-link, never funding internals). Map `NotificationType`/`Channel`/`Status` **enum → string**
     (the AdminPanel-BFF numeric-enum gotcha; the mobile client gets stable names).
   - `MobileNotificationListDto` — `{ Items, Total, UnreadCount }`.
   - `MobileNotificationPreferencesDto` — pass the category matrix through (already cost-free: category + channel +
     enabled/locked, no economics).
3. **Handlers + `MobileNotificationController`** (`api/v1/mobile/notifications`, `[Authorize]` participant, callable
   by the SA via BffAssertion): `GET` (inbox, skip/take), `PATCH {id}/read`, `POST mark-all-read`, `POST device-token`
   (**force `Platform = Fcm`** server-side; the client only sends the token), `GET preferences`, `PUT preferences`.
   Owner identity flows from the token via BffAssertion — never from the body.

## Notification module — owner event-set seed gap-fill (small, additive)
The platform rule: **a `NotificationType` with no template is a silent no-op.** Confirm every **owner-relevant** type
has a template (tr+en), a category mapping, and a sane default preference, so the owner actually receives them. The
owner event set (from `NotificationType`): `OfferCreated`(110), `OfferAccepted`(111), `OfferRejected`(112),
`CompletionSubmitted`(130)/`CompletionApproved`(131)/`CompletionRejected`(132),
`CompletionAutoApproveApproaching`(133), `DisputeOpened`(140)/`DisputeResolved`(141), `PaymentCaptured`(151)/
`PaymentRefunded`(153)/`PaymentReleased`(150)/`PaymentAuthorized`(157), `MaintenanceReminderDue`(103),
`SubscriptionPriceChangeUpcoming`(160), `NewMessageReceived`(200). Most were seeded in N-A..N-E — **verify** and
**only add the missing ones** (duplicate-seed-safe, per the seed convention). Do not change existing templates.

## Don't-break / QA
- Additive: a new remote-call + DTOs/mappers + one BFF controller, plus a duplicate-safe template gap-fill. **No
  module endpoint/handler changed**; the existing `NotificationsController` (used by web/admin) is untouched. WebPush
  endpoints are deliberately not exposed to mobile.
- **Cost-free:** grep the mobile notification payloads — no amount/commission/net/margin fields; Title + Body + type +
  deep-link ref only (and MetadataJson dropped/scrubbed).
- **Identity from token:** device-token + inbox + preferences all scope to the token's participant (module resolves
  ProviderProfileId → recipient); a body-supplied recipient is impossible (no such field). `Platform` forced to Fcm.
- Tests: (1) mobile BFF + solution build 0 errors; (2) inbox passthrough maps to the cost-free DTO (enums as strings,
  MetadataJson dropped/scrubbed), paging honoured; (3) mark-read / mark-all reach the module; (4) device-token forces
  Fcm and registers under the token's recipient; (5) preferences GET/PUT round-trip (locked cells rejected by the
  module); (6) cost-free reflection/grep guard on the mobile DTOs; (7) the owner event-set seed is present + duplicate
  runs are no-ops. Live inbox smoke (a real notification appears + marks read) needs the stack — document it.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO9c_NOTIFICATION_SURFACE.md`: the `INotificationRemoteCall` + cost-free mobile
DTOs + `MobileNotificationController` (inbox/read/mark-all/device-token-Fcm/preferences), the identity-from-token
confirmation (module resolves ProviderProfileId → recipient, no owner-gate needed), the owner event-set template
gap-fill, and the tests. Then **MO9d** (FE Expo: notification center + push permission/FCM-token registration + live
bell on `/hubs/notification` + preferences screen — the owner track's close-out).
