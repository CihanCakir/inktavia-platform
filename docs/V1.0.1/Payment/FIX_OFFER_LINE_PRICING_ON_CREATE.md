# FIX — price offer lines in `CreateServiceRequestOffer` (the real accept-gate root cause)

> **Repo:** `addesso-project` (ServiceRequest module). The §19.2 profit-protection "provider −2.14" that rejects
> every dev offer-accept is **not** a policy-calibration problem — it is **un-priced offer lines**. The one-shot
> `CreateServiceRequestOffer` path (the endpoint provider-web actually uses) never runs the server-authoritative
> pricing, so `LineSubtotal / TaxAmount / CommissionBaseAmount` persist as **0** and the accept-time economics sees a
> ₺0 service. Diagnosed in `REPORT_FIX_ECONOMICS_ACCEPT_GATE.md`. **Fix in code, not config. Do not commit.**

## Confirmed root cause (from code + the diagnostic)
- `SubmitOfferCommandHandler` prices before persisting: **FX resolve → `OfferCalculationService.Calculate(offer)` →
  `MarkSubmitted`**. `Calculate` is what fills the per-line economics — `SetComputedTotals(LineSubtotal, DiscountAmount,
  TaxAmount, LineTotal)` and `SetComputedEconomics(commissionBase)` — plus the offer-level `Subtotal / TaxTotal /
  GrandTotal / CommissionBaseTotal`.
- `CreateServiceRequestOfferCommandHandler` (the `POST .../offers` path) builds the item entities and computes only a
  naive `total = Σ(Quantity*UnitPrice)`, then calls `offer.Submit()` and persists — it **never** calls
  `OfferCalculationService.Calculate` **nor** the FX resolver. So every line keeps `LineSubtotal = TaxAmount =
  CommissionBaseAmount = 0`.
- At accept, the profit-protection engine derives `providerContribution = commission(0) − providerVarCost(2.14) =
  −2.14 < min 0 → Rejected`, **price-independent** (the offer's `UnitPrice` never reaches the economics because the
  commission base is 0). `provider-web`'s `ServiceRequestDetailPage` uses this un-priced create endpoint, so this is a
  **real product bug**, not a test artifact.
- The already-accepted seed offers (SR 30009 / 9011) went through the **draft→submit** path, which is why they have
  real priced lines and clear the gate.

## The fix — make `CreateServiceRequestOffer` self-sufficiently price the lines
`CreateServiceRequestOffer` is a legitimate one-shot create+submit endpoint; it must produce a **fully priced,
economics-complete** offer exactly as draft→submit does. Bring the SubmitOffer pricing sequence into the create
handler; do **not** change `OfferCalculationService`, the offer entity, or any `ProfitProtectionPolicy`.

### 1. Inject the pricing collaborators
Add to `CreateServiceRequestOfferCommandHandler`'s constructor the same three services `SubmitOfferCommandHandler`
already uses:
- `OfferCalculationService _calculation`
- `Services.Fx.OfferFxResolver _fxResolver`
- `UnitCodeValidator _unitCodeValidator`

### 2. Price before persisting (mirror SubmitOffer's order exactly)
After the aggregate is fully built (`offer.AddItem(...)` loop done) and **before** `offer.Submit()` /
`_offerRepository.AddAsync`:
1. **Unit-code validation** — validate any non-empty `UnitCode`s on the items (same `_unitCodeValidator.ValidateUnitCodesAsync`
   call SubmitOffer runs), so a bad unit code fails loud on create too.
2. **Empty-priced-lines guard** — reject with `SR_OFFER_EMPTY` when there are no non-Discount priced lines (parity
   with SubmitOffer; prevents creating the degenerate ₺0 offer that produced −2.14).
3. **FX resolve at the create instant** — `var createInstant = DateTime.UtcNow; await
   _fxResolver.ResolveAndConvertAsync(offer, createInstant, ct);` — converts any foreign lines to TRY **before** the
   economics runs; fail-loud `SR_FX_RATE_UNAVAILABLE` if a source currency has no effective rate. TRY-only offers
   resolve nothing (unchanged).
4. **Calculate** — `_calculation.Calculate(offer);` — fills every per-line `LineSubtotal / TaxAmount / LineTotal /
   CommissionBaseAmount` and the offer-level `Subtotal / TaxTotal / GrandTotal / CommissionBaseTotal`.
5. Keep the existing `offer.Submit()` (create still submits in one shot) and persist as today (`AddAsync` →
   intra-handler `SaveChangesAsync` so the DB assigns the offer id before publish — the WC1b flush already in place).

### 3. Use the computed total, not the naive sum
Replace the message/response `TotalAmount = total` (the naive `Σ Quantity*UnitPrice`) with **`offer.GrandTotal`** (the
post-discount, tax-inclusive, server-authoritative figure), matching what SubmitOffer carries. The naive `total` fed to
`ServiceRequestOfferEntity.Create(...)` may remain as the initial constructor value, but everything downstream (the
`ServiceRequestOfferCreatedMessage.TotalAmount`, the realtime DTO, the response) must reflect the **calculated**
totals — `offer.ToDto()` already reads the computed fields, so it is correct once `Calculate` has run.

### 4. Do NOT change the event contract
The create path's messaging semantics stay as-is: it still publishes the **`OfferCreated` realtime event** and the
**`ServiceRequestOfferCreatedMessage`** (the one the Notification module consumes for the owner "offer received"
notification). Do **not** swap it for `ServiceRequestOfferSubmittedMessage` here — that is SubmitOffer's card event and
changing it is out of scope. Only the **pricing** is added; the fan-out is untouched.

> **Alternative considered (rejected for now):** rerouting `provider-web`'s `ServiceRequestDetailPage` through the
> `SaveOfferDraft → SubmitOffer` pair instead. That also fixes the symptom, but it is a larger FE change and leaves the
> `CreateServiceRequestOffer` **endpoint itself** producing invalid un-priced offers for any other caller. Pricing the
> endpoint is the correct, client-agnostic fix. (If desired later, the FE reroute can be a separate follow-up.)

## Regression guard (this is why all accepts silently broke — protect it)
Add tests so a future change can't reintroduce a 0-economics offer or a single-knob policy tweak can't mask it:
1. **Unit / economics test on create** — construct a realistic priced offer via `CreateServiceRequestOfferCommand`
   (e.g. one Service line ₺1000, taxRate as seeded) and assert the persisted offer has **non-zero** `LineSubtotal`,
   `CommissionBaseAmount` (> 0), `GrandTotal` > 0, and that a TRY-only offer is byte-identical whether created via
   create vs draft→submit.
2. **Profit-protection clearance test** — feed that created offer's economics through the §19.2 evaluation (the same
   path accept uses) and assert **both** the customer-side and provider-side contributions clear their minimums
   (Approved), with the **seeded** policy (cost-share 0.5) unchanged. A degenerate ₺0 offer must still be rejected
   (correct behavior).
3. **Empty / FX edge tests** — no priced lines → `SR_OFFER_EMPTY`; a foreign line with no effective rate →
   `SR_FX_RATE_UNAVAILABLE`.

## Live verification (the payoff — unblocks OFFER_ACCEPTED)
1. As **provider2**, create an offer on a fresh SR **through the provider-web detail page** (the un-priced endpoint) —
   confirm the persisted offer now has non-zero `LineSubtotal / CommissionBaseAmount` (SQL SELECT, surfaced not
   executed by me).
2. As the **owner**, **accept** it → the accept-time economics returns **200 / Approved** (no more "provider −2.14") →
   escrow/assignment proceeds.
3. The **`OFFER_ACCEPTED`** multi-channel notification (web push + email + FCM) finally fires live — the last piece the
   N-F track couldn't demo because accept was blocked.

## Don't-break / QA
- Additive to the create handler only: pricing sequence + guards + computed-total wiring. `OfferCalculationService`,
  the offer entity/domain methods, the event contract, and every `ProfitProtectionPolicy` value are **unchanged**
  (§19.3 "no hardcoded constants" preserved; relaxing the shared policy was correctly sandbox-blocked and would have
  been the wrong fix).
- DB writes are surfaced as SQL, not executed. Identity still comes from the trusted provider-profile context, never
  the body. UTC instants for FX/timestamps.
- Tests: (1) create-path economics non-zero; (2) realistic offer clears §19.2 both sides at seeded 0.5, degenerate ₺0
  still rejected; (3) empty/FX edge cases; (4) `dotnet build` + the SR module test suite green; (5) **live: priced
  offer via provider-web → owner accept → 200/Approved → OFFER_ACCEPTED push fires**.

## Report
`docs/V1.0.1/Payment/REPORT_FIX_OFFER_LINE_PRICING_ON_CREATE.md`: the create-handler diff (injected services + pricing
sequence + computed-total wiring), the SQL proof that a create-path offer now persists non-zero line economics, the
regression-test results, and the **live accept → 200/Approved → OFFER_ACCEPTED fired** proof. Update
[[production_economics_values]] (accept-gate → resolved-in-code) and note in `DECISION_BRIEF_YMM_VAT_COSTSHARE` that
the 0.25-vs-0.5 cost-share remains a real margin-policy question but was **never** the accept-gate blocker.
