# REPORT — BE_MO9c owner notification BFF surface (inbox + read + preferences + device-token)

> MO9 **phase c**: expose the existing Notification module surface to the owner app through the mobile BFF — inbox,
> mark-read/mark-all, preferences, and FCM device-token registration (feeds MO9a) — all passthroughs over queries/
> commands that already exist and are already identity-correct. Plus a small Notification-module template gap-fill.
> Mobile BFF + a duplicate-safe seed. Additive; identity from token; cost-free. **NOT committed.**
>
> Repos: `addesso-project` (`Bff/src/Marine.Participant.Mobile` + a Notification seed edit).

## Outcome
- **Builds clean** — the mobile BFF host + the Notification Repository: 0 errors.
- **Tests: 33 green** — mobile BFF `…UnitTests` (12: 8 MO9b + 4 new MO9c mapper) + Notification `Application.UnitTests`
  (21: 15 MO9a + 6 new MO9c seed).
- **No module endpoint/handler changed.** The existing `NotificationsController` (used by web/admin) is untouched;
  WebPush endpoints are deliberately not exposed to mobile.

---

## Identity is already correct — no owner-gate needed
Every module handler resolves the recipient via `ResolveEffectiveRecipientId()` = `KeycloakTokenInfo.ProviderProfileId`
(the BffAssertion the mobile BFF sends as `X-Aizen-Provider-Profile-Id`) → else `UserInfo.UserId`. So the inbox /
device-token / preferences scope to the caller's participant automatically — a body-supplied recipient is impossible
(no such field), and there is no by-id owner-gate to add (unlike the SR endpoints). The mobile BFF handlers simply
resolve the participant (so the assertion is populated) and forward.

## BE — mobile BFF passthroughs (new, thin; reuse the module verbatim)
- **`INotificationRemoteCall`** (Refit) → `api/v1/notification/notifications`: `GetUserNotifications(skip, take)`,
  `MarkAsRead(id)`, `MarkAllAsRead()`, `RegisterDeviceToken(body)`, `GetPreferences()`, `UpdatePreference(body)` —
  typed concrete module DTOs (never `object`/`JsonElement`). DI-registered; base URL
  `RemoteCalls:INotificationRemoteCall:BaseUrl` (→ notification-api). The WebPush trio is not included.
- **Cost-free mobile DTOs + mapper** (`Contracts/Notification/` + `Notification/MobileNotificationMapper.cs`):
  - `MobileNotificationDto` `{ Id, Type (string), Title, Body, ReferenceType, ReferenceId, IsRead, CreatedAt, ReadAt }`
    — **`MetadataJson` dropped** (it carries only internal transaction/context ids, no amounts), and `Channel`/`Status`
    dropped. `NotificationType`/`Channel`/`Status` cross as **enum names** (the AdminPanel numeric-enum gotcha — the
    client gets stable strings). The money in a Payment notification rides the human `Body` text (unavoidable — it's
    the message); there is no numeric amount field on the wire.
  - `MobileNotificationListDto { Items, Total, UnreadCount }` (the badge).
  - `MobileNotificationPreferencesDto` — the per-category × channel matrix passed through (already cost-free:
    category + enabled/locked, no economics).
- **Handlers + `MobileNotificationController`** (`api/v1/mobile/notifications`, `[Authorize(ParticipantAuthenticated)]`,
  callable by the SA via BffAssertion): `GET` (inbox, skip/take), `PATCH {id}/read`, `POST mark-all-read`,
  `POST device-token` (**`Platform` forced to `Fcm`** server-side — the client sends only the token), `GET preferences`,
  `PUT preferences`. Owner identity flows from the token; never the body.

## Notification module — owner event-set template gap-fill (small, additive, duplicate-safe)
A `NotificationType` with **no template is a silent no-op**. Audited the owner event set against
`NotificationTemplateSeed.BuildTemplates()`: 12/16 already had a template; **4 were missing** and are now added
(InApp, tr placeholders matching each emitting consumer's Variables):
| Type | TemplateCode | Placeholder keys |
|---|---|---|
| `OfferRejected` (112) | `SR_OFFER_REJECTED_INAPP` | `serviceRequestId` |
| `CompletionRejected` (132) | `SR_COMPLETION_REJECTED_INAPP` | `serviceRequestId` |
| `PaymentCaptured` (151) | `PAYMENT_CAPTURED_INAPP` | `amount`, `currency`, `transactionCode` |
| `PaymentRefunded` (153) | `PAYMENT_REFUNDED_INAPP` | `refundedAmount`, `currency`, `transactionCode` |

`PaymentCaptured`/`PaymentRefunded` already had consumers (`PaymentCapturedConsumer`/`PaymentRefundedConsumer`) but no
InApp template → they were silent; now they render. The category map (100–199 + 200) and the default-preference policy
already cover the owner set — no change there. The seed dedupes by `TemplateCode`, so re-running is a no-op; existing
templates are untouched.

---

## Config / runtime prerequisites (env-gated)
- `RemoteCalls__INotificationRemoteCall__BaseUrl: http://notification-api:8080` added to the `bff-marine-mobile`
  compose service (also back-filled the MO7 `RemoteCalls__IParticipantMembershipRemoteCall__BaseUrl`, which was
  missing from compose).
- **`notification-api` now trusts the mobile BFF assertion** — added `BffAssertion__AllowedClientIds__2:
  marine-mobile-bff` to the notification-api compose service (it previously listed only provider/admin, so the mobile
  BFF's assertion would have been ignored → recipient unresolved). Mirrors the other module APIs.
- As with every MO task, the mobile BFF service-account token must carry the `notification-api` audience for the S2S
  call (env-gated; same posture as payment-api in MO3/MO7). For K8s, set the same three keys.

---

## Tests
**Mobile BFF (`MobileNotificationMapperMo9cTests`, 4):** inbox maps to the cost-free DTO (enum → string name,
MetadataJson/Channel/Status dropped); list paging (Total/UnreadCount) honoured; preferences matrix passes through
(locked flags preserved); a cost-free reflection guard on `MobileNotificationDto` (no amount/commission/net/margin/
metadata/provider member).
**Notification seed (`OwnerEventSetSeedMo9cTests`, 6):** the 4 gap-filled types now have an InApp template (theory);
the whole owner event set is covered; TemplateCodes are unique (re-seed is a no-op).

### How the remaining acceptance criteria are enforced
- **(3) mark-read / mark-all reach the module** and **(4) device-token forces Fcm + registers under the token's
  recipient** and **(5) preferences GET/PUT round-trip** — thin passthrough handlers over the identity-correct module
  commands (build-verified); the module resolves the recipient from the assertion, and the handler hard-codes
  `Platform = Fcm`. A **live inbox smoke** (a real notification appears + marks read; a token registers; a locked
  cell is rejected by the module) needs the running stack — documented below.

### Live smoke (manual, env-gated)
Deploy notification-api (with `marine-mobile-bff` allowlisted) + the mobile BFF (with the notification base URL) →
`POST /api/v1/mobile/notifications/device-token { deviceToken }` (registers Fcm under the participant) → trigger an
owner notification → `GET /api/v1/mobile/notifications` shows it (Type as a string name, no MetadataJson) with
`UnreadCount` → `PATCH {id}/read` flips it → `GET/PUT preferences` round-trips (a locked InApp/Account cell is rejected
by the module).

## Don't-break / QA
Additive: a new remote-call + DTOs/mapper + one BFF controller + a duplicate-safe template gap-fill. No module
endpoint/handler changed; the admin/web `NotificationsController` is untouched; WebPush not exposed to mobile.
Cost-free: no amount/commission/net/margin field on the mobile payloads (MetadataJson dropped). Identity from token;
`Platform` forced to Fcm.

## Next
**MO9d** — FE Expo: notification center + push permission / FCM-token registration + the live bell on
`/hubs/notification` (MO9b) + a preferences screen — the owner track's close-out.
