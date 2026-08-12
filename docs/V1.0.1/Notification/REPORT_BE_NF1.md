# REPORT — BE_NF1 notification correctness layer (D1/D2/D4/D5)

> Implements `docs/V1.0.1/Notification/BE_NF1_CORRECTNESS_LAYER.md`. Additive, in-app + web-push only (email/FCM = N-F2).
> Builds 0 errors (solution), Notification tests 21/21, ServiceRequest tests 180/180. **Nothing committed.**
> **D1 and D2 live-verified on the running stack.** One material discovery on owner inbox visibility is flagged below.

## What changed (code)

### D1 — region fan-out rebound to the event that actually fires
- **New consumer** `Modules/Notification/.../Consumers/ServiceRequest/ServiceRequestPublishedConsumer.cs` binds
  `ServiceRequestPublishedMessage` (the event the publish flow actually emits). It does the **region fan-out** — moved
  verbatim from the dead consumer — `GetProvidersForArea(LocationCityCode, ServiceCategoryCode)` → one
  `ServiceRequestAreaOpportunity` (InApp) per provider (keyed by `ProviderProfileId`, owner excluded, **sequential**),
  plus an **owner-facing** `ServiceRequestPublished` ("your request is live") notification that supersedes the never-fired
  "created" one. Auto-registered by the bus assembly scan (no DI edit).
- **Retired** the dead `ServiceRequestCreatedConsumer.cs` (nothing publishes `ServiceRequestCreatedMessage`; grep of
  `new ServiceRequestCreatedMessage` = empty). Left the message *type* in place (only referenced by an XML `<see cref>`).
- **New enum + template:** `NotificationType.ServiceRequestPublished = 104` + `SR_PUBLISHED_INAPP` template.

### D2 — offer → owner
- **`ServiceRequestOfferCreatedMessage` gains `OwnerUserId`** (additive; defaults 0). Populated at the sole publisher,
  `CreateServiceRequestOfferCommandHandler` (`OwnerUserId = sr.OwnerUserId`).
- **`ServiceRequestOfferCreatedConsumer`** now also notifies the owner (`RecipientUserId = OwnerUserId`,
  `NotificationType.OfferReceived = 113`, InApp) when `OwnerUserId != 0`. The provider `OfferCreated` (110) confirmation
  is **kept**. New enum `OfferReceived = 113` + `SR_OFFER_RECEIVED_INAPP` template (silent-no-op guard satisfied).

### D4 — inbox filtered to the canonical InApp row
- `NotificationRepository`: `GetByRecipientAsync`, `CountByRecipientAsync`, `GetUnreadCountAsync`, and
  `BulkMarkAsReadAsync` now all filter `Channel == NotificationChannel.InApp` so list, total, unread, and mark-all agree
  on the one logical row once N-F2 adds Email/Push rows. Behaviour-neutral today (all rows are InApp).

### D5 — CompletionApproved recipient-key fix
- **`ServiceRequestCompletionApprovedMessage` gains `ProviderProfileId`** (additive). Populated at
  `ApproveServiceRequestCompletionCommandHandler` from the SR's assignment (`GetByServiceRequestIdAsync`).
- **`ServiceRequestCompletionApprovedConsumer`** now keys by `ProviderProfileId` (consistent with OfferCreated /
  AssignmentCreated), falling back to `ProviderUserId` only when the profile id is absent. Previously it filed under the
  raw `ProviderUserId` — a mis-key masked only when `UserId == ProfileId`.

## Live verification (running stack)

Deployed by rebuilding + recreating `service-request-api` and `notification-api`. Templates `SR_PUBLISHED_INAPP` (104)
and `SR_OFFER_RECEIVED_INAPP` (113) seeded on startup (idempotent by TemplateCode). Actors: owner
`qa.owner.aug5@inktavia.com` (User 100029 / participant profile 100030), provider2 `provider2@inktavia.com`
(profile 100011, city 35, electrical). Both authenticated passwordlessly (owner mobile OTP; provider OTP → real Keycloak
PKCE handoff — token never logged).

### D1 — PASS
Created + published **SR 60** (ELECTRICAL / city 35) as the owner. Result (notification rows, baseline was Id 197):

| Id | RecipientUserId | Type | Channel | Ref | Title |
|----|-----------------|------|---------|-----|-------|
| 198 | 100029 (owner) | 104 ServiceRequestPublished | InApp | 60 | "Talebiniz yayında: SR129E1A28" |
| 199 | 100011 (provider2) | 102 ServiceRequestAreaOpportunity | InApp | 60 | "Bölgende yeni iş talebi" |

Log: `SR SR129E1A28 region fan-out: notified 1 providers in city 35 (category ELECTRICAL).` Owner not double-notified as
a provider. (Contrast: SR 59, published under the old code during N-F0, produced **zero** rows.)

### D2 — PASS (notification rows created)
Provider2 created an offer on SR 60. Result (baseline was Id 199):

| Id | RecipientUserId | Type | Channel | Ref | Title |
|----|-----------------|------|---------|-----|-------|
| 200 | 100011 (provider2) | 110 OfferCreated | InApp | 60 | "New Offer on Request #60" (provider confirmation kept) |
| 201 | 100029 (owner) | 113 OfferReceived | InApp | 60 | "Yeni teklif aldınız — Talep #60" |

The owner now gets a row (previously nothing); the provider confirmation still fires; the message carries `OwnerUserId`.

### D4 — PASS (verified via the provider inbox)
Injected a synthetic **Email** (Channel=3) row for provider2 (100011). DB then held 74 InApp + 1 Email row; the provider
inbox (`GET /api/v1/provider/notifications`) returned **total 74, channels = [InApp] only**, and the Email row was
**absent**. Before the filter it would have surfaced as a duplicate list row. Synthetic row deleted afterward.

### D5 — code-verified (not live-triggered)
The publisher now sources `ProviderProfileId` from the assignment and the consumer keys by it. A live trigger needs a full
assigned→completion-submitted→owner-approve cycle (the exposing case is SR 30009, provider profile 11012 ≠ user 10012);
that flow is outside this smoke's D1/D2 scope. Covered by the 0-error build + the green ServiceRequest/Notification tests.

## ⚠ Material discovery — owner mobile inbox does not render owner notifications → RESOLVED by BE_NF1b
The D2 owner row (201) is created correctly per the doc (`RecipientUserId = OwnerUserId = 100029`), but the owner's
**mobile inbox** initially returned **0 items**. Investigation (per the follow-up ask) found **two confounded** causes,
both since fixed:
1. **Stale `bff-marine-mobile`** — its Refit client couldn't deserialize the new `NotificationType` values 104/113, so
   the whole inbox call 500'd. Fixed by rebuilding the mobile BFF (a required deploy step whenever the enum grows;
   `bff-adminpanel` also deserializes `NotificationType` and should be rebuilt too).
2. **Recipient-key mismatch** — owner notifications are filed under `OwnerUserId` (user id 100029) but the mobile owner
   inbox + device tokens resolve the participant **profile id** (100030). Confirmed once #1 was fixed: a probe under
   100030 surfaced, a probe under 100029 did not.

Both are addressed in **`BE_NF1b`** (`REPORT_BE_NF1b.md`): a scoped Identity user-id→participant-profile-id resolver, owner
notifications filed under the profile id, and a one-time backfill of the historical rows. After NF1b the owner inbox
shows the offer (row 201) + full history, and web push reaches the owner. This report's D2 remains valid (the row is
created); NF1b makes it **visible**.

## Also flagged (unchanged from N-F0)
- **0 active WebPush subscriptions** — even with the fan-out now firing, provider web push won't land until providers
  register a WebPush subscription (client/ops prerequisite).

## Test artifacts left on the dev stack
SR 60 (published ELECTRICAL/city-35) and one offer on it by provider2. Notification rows 198–201.

## Next
**N-F2** (Email sends + templates, FCM/APNs on the InApp push path) → **N-F3** (lifecycle parity + deeplinks). The
owner-inbox visibility issue above should be scheduled alongside these so D2 is user-visible.
