# BE_WC1b — OFFER-card repoint (WC1 smoke fix)

> **Repo:** `addesso-project` (ServiceRequest + Messaging). Phase-4 **WC1 follow-up**: the WC1 live smoke found the
> OFFER card never fires — the real provider offer path (`CreateServiceRequestOffer`) produces **Submitted** offers
> **without** calling `SubmitOffer`, so the WC1 `ServiceRequestOfferSubmittedMessage` is never published → no
> `sys:{srId}:OFFER:{offerId}` row. Repoint the card so it fires on the real path too. Additive; flagged (rides the
> existing WC1 flag). **Do not commit.**

## Root cause (from the smoke + code)
- `CreateServiceRequestOffer` creates the offer via `ServiceRequestOfferEntity.Create(...)` (produces a **Submitted**
  offer in the live path), flips SR status to `OfferReceived`, publishes the realtime `OfferCreated` +
  **`ServiceRequestOfferCreatedMessage`** (`{ ServiceRequestId, OfferId, ProviderProfileId, ProviderUserId,
  TotalAmount, CurrencyCode }`). It does **not** call `SubmitOffer`.
- WC1 wired the OFFER card to `ServiceRequestOfferSubmittedMessage` (published only by `SubmitOffer`, the draft→submit
  path) → for a directly-created Submitted offer the card never fires.
- `ServiceRequestOfferCreatedMessage` carries **no Status** — so a consumer can't tell a Draft create from a Submitted
  create.

## Fix — drive the OFFER card from `OfferCreated` (guarded), keep `OfferSubmitted`, dedup collapses both
1. **Add a Status to `ServiceRequestOfferCreatedMessage`** (additive): `ServiceRequestOfferStatus Status` (or a
   `bool IsSubmitted`). Populate it in **both** publishers — `CreateServiceRequestOffer` (the real value:
   Submitted/Draft) and any other offer-created publish. Existing consumers ignore the new field.
2. **New Messaging consumer for `ServiceRequestOfferCreatedMessage`:** when `Status >= Submitted`, write the OFFER card
   via the shared `ServiceRequestLifecycleMessageWriter`:
   - `SourceKey = "sys:{srId}:OFFER:{offerId}"` (offer-scoped — multiple providers ⇒ multiple cards, each distinct;
     idempotent via the WC0 partial unique index).
   - Content `offer:{offerId}|{TotalAmount:F2} {CurrencyCode}`, `SenderRole` = Provider, `Type` = StatusChange (no new
     enum — matches the sync-row parity / content-based read side, per the WC1 decision).
   - **Skip Draft** offers (no card until Submitted).
3. **Keep the `ServiceRequestOfferSubmittedMessage` consumer** for the draft→submit path. Since both consumers use the
   **same** `sys:{srId}:OFFER:{offerId}` key, the WC0 unique index collapses them to one row (a draft that is later
   submitted, or a direct Submitted create, both yield exactly one card). The sync consumer's lifecycle key derivation
   already converges on the same key (WC1).
4. **Flag:** rides the existing `Messaging:WriteCutover:SystemMessages` gate — the SR-side offer-message write in
   `SubmitOffer`/`CreateServiceRequestOffer` (the `sr.Messages` offer row) stays flag-gated exactly as the other
   lifecycle writes; when ON, Messaging is the sole producer of the OFFER card via this consumer.

## Don't-break / QA
- Additive: one new nullable event field + one Messaging consumer; the `OfferSubmitted` consumer + the other 4
  lifecycle consumers are unchanged. The fresh-SR `if(!created) Update()` fix from the smoke stays.
- **Idempotency:** one OFFER card per `(conversation, offerId)`; a create-Submitted, a draft→submit, redelivery, and
  the parallel sync row all collapse to one via the unique index.
- Tests: (1) SR + Messaging + solution build 0 errors; (2) `CreateServiceRequestOffer` (Submitted) → OFFER card fires
  with `sys:{srId}:OFFER:{offerId}` + `offer:{id}|{total} {ccy}`; (3) a Draft create → **no** card; (4) draft→
  `SubmitOffer` → card fires once (no duplicate with a later/earlier OfferCreated for the same offer); (5) two
  providers on one SR → two distinct OFFER cards; (6) flag ON → SR writes no `sr.Messages` offer row, the card still
  appears (Messaging sole producer); (7) no new notification for the OFFER card. Live re-smoke: create an offer as
  provider2 → the OFFER card appears once on owner + provider + admin (flag OFF and ON).

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC1b_OFFER_CARD_REPOINT.md`: the added `OfferCreated.Status`, the new OfferCreated
Messaging consumer (guarded Status≥Submitted, `sys:{srId}:OFFER:{offerId}`, dedup with OfferSubmitted), and the tests
+ the live re-smoke of the OFFER card. **Remaining WC1-parity item:** the Payment S2S 401 (pre-existing) still blocks
the on-surface confirmation of `OFFER_ACCEPTED`/`JOB_STARTED`/`JOB_COMPLETED` — diagnose separately (their events are
real + use the proven writer path, so the risk is wiring, not the engine). Then **WC2**.
