# REPORT — SMOKE_NF0 notification multi-channel diagnosis

> Read-only diagnosis against the running stack + code. Pinpoints the **actual live root cause** of each symptom
> (D1–D5) so N-F1/N-F2/N-F3 target reality. **No code changed, nothing committed.**

## Environment facts (confirmed live)
- `NotificationChannel`: InApp=1, Push=2, Email=3, Sms=4. `PushPlatform`: Fcm=1, Apns=2, WebPush=3.
- VAPID **is configured** on notification-api (`Vapid__PublicKey/PrivateKey/Subject` all set) — web push is NOT skipped for lack of keys.
- **All 193 notification rows in the DB are `Channel=InApp`** — **zero Push/Email/Sms rows exist.**
- **Device tokens: 1 total, and it is INACTIVE** — `(UserId 100012, Platform=WebPush, IsActive=false)`. **No active WebPush and no Fcm token exists for any provider or owner.**

---

## D1 — Provider web push on a new SR: WHY it doesn't land
**Root cause (primary, live-confirmed): the area fan-out never runs — the consumer listens to a message that is never published.**

- `ServiceRequestCreatedConsumer` (`Modules/Notification/.../Consumers/ServiceRequest/ServiceRequestCreatedConsumer.cs`) binds
  `AizenBaseMessageConsumer<ServiceRequestCreatedMessage>` and does both the owner "created" notification and
  `FanOutToAreaProvidersAsync` (→ `GetProvidersForArea` → one `ServiceRequestAreaOpportunity` InApp per provider).
- **`ServiceRequestCreatedMessage` is never published** — a repo-wide grep for `new ServiceRequestCreatedMessage`
  returns **nothing**. The SR create/publish flow instead publishes **`ServiceRequestPublishedMessage`**
  (`PublishServiceRequestCommandHandler.cs:62`), whose **only consumer is the MarineProvider realtime edge** — there is
  **no notification consumer for it**.
- **Live proof:** created SR 59 (`electrical`, city `35`, published — `PublishedAt` set). Result: **no fan-out log line,
  no owner `ServiceRequestCreated` row, no provider `ServiceRequestAreaOpportunity` row** (Type 102). Confirmed by the DB:
  **zero Type-101 (StatusChanged) and zero Type-102 (AreaOpportunity) rows exist in the entire system.**
- **This overturns the doc's hypothesis** (fan-out returns N=0 because providers lack a category row). In fact the
  eligibility read **would** return provider2: `GetProvidersForArea(city, category)` filters `UserProfiles`
  (RoleContext=Organizer(3), ApprovalStatus=Approved(1), Status=Active(2), `City == cityCode`) `EXISTS` a
  `provider_service_categories` row, and provider2 (profile 100011) has `City=35` + an `electrical` category row — it
  **matches**. So the fan-out is not empty; it simply **never fires** because of the wrong message type.
- **Secondary cause (would still block web push even if the message were rewired):** **no active WebPush subscription.**
  The only device token is inactive and belongs to a non-provider; provider2 has **zero** `UserDeviceTokenEntity` rows.
  `NotificationSentPushConsumer` filters to `Platform==WebPush` active tokens with VAPID — with none, nothing sends.
- **Not the cause:** VAPID (configured); the category `EXISTS` join (provider2 has the row); the provider preference
  (moot — no notification is created to gate).

→ **Order of fixes for D1:** (1) wire the fan-out to the **published** event (`ServiceRequestPublishedMessage`) — or
publish `ServiceRequestCreatedMessage` — so the consumer runs; (2) ensure providers register an **active WebPush**
subscription (0 exist today). Only then does web push land.

## D2 — Offer → owner (the missing notification): CONFIRMED gap
- `ServiceRequestOfferCreatedConsumer` files the row to **`RecipientUserId = message.ProviderProfileId`** (the provider),
  `Type=OfferCreated(110)`, `Channel=InApp`. **The owner is never notified.**
- **Live proof:** the two `Type=110` rows (Ids 161/162, SR 55/56) went to **RecipientUserId=100011 (provider2's
  profile)**; **owner 100029 has zero Type-110 rows** (only 111/140/200).
- **Message shape gap (blocks N-F1):** `ServiceRequestOfferCreatedMessage` carries only
  `ServiceRequestId, OfferId, ProviderProfileId, ProviderUserId, TotalAmount, CurrencyCode, Status` — **no `OwnerUserId`.**
  To notify the owner, N-F1 must do a cross-module **SR→owner lookup** keyed on `ServiceRequestId` (the only handle).

## D3 — FCM push path (owner mobile Firebase): never invoked
- FCM fires **only** via `PushNotificationDispatcher` → `FcmSender`, which runs **only for a `Channel=Push` notification**
  (routed by `CompositeNotificationDispatcher`). `IFcmSender` is referenced in exactly two places (the dispatcher + the
  sender) — it is the sole FCM path.
- **No SR/offer/lifecycle consumer creates a `Channel=Push` notification** — every one hard-codes `Channel=InApp`
  (grep of `Consumers/ServiceRequest/*` = InApp only; the only `Channel=Push` creators are the Identity provider-
  approval + CargoDry milestone consumers). **Live proof:** 0 rows with `Channel=2` in the DB.
- So the owner's Firebase push **never fires** for SR/offer/lifecycle events. The only "push" these produce is the VAPID
  **WebPush** piggybacked on the InApp `NotificationSentMessage` (`Channel=InApp` only publishes that message).
- Additionally, the owner has **no `Platform=Fcm` device token** (0 Fcm tokens system-wide) — so even a `Channel=Push`
  notification would find no token. → N-F2 must (a) create a `Channel=Push` notification for these events, and (b) the
  owner mobile app must register an Fcm token.

## D4 — Inbox list dedup (1 notification, N channels): NO channel filter
- `GetUserNotifications` → `NotificationRepository.GetByRecipientAsync` filters **only** by `RecipientUserId`
  (`WHERE x.RecipientUserId == userId`, ordered by unread/CreatedAt/Id). **There is no `Channel` filter**, and the DTO
  copies `Channel` through — so **all channels are returned**.
- **Consequence:** if an event produced both an `InApp` and an `Email`/`Push` row for the same recipient, the inbox list
  returns **both** — a duplicate second row. Nothing dedupes by `(Type, ReferenceId)`. (Contrast the mobile mapper
  `MobileNotificationEventSocketMapper.cs:59` which DOES gate `Channel != InApp → skip` — the module inbox query has no
  such guard.)
- **Live state:** no duplication today only because **every** row is InApp (0 non-InApp rows). The moment N-F1 adds a
  second channel per event, the inbox dups. → **N-F1 must filter the inbox query to `Channel==InApp`** (the canonical
  row) or dedupe by `(Type, ReferenceId)`.

## D5 — Lifecycle recipients (parity baseline), live-confirmed
All consumers create `Channel=InApp` only. Recipient per consumer (verified against live rows):

| Consumer | Message | Recipient | Type | Live rows |
|----------|---------|-----------|------|-----------|
| `ServiceRequestOfferCreatedConsumer` | `ServiceRequestOfferCreatedMessage` | **provider** `ProviderProfileId` | OfferCreated(110) | 161/162 → 100011 ✅ |
| `ServiceRequestOfferAcceptedConsumer` | `ServiceRequestOfferAcceptedMessage` | **owner** `OwnerUserId` | OfferAccepted(111) | 191/194/196 → 100029 ✅ |
| `ServiceRequestAssignmentCreatedConsumer` | `ServiceRequestAssignmentCreatedMessage` | **provider** `ProviderProfileId` | AssignmentCreated(120) | 192/195 → 11012 ✅ |
| `ServiceRequestStatusChangedConsumer` | `ServiceRequestStatusChangedMessage` | **actor** `ActorUserId ?? skip` | StatusChanged(101) | **0 rows** (skips when ActorUserId null) |
| `ServiceRequestCompletionApprovedConsumer` | `ServiceRequestCompletionApprovedMessage` | **provider `ProviderUserId`** (raw) | CompletionApproved(131) | 166 → 100011 |

**Parity issues for N-F3:**
- **Recipient-key inconsistency:** OfferCreated + AssignmentCreated key by **`ProviderProfileId`**; CompletionApproved
  keys by **`ProviderUserId`** (its message has no ProfileId field). If provider notifications/tokens/prefs are keyed by
  ProfileId, CompletionApproved is **mis-filed** (invisible inbox row / no push). It only "looks fine" in the live rows
  because provider2's UserId == ProfileId (both 100011) — the bug is masked. SR 30009's provider (profile 11012 / user
  10012) would expose it.
- **StatusChanged** notifies the **actor** (often the person who caused the change) and **skips entirely when
  `ActorUserId` is null** — hence 0 rows; N-F3 should decide the intended recipient (owner and/or provider).
- **All InApp** — no channel adds Push/Email; N-F3 adds the missing channels.

---

## Summary — what drives each fix
- **N-F1 (offer→owner + multi-channel core):** offer message has **no OwnerUserId** → SR→owner cross-lookup on
  `ServiceRequestId`; the inbox query has **no channel filter** → filter to `Channel==InApp` before multi-channel is
  added (else the inbox dups).
- **N-F2 (FCM):** SR/offer/lifecycle consumers create **InApp only** → no `Channel=Push` → FCM never fires; add a
  `Channel=Push` notification (+ the owner must register an Fcm token).
- **N-F3 (lifecycle parity + deeplinks):** fix the CompletionApproved **ProviderUserId-vs-ProfileId** mis-key and the
  StatusChanged actor-only/skip behavior; add missing channels.
- **D1 headline (the single actual reason provider web push doesn't land):** the area-opportunity fan-out **never runs**
  because `ServiceRequestCreatedConsumer` subscribes to `ServiceRequestCreatedMessage`, which **no code publishes** (the
  real event is `ServiceRequestPublishedMessage`, unconsumed by notifications) — and even if wired, **no provider has an
  active WebPush subscription** (0 device tokens).

*(Test artifact left on the dev stack: SR 59 — a published `electrical`/city-35 SR used for the D1 probe.)*
