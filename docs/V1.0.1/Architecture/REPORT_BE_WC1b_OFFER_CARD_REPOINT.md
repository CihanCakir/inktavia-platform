# REPORT — BE_WC1b — OFFER-card repoint (WC1 smoke fix)

> **Repo:** `addesso-project` (ServiceRequest + Messaging). **Status:** implemented + **verified live**, not committed.
> Additive; rides the existing `Messaging:WriteCutover:SystemMessages` flag.

## Problem (from the WC1 live smoke)
WC1 wired the thread's OFFER card to `ServiceRequestOfferSubmittedMessage` (published only by the `SubmitOffer`
command / draft→submit path). But the **real** provider offer path — `CreateServiceRequestOffer` (`POST
/api/v1/provider/service-requests/{id}/offers`) — calls `offer.Submit()` internally and publishes
`ServiceRequestOfferCreatedMessage`, **never** invoking `SubmitOffer`. So for a directly-created Submitted offer the
WC1 event never fired → **no `sys:{srId}:OFFER:{offerId}` card** (smoke: 3 submitted offers, 0 cards).

## Fixes
### 1. `ServiceRequestOfferCreatedMessage.Status` (additive)
Added `ServiceRequestOfferStatus Status` (default `Draft`), populated from `offer.Status` in
`CreateServiceRequestOfferCommandHandler` (the real value — `Submitted`). Existing consumers (Notification) ignore it.
A consumer can now distinguish a Submitted create from a Draft create (the event previously carried no status).

### 2. New Messaging consumer `ServiceRequestOfferCreatedCardConsumer`
Consumes `ServiceRequestOfferCreatedMessage`; when `Status >= Submitted` it writes the OFFER card via the shared
`ServiceRequestLifecycleMessageWriter`:
- `SourceKey = sys:{srId}:OFFER:{offerId}`, `SenderRole = Provider`, `Type = StatusChange`, content
  `offer:{offerId}|{TotalAmount:F2} {CurrencyCode}` (no new enum — matches the sync-row parity / content-based read
  side, per the WC1 decision).
- **Draft offers are skipped** (no card until Submitted).
The existing `ServiceRequestOfferSubmittedCardConsumer` is kept for the draft→submit path. Both consumers use the
**same** `sys:{srId}:OFFER:{offerId}` key, so the WC0 partial unique index collapses a create-Submitted, a draft→submit,
redelivery, and the parallel sync row to exactly one card.

### 3. Offer identity flush (root-cause bug the repoint surfaced)
`CreateServiceRequestOffer` published `OfferCreated` with `offer.Id = 0` — the identity is assigned on **save**, not on
`AddAsync`, and the publish ran before the framework's post-handler unit-of-work saved. The first live card came out as
`sys:{srId}:OFFER:0` / `offer:0|…` and would have **collided across offers**. Fix: an intra-handler
`await _db.SaveChangesAsync(ct)` after the offer add + SR-status update, **before** the publishes (same pattern
`SaveOfferDraft` already uses). This also fixes the pre-existing `offerId = 0` in the `OfferCreated` **realtime** event,
the **notification** metadata, and the **BFF response**.

## Live verification (rebuilt + redeployed messaging-api + service-request-api)
- **Build:** full `Aizen.sln` **0 errors**; `Messaging.Application.UnitTests` **15 pass**.
- **Consumer registered:** `Configured endpoint ServiceRequestOfferCreatedCard` on the bus (alongside
  `ServiceRequestOfferSubmittedCard`).
- **Flag OFF (SR 55):** provider2 (OTP session, :3002) `CreateOffer` → OFFER card **`sys:55:OFFER:17`**
  (`SenderRole=Provider`, `Type=StatusChange`, `offer:17|2000.00 TRY`), **dupe 0**, **no `OFFER:0`** row — correct real
  offer id (BFF response `offerId:17`). ✅
- **Two offers → two distinct cards (item 5):** a 2nd offer (id 18) → `sys:55:OFFER:17` + `sys:55:OFFER:18`
  (`offer:18|2400.00 TRY`), 2 distinct cards. ✅
- **Flag ON (SR 56):** offer 19 → card `sys:56:OFFER:19` appears (Messaging **sole producer**); SR-store offer row
  (`MessageType=4`) = **0**. ✅
- **Notification silence (item 7):** SR 56 has one `Type=110` `OfferCreated` **provider notification** — the
  **pre-existing** offer notification from the `OfferCreated → Notification` consumer, **not** the card. The WC1b card
  writer publishes with empty `RecipientUserIds` → no new notification for the card. ✅
- **Draft → no card (item 3):** covered by the `Status >= Submitted` guard (skips Draft) — code + guard verified; not
  driven live because `CreateServiceRequestOffer` always submits and the draft→`SaveOfferDraft` path wasn't exercised.

## Idempotency
One OFFER card per `(conversation, offerId)`. A create-Submitted, a draft→submit, redelivery, and the parallel sync row
all key `sys:{srId}:OFFER:{offerId}` → collapse to one via the WC0 partial unique index. (The transitional flag-OFF
conversation-creation race documented in the WC1 smoke does not apply to the `CreateOffer` card flow — that path writes
no `sr.Messages` offer row, so there is no parallel sync producer to race with.)

## Remaining WC1-parity item (unchanged, not WC1b)
`OFFER_ACCEPTED` / `JOB_STARTED` / `JOB_COMPLETED` still can't be driven on-surface because owner **accept** fails with a
pre-existing **Payment S2S 401** (`ServiceRequest → Payment CalculateServiceRequestEconomicsAsync`, 401) — diagnose
separately. Their events are real and use the proven System-writer path (`CONVERSATION_CLOSED` verified live in the WC1
smoke), so the risk there is wiring/config, not the engine. Then **WC2**.

## State left on the stack
Flag reverted **OFF** (compose edit removed, SR-api restarted); messaging-api + SR-api run the fixed WC0+WC1+WC1b code;
no credentials changed; the provider token stayed in the browser session (never extracted/logged). Working-tree changes
(4 files) uncommitted: `ServiceRequestOfferCreatedMessage`, `CreateServiceRequestOfferCommandHandler`,
`ServiceRequestLifecycleConsumers`, plus the WC1 smoke fixes. Report: this file.
