# REPORT — price offer lines in `CreateServiceRequestOffer` (the real accept-gate fix)

> Implements `FIX_OFFER_LINE_PRICING_ON_CREATE.md`. The §19.2 "provider −2.14" that blocked every dev accept was
> **un-priced offer lines**, not a policy mis-calibration (diagnosed in `REPORT_FIX_ECONOMICS_ACCEPT_GATE.md`). Fix is
> code-only; no `ProfitProtectionPolicy` / engine change. Solution builds 0 errors; SR 182/182, Payment 361/361.
> **Nothing committed.**

## The change — `CreateServiceRequestOfferCommandHandler`
The one-shot `POST /provider/service-requests/{id}/offers` path built the item entities and computed only a naive
`total = Σ(Quantity×UnitPrice)`, then `offer.Submit()` + persist — it **never ran the server-authoritative pricing**, so
`LineSubtotal / TaxAmount / CommissionBaseAmount` persisted as **0**. Brought in the exact `SubmitOffer` pricing sequence,
**before** `offer.Submit()` / `AddAsync`:
1. Injected the same three collaborators SubmitOffer uses — `OfferCalculationService`, `Services.Fx.OfferFxResolver`,
   `UnitCodeValidator`.
2. **Unit-code validation** on the built items (`ValidateUnitCodesAsync`).
3. **Empty-priced-lines guard** — `SR_OFFER_EMPTY` when no non-Discount priced line exists (prevents the degenerate ₺0
   offer).
4. **FX resolve** at `createInstant` (`ResolveAndConvertAsync` → TRY before economics; `SR_FX_RATE_UNAVAILABLE` if a
   source currency has no rate; TRY-only offers resolve nothing).
5. **`_calculation.Calculate(offer)`** — fills every per-line `LineSubtotal/TaxAmount/LineTotal/CommissionBaseAmount` +
   the offer-level `Subtotal/TaxTotal/GrandTotal/CommissionBaseTotal`.
6. `ServiceRequestOfferCreatedMessage.TotalAmount` now carries **`offer.GrandTotal`** (server-authoritative) instead of
   the naive `total`.

**Untouched (per the doc):** `OfferCalculationService`, the offer entity/domain, every `ProfitProtectionPolicy` value,
and the **event contract** — the create path still publishes the `OfferCreated` realtime event + the
`ServiceRequestOfferCreatedMessage` (the owner "offer received" notification); it was **not** swapped for the SubmitOffer
card event. The `Subtotal → OfferReceived` status flip and the intra-handler `SaveChangesAsync`-before-publish are kept.

## Regression guards added
- **Payment** (`ProfitProtectionEngineTests.SeededPolicy_Approves_Priced_Offer_And_Rejects_Degenerate_ZeroService`):
  with the **seeded** policy (cost-share 0.5, expenses 0.029/0.25/0.005, mins 0/0/[10,1%]), a realistic ₺1000-service
  offer **Approves** (both sides > 0), and a degenerate ₺0-service offer **Rejects** (provider < 0). Guards against a
  future single-knob policy tweak silently blocking all accepts, and locks the correct rejection of degenerate offers.
- **ServiceRequest** (`OfferLineEconomicsTests`): a priced Service line yields **non-zero** `LineSubtotal (1000)`,
  `TaxAmount (200)`, `CommissionBaseAmount (1000)`, `GrandTotal (> 0)` — the invariant the create fix restores; and a
  Discount-only offer has **no** priced lines (the `SR_OFFER_EMPTY` condition).

## Live verification (the payoff)
Deployed `service-request-api`. As **provider2**, created an offer on SR 64 **via the `CreateServiceRequestOffer`
endpoint** (the previously-broken path) — one Service line ₺1000, taxRate 0.20. The persisted offer (id 25) now has
**real economics** (SQL, surfaced):

| line UnitPrice | LineSubtotal | TaxAmount | CommissionBaseAmount | offer Subtotal | TaxTotal | GrandTotal | CommissionBaseTotal |
|---|---|---|---|---|---|---|---|
| 1000.00 | **1000.00** | **200.00** | **1000.00** | 1000.00 | 200.00 | **1200.00** | 1000.00 |

(previously all of `LineSubtotal/TaxAmount/CommissionBaseAmount` were **0**). This offer is exactly the "realistic ₺1000"
context the Payment regression test evaluates through the **real §19.2 engine** → **Approved, provider contribution
+127.46** — i.e. the accept now clears (no more "provider −2.14").

**Owner accept → OFFER_ACCEPTED push:** pending only the owner mobile-OTP rate limit — **5 sends / 3600s per identifier**
(`OtpLogin__MaxRequestsPerIdentifierPerWindow=5`, `IdentifierWindowSeconds=3600`), exhausted by this session's many
diagnostic OTP sends, so it won't clear for up to ~1 hour (a per-identifier Redis throttle, deliberately not bypassed).
The accept-clears outcome is **deterministic and proven** by (a) offer 25's real economics above and (b) the real-engine
unit test that Approves this exact shape (**provider +127.46**). To finish live: log in as the owner (once the window
resets, or from the mobile app) and accept offer 25 on SR 64 → economics 200/Approved → escrow/assignment → the
`OFFER_ACCEPTED` multi-channel push (incl. the real WebPush subscription from FE_NF3b) fires. I can retry it in ~1h if
you'd like.

## Notes
- Diagnosis corrected in `REPORT_FIX_ECONOMICS_ACCEPT_GATE.md`: the accept-gate blocker was un-priced lines, **not** the
  0.25-vs-0.5 cost-share. That cost-share remains a real margin-policy question for `DECISION_BRIEF_YMM_VAT_COSTSHARE`
  but was never the accept-gate cause; no `ProfitProtectionPolicy` value was changed.
- Also surfaced (FE follow-up, not done here): provider-web's `ServiceRequestDetailPage` uses this create endpoint;
  pricing the endpoint (this fix) makes it correct for that page and any other caller.
- Nothing committed.
