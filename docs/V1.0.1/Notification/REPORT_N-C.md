# REPORT — N-C event coverage audit + region-targeted "new job in your area" (+ PreAuth type)

> Phase N-C. Most event→notification consumers already existed; the real work was **region targeting** (built on the new
> Identity **I2** eligibility read-model — see `docs/V1.0.1/Identity/REPORT_I2_PROVIDER_ELIGIBILITY.md`), a **coverage
> audit** (with two recipient fixes), and a **latent PreAuth** type. Everything inherits N-B per-user gating.

## C1 — region-targeted "new job in your area" (main work)
`ServiceRequestCreatedConsumer` used to notify **only** `OwnerUserId`. It now also **fans out to region providers**:
- After the owner's confirmation (unchanged), it calls the new Identity read-model via
  `INotificationIdentityRemoteCall.GetProvidersForArea(message.LocationCityCode, message.ServiceCategoryCode)` and sends
  each returned provider a **`ServiceRequestAreaOpportunity`** notification ("Bölgende yeni iş talebi: {title}").
- `RecipientUserId = ProviderProfileId` (where provider tokens/prefs/inbox live). One notification per provider,
  **sequentially** (WS2/single-DbContext pattern). The owner is excluded (no double-notify). Guards: skip when
  `LocationCityCode` is null; the whole fan-out is wrapped in try/catch so a resolver failure never rolls back the owner
  notification. Scale is bounded by the read-model's `take` cap.
- `ServiceRequestAreaOpportunity = 102` sits in the **ServiceRequests band (100–132)** → it inherits N-B
  **ServiceRequests** gating automatically (in-app always; push only if the provider opted-in + is subscribed). A distinct
  type (not `ServiceRequestCreated`) so providers get the correct area-facing copy without disturbing the owner's
  template; a template `SR_AREA_OPPORTUNITY_INAPP` was seeded.
- **Data source = Identity I2** (`GetProvidersForArea`, approved+active providers whose City matches + category, MVP
  city-level). The task's original assumption (reusable SR-module discovery) didn't hold — I2 is the permanent source.

## C2 — coverage audit (verified; two recipient fixes)
Audited every consumer under `Modules/Notification/.../Consumers/`. Recipients are correct across ServiceRequest / Payment
/ Messaging / Identity / CargoDry (e.g. `PaymentCaptured→PayerProfileId`, `PayoutCompleted→ProviderProfileId`,
`MessagingMessageSent→RecipientUserIds` loop, offer-accepted/completion/dispute → the right owner/opener). **Two real
bugs fixed:** `ServiceRequestOfferCreatedConsumer` and `ServiceRequestAssignmentCreatedConsumer` sent to
`message.ProviderUserId` (raw user id) while the message also carries `ProviderProfileId` — provider notifications/tokens/
prefs are keyed by **ProviderProfileId**, so those notifications landed where the provider's inbox never queries
(invisible + no push). Both now use `ProviderProfileId`.

## C3 — "Ödeme Provizyonda" (PreAuth) type + consumer (latent until P9)
Added `NotificationType.PaymentAuthorized = 157` (Payments category — the map now covers 150–159), a
`PaymentAuthorizedMessage` (Payment abstraction), a `PaymentAuthorizedConsumer` (notifies the payer, mirrors
`PaymentCapturedConsumer`), and a `PAYMENT_AUTHORIZED_INAPP` template. **Latent by design:** Payment BE-P9 PreAuth is
scaffolded (`PaymentAuthMode` enum/resolver/column) but the authorize-only gateway flow + "Authorized/Held" transaction
state + the event publish are **not built** (explicit `TODO(P9-PreAuth)`), so nothing publishes `PaymentAuthorizedMessage`
yet. The type/consumer/template compile and are wired, ready for P9. Documented as a P9 dependency.

## Deploy / quality
`notification-api` (+ `identity-api` for I2) rebuilt + redeployed. Builds 0 errors. N-A push transport, N-B gating,
WS2/two-phase bus, and messaging untouched.

## Verify (live — bus injection, no customer session)
Exercised the fan-out by publishing a real `ServiceRequestCreatedMessage` on the Aizen bus (via a temporary
Development-only `IAizenMessagePublisher` endpoint in the Notification host — added, used, then **removed + redeployed**).
Provider **100011** (İzmir city `35`, category `electrical`, approved+active) had the real browser subscription
associated to it for the test (restored afterward).

1. **Match — İzmir(35) + electrical** → consumer log `region fan-out: notified 1 providers in city 35 (category
   electrical)`; **in-app row** persisted (Type 102 `ServiceRequestAreaOpportunity`, "Bölgende yeni iş talebi: …"); and a
   **real FCM push** received by the service worker (`Web push sent to UserId=100011 for NotificationId=126`), payload
   carrying `referenceType:"ServiceRequest", referenceId:999001` for the deep-link. ✓
2. **Wrong city (34)** → `notified 0 providers` (100011 is in 35; the Istanbul provider 100009 is **Inactive** →
   excluded); **wrong category (hull-paint, city 35)** → `notified 0 providers` (100011 serves only electrical). No push,
   no in-app rows. ✓
3. **N-B gating:** with the provider's **ServiceRequests → Push OFF**, republishing İzmir+electrical still fanned out and
   created the **in-app row**, but delivered **no push** (0 SW notifications). In-app is the baseline; push respects the
   preference. ✓
- **Owner** still gets their separate `ServiceRequestCreated` confirmation (path unchanged).
- **Cleanup:** temp endpoint removed + redeployed; device token restored to its original user; test preference + test
  notification rows deleted.
- **C2 fixes:** offer/assignment consumers now target `ProviderProfileId` (build-verified).
- **C3:** `PaymentAuthorized` type/message/consumer/template compile + are wired (latent — won't fire until P9).

## Next
N-D (live support channel) / N-E (structured reject-cancel reason taxonomy). Region targeting graduates from city-level
to geo/service-area with GeoDiscovery (I2 left the seam).
