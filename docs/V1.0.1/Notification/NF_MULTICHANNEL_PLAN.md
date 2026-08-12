# N-F — SR/offer lifecycle multi-channel notifications (web push + FCM + email) — PLAN

> **User report:** (1) a **new service request** arrives **live** to the provider but **no web push**; (2) when a
> provider **makes an offer**, the **owner gets no notification**. Requirement: provider gets **web push + email** on a
> new SR **in their city/category** (region-targeted, deeplink → offer/request detail); owner gets **email + Firebase
> push** when they receive an offer; the **notification list shows ONE logical notification** even though 2+ channels
> fire; and `OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED` follow the same per-party, parallel, **preference-gated**
> pattern. **Do not commit.**

## Confirmed architecture (investigated)
`SendNotificationCommand` is **per-channel**: one `(Type, Channel)` template → one `NotificationEntity` (no template
for that pair ⇒ silent no-op). Dispatch is **channel-keyed** (`INotificationDispatcher` keyed by `NotificationChannel`):
- **`Channel = InApp`** → in-app row **+** publishes `NotificationSentMessage` → **`NotificationSentPushConsumer`**
  dispatches **WEB PUSH ONLY** (`Platform == WebPush`), N-B Push-gated. (Also drives the live badge.)
- **`Channel = Push`** → `PushNotificationDispatcher` → **FCM/APNs** via the MO9a `FcmSender`.
- **`Channel = Email`** → `EmailNotificationDispatcher` → `SmtpEmailSender` (or the stub), N-B Email-gated.

So **one InApp notification yields in-app + web push** — but **not FCM and not email**.

## Confirmed gaps (root causes)
1. **Offer → owner MISSING.** `ServiceRequestOfferCreatedConsumer` sends to `RecipientUserId = ProviderProfileId`
   (the offering provider's confirmation), `Channel = InApp`. **The owner is never notified.** (Bug #2.)
2. **FCM push never fires** for these events. FCM only dispatches for a `Channel = Push` notification, but every SR/
   offer/lifecycle consumer creates `Channel = InApp` only, and the InApp→push path is **WebPush-only**. So the owner's
   **Firebase mobile push never fires**.
3. **Email never fires** for these events — no consumer sends a `Channel = Email` notification.
4. **Provider web push not landing on a new SR** — the region fan-out **exists** (`ServiceRequestCreatedConsumer.
   FanOutToAreaProvidersAsync` → `GetProvidersForArea(cityCode, category)` → one `ServiceRequestAreaOpportunity`
   (`InApp`) per provider, which SHOULD trigger web push). So the miss is downstream: **(a)** `GetProvidersForArea`
   returns 0 (providers only have a City, no service-area — the GeoDiscovery-deferred issue), **(b)** the provider has
   **no active WebPush subscription**, or **(c)** N-B Push preference is off. → **live diagnosis.**

## Live-diagnosis items (N-F0)
- **Web-push-not-landing:** for a real new SR in a provider's city, does `GetProvidersForArea` return the provider?
  does that provider have an active `UserDeviceTokenEntity` (Platform=WebPush)? is N-B Push enabled? — pinpoint which.
- **List dedup:** does `GetUserNotifications` (inbox) return **InApp-channel rows only**, or all channels? This decides
  whether "1 notification, N channels" needs an inbox filter or already holds (email/push as delivery-only).
- **Recipient-id keying:** confirm provider notifications key on `ProviderProfileId` (inbox/token/pref) and owner on
  the owner user id — consistently across the new owner path.

## Phased plan

### N-F0 — diagnosis (live, no behaviour change)
Confirm the 3 items above against the running stack + a real SR/offer. Output pinpoints each root cause so N-F1/N-F2
fix the real thing, not a guess. Kickoff `SMOKE_NF0_NOTIFICATION_DIAGNOSIS.md`.

### N-F1 — multi-channel core + offer→owner (the main fix)
- **Add the owner notification on offer-created** (the missing bug): notify the **owner** ("you received an offer",
  ReferenceType/Id → offer detail) — the offer event must carry/resolve the owner id (add `OwnerUserId` to
  `ServiceRequestOfferCreatedMessage` if absent, or resolve it).
- **A multi-channel send** so a consumer emits **one logical notification** that fans to **InApp (the list row) +
  web/FCM push + email**, each **N-B preference-gated**, without hand-rolling 3 calls. Options: (a) a
  `SendNotificationMultiChannel` that creates the InApp row (canonical/list) + issues Email + ensures push, or (b) keep
  per-channel calls but add a small helper. The **inbox shows the InApp row only** (1 logical notification); email +
  push are deliveries. Preserve the WS2 exactly-once / sequential-loop discipline.
- Wire it for **SR-created → region providers** (in-app + web push + email) and **offer-created → owner** (in-app +
  FCM + email). Preference-gated throughout.
- Kickoff `BE_NF1_MULTICHANNEL_OFFER_TO_OWNER.md`.

### N-F2 — FCM/APNs dispatch on the InApp push path (owner mobile push)
Extend the InApp→push path so it dispatches **FCM/APNs** tokens too, not just WebPush — either broaden
`NotificationSentPushConsumer` (or add a sibling `NotificationSentFcmConsumer`) to send to `Platform ∈ {Fcm, Apns}`
tokens via the MO9a `FcmSender` (multicast where possible), N-B Push-gated, invalid-token cleanup. Then **one InApp
notification → web push (browser) + FCM push (mobile)** — the owner's Firebase push fires. Kickoff
`BE_NF2_FCM_PUSH_DISPATCH.md`. (Live FCM needs the Firebase secret — dev-safe stub otherwise, per MO9a.)

### N-F3 — lifecycle parity + deeplinks
`OFFER_ACCEPTED` (→ **provider**), `JOB_STARTED` (→ **owner**), `JOB_COMPLETED` (→ **owner**) each fire the
multi-channel notification to the right party (confirm the existing consumers' recipients + add the missing channels).
**Deeplinks:** the push payload carries `ReferenceType/ReferenceId`; the FE routes the click — provider web push →
offers page → offer detail; owner push → offer/SR detail. Kickoff `BE_NF3_LIFECYCLE_PARITY_DEEPLINKS.md`.

## Cross-cutting invariants
- **Preference-gated:** every channel respects N-B (InApp always-on baseline; Push + Email opt-in per category).
- **One logical notification in the list** (InApp row); web/FCM push + email are delivery mechanisms, not extra list
  rows.
- **Region targeting** unchanged (city-code + category via `GetProvidersForArea`).
- **Exactly-once** discipline preserved (WS2 + sequential per-recipient loops).
- **Silent-no-op guard:** every new (Type, Channel) pair needs a seeded template — add Email (and any missing) templates
  for the SR/offer/lifecycle types (a type with no template for a channel = no delivery on that channel).

## Definition of done
Provider: new SR in their region → in-app + **web push** + **email**, deeplink to the request/offer. Owner: offer
received → in-app + **Firebase push** + **email**, deeplink to the offer. Accept/start/complete → the same, per party.
The list shows **one** notification per event. All preference-gated. Region-targeted by city/category.
