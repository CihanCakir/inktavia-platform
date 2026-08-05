# N-C — event coverage audit + region-targeted "new job in your area" (+ Ödeme Provizyonda type)

> **Repos:** `addesso-project` (Notification + ServiceRequest + Payment modules). Phase N-C of
> `Notification/ROADMAP_DELIVERY_SUPPORT_REASONS.md`. Most events already have notification consumers — the real work is
> **region targeting for new service requests** + a small coverage audit + the missing **PreAuth ("Ödeme Provizyonda")**
> type. All notifications automatically inherit **N-B** per-user category×channel gating (no extra work).

## Current coverage (investigated — already wired)
Notification consumers already exist for: SR **Created/StatusChanged/OfferCreated/OfferAccepted/AssignmentCreated/
CompletionSubmitted/DisputeOpened**, Payment **Captured/EscrowReleased/Cancelled/Failed/Refunded/ReminderRequested/
PayoutCompleted**, **MessagingMessageSent**, all CargoDry, Identity profile/OTP. So "Kullanıcı Mesajı"
(`MessagingMessageSent`) and "Ödeme Alındı" (`PaymentCaptured`) already fire. **Gaps below.**

## C1 — region-targeted "new job in your area" (the main work)
**Gap:** `ServiceRequestCreatedConsumer` notifies **only `OwnerUserId`** (the requester). It does **not** notify providers
in the region. But `ServiceRequestCreatedMessage` carries `LocationCityCode`, `LocationCountryCode`, `ServiceCategoryCode`,
`Priority`, `Title` — everything needed to fan out.
- **Add region fan-out:** when a service request is created, notify **providers whose city matches `LocationCityCode`
  (+ service category matches `ServiceCategoryCode`)**, each with a `ServiceRequestCreated` notification ("Bölgende yeni
  iş talebi: {Title}"). Resolve the target providers via the **existing discovery / provider-by-city** infra in the
  ServiceRequest module (there's `GetProviderDiscovery` / `LocationCityCode` filtering); expose a remote call
  `GetProvidersForServiceRequestArea(cityCode, categoryCode)` returning active/approved provider user ids, and the
  consumer sends one notification per provider. **Do not** notify the owner twice (keep the existing owner notification,
  add the provider fan-out as a separate recipient set).
- **MVP = city-level** (matches the provider realtime city group). Richer radius/service-area targeting comes with
  **Identity I2** (service-area) — leave a seam for it.
- Inherits N-B gating: each provider only gets push if they enabled ServiceRequests→Push (and are subscribed); in-app
  always lands. Respect the two-phase-bus/sequential patterns; one notification per provider.
- Guard scale: only **active/approved** providers in the city with a matching category; cap/paginate if a city has many.

## C2 — coverage audit (verify recipients, fix gaps)
Confirm each requested/expected event notifies the **right** recipient(s):
- **Kullanıcı Mesajı** (`MessagingMessageSent`) → participants except sender (done in messaging-completion).
- **Ödeme Alındı** (`PaymentCaptured`) → the correct party (payer/owner + provider as designed).
- Offer accepted/rejected, assignment, completion, dispute, payout, refund → the right counterparty.
Fix any consumer that targets the wrong/no recipient. (Most are fine — this is a verification pass.)

## C3 — "Ödeme Provizyonda" (PreAuth) type + consumer (latent until P9)
- Add a `NotificationType.PaymentAuthorized` (or `PaymentPreAuthHeld`) in the 150-band and map it to the **Payments**
  category (N-B). Add a `PaymentAuthorizedConsumer` that notifies the payer when a **PreAuth/authorization hold** is
  placed (P9 auth-mode). This **won't fire until P9 PreAuth is live** — define it now so it's ready; document that.
- (If a suitable Payment bus event doesn't exist yet, note it as a P9 dependency rather than inventing a fake trigger.)

## Don't-break / QA
- Additive: extend one consumer (SR-created fan-out) + a remote call + one new type/consumer (latent) + verification. N-A
  push transport, N-B gating, WS2/two-phase-bus, messaging untouched. Category map (N-B) covers the new type. Envelope-
  correct remote calls; same-image redeploy; builds clean.

## Verify (on-screen)
1. **Region targeting:** create a service request in a city where a matching-category provider exists → **that provider**
   gets a "new job in your area" notification (in-app + push if opted-in); a provider in a **different** city/category
   does **not**. The owner still gets their own confirmation.
2. Message + payment-captured notifications reach the right recipients (spot-check).
3. N-B gating holds: a provider with ServiceRequests→Push off gets the in-app row but no push.
4. PreAuth type/consumer compiles and is wired (won't fire without P9 — documented).

## Report
`docs/V1.0.1/Notification/REPORT_N-C.md`: the SR-created region fan-out (resolver + recipients + scale guard), the
coverage-audit results (any recipient fixes), the PreAuth type/consumer (+ its P9 dependency), and the on-screen
region-targeting proof. Next: N-D (live support channel) / N-E (reject-cancel reason taxonomy).
