# BE_NF1 — notification correctness layer (fire to the right party, right event) — the two headline bugs

> **Repo:** `addesso-project` (Notification module; + a small additive SR event field). Fix the two bugs the user
> reported, per the **N-F0 live diagnosis** (`REPORT_SMOKE_NF0.md`): (1) a new SR gives the provider **no push** because
> the notification fan-out is bound to a **never-published event**; (2) an offer gives the **owner nothing**. Also the
> inbox dedup filter (D4) + a latent lifecycle mis-key (D5) — both needed before N-F2 adds email/FCM. **In-app + web
> push only** here; email + FCM are **N-F2**. Additive. **Do not commit.**

## Diagnosis (confirmed live — N-F0)
- **D1:** `ServiceRequestCreatedConsumer` (owner confirmation **+** region fan-out) binds `ServiceRequestCreatedMessage`
  — **which nothing ever publishes.** The create/publish flow publishes **`ServiceRequestPublishedMessage`**
  (`PublishServiceRequestCommandHandler.cs:62`), whose only consumer is the MarineProvider realtime edge. So the whole
  consumer is **dead** → 0 notifications, 0 web push. (Live: SR 59 published → 0 rows; 0 Type-102 in the DB.)
  `ServiceRequestPublishedMessage` **already carries** `ServiceRequestId, RequestCode, Title, OwnerUserId,
  ServiceCategoryCode, LocationCityCode`, and its own XML doc says *"This — not ServiceRequestCreatedMessage — is the
  event providers care about."* Secondary blocker: **0 active WebPush subscriptions** (providers haven't granted
  browser push) — a client/ops matter; the fan-out must fire regardless (in-app lands; push lands once subscribed).
- **D2:** `ServiceRequestOfferCreatedConsumer` files only to `ProviderProfileId`; the **owner gets nothing**. The
  message carries **no `OwnerUserId`** → resolve it.
- **D4:** the inbox `GetByRecipientAsync` filters by `RecipientUserId` only — **no channel filter** → once N-F2 adds
  Email/Push rows, the list would show **duplicates**. Masked today (all 193 rows are InApp).
- **D5:** `ServiceRequestCompletionApprovedConsumer` keys by **raw `ProviderUserId`** while OfferCreated/AssignmentCreated
  key by **`ProviderProfileId`** — a **mis-key** masked only because provider2's `UserId == ProfileId`. Latent bug.

## BE — N-F1

### 1. D1 — move the region fan-out to the event that actually fires (`ServiceRequestPublished`)
- **Add a Notification consumer for `ServiceRequestPublishedMessage`** that does the region fan-out:
  `GetProvidersForArea(LocationCityCode, ServiceCategoryCode)` → one `ServiceRequestAreaOpportunity` (Channel=InApp) per
  provider (keyed by **`ProviderProfileId`**), **excluding the owner**, **sequentially** (WS2 discipline). This is the
  exact logic currently stranded in `ServiceRequestCreatedConsumer.FanOutToAreaProvidersAsync` — move it. In-app row +
  web push (via the existing `NotificationSentMessage` → `NotificationSentPushConsumer`) now actually fire.
- **Owner-facing:** on publish, optionally notify the owner *"your request is now live / open for offers"* (Channel=
  InApp). (The dead "created" confirmation is superseded by a meaningful "published" one.)
- **Retire the dead `ServiceRequestCreatedConsumer`** (nothing publishes its event) — or repoint it; grep-confirm no
  other publisher first (the N-F0 report says none). Keep `ServiceRequestCreatedMessage` the type only if something
  else uses it.
- **Note (not N-F1 code):** the 0-WebPush-subscriptions is why push still won't land until providers grant browser
  push — surface it (provider-web must prompt/register a WebPush subscription; ops/QA item). The backend fan-out is the
  fix; the subscription is the client prerequisite.

### 2. D2 — offer → owner
- **Add `OwnerUserId` to `ServiceRequestOfferCreatedMessage`** (additive; the publisher — `CreateServiceRequestOffer` /
  the offer flow — has the SR, so it has the owner). Populate it.
- In `ServiceRequestOfferCreatedConsumer`, **also notify the owner**: `RecipientUserId = OwnerUserId`, a new/owner-facing
  `OfferCreated`-owner notification ("you received an offer", `ReferenceType=ServiceRequest`/offer, → offer detail),
  Channel=InApp. **Keep** the provider confirmation. Ensure a template exists for the owner-facing message (silent-no-op
  guard).

### 3. D4 — inbox shows one logical notification (filter to InApp)
- `GetByRecipientAsync` (the inbox read behind `GetUserNotifications`): **filter to `Channel == InApp`** so the list
  shows the canonical logical notification. Email + web/FCM push are **delivery mechanisms**, not list rows. (Required
  before N-F2 adds Email/Push rows; harmless today since all rows are InApp.)

### 4. D5 — lifecycle mis-key fix
- `ServiceRequestCompletionApprovedConsumer`: key the notification by **`ProviderProfileId`** (consistent with the other
  consumers), not the raw `ProviderUserId`. Confirm the message carries the profile id (add it if absent, additive).
  Prevents a silent mis-file for any provider where `UserId != ProfileId`.

## Don't-break / QA
- Additive: a new `ServiceRequestPublished` notification consumer (fan-out moved, not duplicated) + the dead
  `ServiceRequestCreated` consumer retired + `OwnerUserId`/profile-id fields (additive) + the inbox InApp filter + the
  D5 key fix. In-app + web-push paths unchanged in mechanism; email/FCM are N-F2. Preference-gating + WS2 sequential
  discipline preserved.
- Tests: (1) Notification + SR + solution build 0 errors; (2) **publish an SR in a provider's city/category → the
  provider gets a `ServiceRequestAreaOpportunity` in-app row** (+ web push if subscribed); the owner is not
  double-notified; a no-city SR skips fan-out; (3) **an offer → the owner gets an in-app notification** (+ the provider
  confirmation still fires); (4) the inbox returns InApp rows only (send a test Email row → not in the list); (5)
  CompletionApproved files under ProviderProfileId; (6) the retired ServiceRequestCreated consumer has no publisher.
  Live smoke: publish an SR (provider sees in-app + push if subscribed) + make an offer (owner sees in-app) — on the
  stack, reusing SR 59 / a fresh SR.

## Report
`docs/V1.0.1/Notification/REPORT_BE_NF1.md`: the fan-out rebind to `ServiceRequestPublished` (D1), offer→owner + the
`OwnerUserId` field (D2), the inbox InApp filter (D4), the CompletionApproved key fix (D5), the retired dead consumer,
and the live proof that a published SR now notifies area providers and an offer now notifies the owner. Flag the
0-WebPush-subscriptions client prerequisite. Then **N-F2** (multi-channel: Email sends + templates, and FCM/APNs
dispatch on the InApp push path so the owner's Firebase push fires) → **N-F3** (lifecycle parity + deeplinks).
