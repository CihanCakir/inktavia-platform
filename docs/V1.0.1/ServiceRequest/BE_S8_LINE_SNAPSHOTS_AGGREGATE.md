# BE-S8 — Immutable line snapshots → aggregate derivation + 8 equalities (0 tolerance) — Backend Prompt

> **Module:** `Aizen.Modules.Payment` (line snapshots live with the aggregate `PaymentEconomicsSnapshot`, BE-P1 — same
> immutability/settlement domain). **Phase:** ServiceRequest S8 (roadmap `docs/V1.0.1/ServiceRequest/ROADMAP.md`), last of
> the narrow P8 core (S1 ✅ → S7 ✅ → **S8** → Payment P8).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §20.15 (line→aggregate + the 8 equalities), §5/§13.4 (single
> immutable aggregate snapshot — preserved), §13.6 (rounding).
> **Rule:** EXTEND BE-P1's immutable `PaymentEconomicsSnapshotEntity` pattern (validating `Create` factory, private
> setters, no mutators, `MoneyMath`, zero-tolerance invariants) to a **line-first creation path** with child line
> snapshots; do NOT rewrite BE-P1, do NOT weaken its existing aggregate invariants.
> **Non-goal:** no acceptance wiring / resolver calls / checkout / `MarkApplied` (all P8); no travel snapshot (S4) / no
> pricing-attribute snapshot (S2) — **reserve the names, do not build them here**; discount allocation is modeled but
> **0 in the narrow core** (S6 populates later). S8 is the **immutable line-snapshot model + `CreateFromLines` factory +
> the 8 equalities**, unit-tested with synthetic line sets. Inspect first.

## 0. Verified current state
- BE-P1 `PaymentEconomicsSnapshotEntity` (Payment.Domain/Entities/Economics) — immutable, `Create(...)` validating
  factory, `MoneyMath.Round`(2dp)/`RoundRate`(4dp), aggregate fields: `ServiceAmountSnapshot, ServiceVatAmountSnapshot,
  ServiceGrossAmountSnapshot, CustomerPayableServiceAmountSnapshot, CommissionBaseAmountSnapshot, CommissionRateSnapshot,
  CommissionAmountSnapshot, ProviderNetAmountSnapshot, PlatformFee*Snapshot, CustomerTotalAmountSnapshot,
  PlatformGrossShareSnapshot`, `ContextId`, `SnapshotCode`, `CurrencyCodeSnapshot`. Invariants throw
  `PaymentEconomicsInvariantException`. `transactions.EconomicsSnapshotId` FK exists. **Missing = the §20.15 line
  decomposition fields + any line children.**
- S1 (done) gives per-line `CommissionBaseAmount` + `LineCommissionEligibility`; S7 (done) gives the resolved per-line
  `{Commissionable, ResolvedRate, RuleCode, CommissionAmount, ProviderNet}` + transaction totals (Σ = transaction, per-line
  rounding preserved). These are exactly the **line inputs** S8 snapshots.

## 1. §20.15 aggregate decomposition fields (append to `PaymentEconomicsSnapshotEntity`)
Add (append-only; do not touch existing) the aggregate fields the 8 equalities reconcile to, each **derived from line
sums** in the factory:
`OriginalServiceGrossAmountSnapshot` (Σ line gross before discounts), `TotalCustomerDiscountSnapshot`,
`TotalProviderFundedDiscountSnapshot`, `TotalPlatformFundedDiscountSnapshot`, `ServiceVatTotalSnapshot`. The already-present
`CommissionAmountSnapshot`(=TotalProviderCommission), `ProviderNetAmountSnapshot`(=ProviderNetTotal),
`ServiceVatAmountSnapshot`, `CustomerTotalAmountSnapshot`, `PlatformFeeGrossAmountSnapshot` map to the remaining equalities.

## 2. Immutable line snapshot entities (Payment.Domain/Entities/Economics)
FK to `PaymentEconomicsSnapshot.Id` (`OnDelete Restrict`), private setters, **no mutators** (insert-only, like BE-P1):
- `OfferLineEconomicsSnapshotEntity`: `EconomicsSnapshotId`, `LineRef` (offer-item correlation), `ItemType`,
  `PricingMethod`, `LineGrossBeforeDiscount`, `CustomerDiscountAmount`, `ProviderFundedDiscountAmount`,
  `PlatformFundedDiscountAmount`, `CommissionEligibility`, `CommissionBaseAmount`, `CommissionRate`, `CommissionAmount`,
  `ProviderNetAmount`, `LineVatAmount`, `LineTotalAmount`, `CurrencyCode`, `SortOrder`.
- `CommissionAllocationSnapshotEntity`: `EconomicsSnapshotId`, `LineRef`, `CommissionRuleId?`, `CommissionRuleCode?`,
  `CommissionBaseAmount`, `ResolvedRate`, `CommissionAmount`, `Commissionable` (the resolved-rule audit trail from S7).
- `DiscountAllocationSnapshotEntity`: `EconomicsSnapshotId`, `LineRef`, `FundingSource` (Platform/Provider/Shared/Supplier),
  `DiscountAmount`, `RuleCode?` — **modeled now, rows only when S6 provides discounts** (narrow core = none → totals 0).
- **Reserve (do NOT build in S8):** `TravelPricingSnapshot` (S4), `PricingAttributeSnapshot` (S2) — mention as deferred.

## 3. `CreateFromLines` factory (line-first, mirrors BE-P1) — the heart
`PaymentEconomicsSnapshotEntity.CreateFromLines(contextId, currency, IReadOnlyList<LineEconomicsInput> lines,
platformFee: {ruleId?, rate, min, max, base, net, vat, gross}, customerPayableServiceAmount, ...)`:
- Each `LineEconomicsInput`: `LineRef, ItemType, PricingMethod, GrossBeforeDiscount, CustomerDiscount,
  ProviderFundedDiscount, PlatformFundedDiscount, CommissionEligibility, CommissionBase, CommissionRate, CommissionAmount,
  ProviderNet, LineVat, LineTotal, RuleId?, RuleCode?, Commissionable`. Normalise every money field through `MoneyMath.Round`
  (rate `RoundRate`) — **exactly as BE-P1**.
- **Derive aggregates as line sums** (never from a passed-in blended number):
  `OriginalServiceGross = Σ GrossBeforeDiscount`, `TotalCustomerDiscount = Σ CustomerDiscount`,
  `TotalProviderFundedDiscount = Σ ProviderFundedDiscount`, `TotalPlatformFundedDiscount = Σ PlatformFundedDiscount`,
  `TotalProviderCommission = Σ CommissionAmount`, `ProviderNetTotal = Σ ProviderNet`, `ServiceVatTotal = Σ LineVat`,
  `ΣLineTotal = Σ LineTotal`. Then map to the aggregate fields (§1) + reuse BE-P1's `CustomerTotal`/`PlatformGrossShare`
  derivations.
- **8 equalities (§20.15) — zero tolerance, exact decimal** (throw `PaymentEconomicsInvariantException` naming the failed
  one):
  ```
  Σ(line gross before discounts)      == OriginalServiceGrossAmount
  Σ(line customer discounts)          == TotalCustomerDiscount
  Σ(line provider-funded discounts)   == TotalProviderFundedDiscount
  Σ(line platform-funded discounts)   == TotalPlatformFundedDiscount
  Σ(line commission amounts)          == TotalProviderCommission (== CommissionAmountSnapshot)
  Σ(line provider net amounts)        == ProviderNetTotal        (== ProviderNetAmountSnapshot)
  Σ(line VAT amounts)                 == ServiceVatTotal
  Σ(line totals) + PlatformFeeGross   == CustomerTotalAmount
  ```
- **Also re-assert BE-P1's existing aggregate invariants** (ProviderNet+PlatformGrossShare==CustomerTotal, commission ==
  Round(base×rate) — note: at aggregate this is the SUM of per-line rounded commissions, so the aggregate
  `CommissionAmount` must equal `Σ line CommissionAmount`, NOT `Round(aggregateBase × blendedRate)`; keep BE-P1's own
  invariant satisfied by construction or relax it explicitly to the line-sum definition — **document which**, do not create
  a contradiction between BE-P1's rule and the line-sum rule).
- Returns the aggregate entity **with its line snapshot children attached** as one immutable unit. Per-line commission
  rounding from S7 is carried through unchanged (so S7 preview and S8 snapshot reconcile to the same numbers).

> **BE-P1 reconciliation (critical):** BE-P1 asserts `CommissionAmount == Round(CommissionBaseAmount × CommissionRate)`.
> With line-level rounding, the aggregate commission = Σ rounded line commissions, which may differ from
> `Round(ΣBase × blendedRate)`. Resolve this explicitly: the aggregate `CommissionRateSnapshot` becomes a **reporting-only
> effective rate** (`= CommissionAmount / CommissionBase` if base>0, rounded 4dp) and the **binding** invariant is the
> line-sum (`CommissionAmountSnapshot == Σ line commission`). Adjust BE-P1's aggregate invariant accordingly (narrow,
> documented change — this is the one BE-P1 field whose invariant S8 must reconcile).

## 4. Persistence + migration (append-only)
- New tables `payment_offer_line_economics_snapshots`, `payment_commission_allocation_snapshots`,
  `payment_discount_allocation_snapshots` (`numeric(18,4)` money / `numeric(9,4)` rate, enum `HasConversion<int>()`, FK to
  `payment_economics_snapshots`, index `(EconomicsSnapshotId)`, `(EconomicsSnapshotId, LineRef)`). New aggregate columns
  (§1) on `payment_economics_snapshots`. DbSets, EF configs, DI. Append-only; existing 19 legacy snapshots get the new
  columns nullable/0 (no backfill of line children for historic rows). Reversible, duplicate-safe.

## 5. Tests (rigorous — mirrors BE-P1's discipline)
- **8 equalities pass:** a synthetic line set {Service eligible w/ commission+vat, Travel exempt, a Product} + platform fee
  → aggregate derived = Σ lines; all 8 hold; snapshot created with children.
- **Each equality fails individually** (tamper one line) → `PaymentEconomicsInvariantException` naming that equality; no
  snapshot created.
- **Zero discounts (narrow core):** all discount totals 0; equalities still hold; no DiscountAllocation rows.
- **Line-sum commission vs blended:** per-line rounding (e.g. two lines 20.02 total) is preserved; aggregate commission ==
  Σ line (not `Round(ΣBase×blendedRate)`); reporting rate = amount/base (4dp). BE-P1 aggregate invariants still hold under
  the reconciled definition.
- **Immutability:** no public setter/mutator on any snapshot entity; children insert-only; FK OnDelete Restrict.
- **Rounding:** `MoneyMath`; zero tolerance on all 8; determinism (same lines → same snapshot values).
- **BE-P1 untouched paths:** the existing aggregate `Create(...)` still works for callers that don't use lines (or is
  superseded only where documented); existing 24 BE-P1 tests green.

## 6. Acceptance criteria
- Immutable line snapshot entities (OfferLineEconomics/CommissionAllocation/DiscountAllocation) FK'd to the single
  `PaymentEconomicsSnapshot`; aggregate totals **derived only from line sums**; **8 equalities enforced with zero
  tolerance**; travel/attribute snapshots reserved (deferred to S4/S2), discount allocation modeled but 0 in narrow core.
- BE-P1's aggregate `CommissionRate` reconciled to a reporting-only effective rate; the binding commission invariant is the
  line-sum; no contradiction between BE-P1 and S8 rules; per-line rounding carried through (S7↔S8 reconcile).
- Pure/structural factory (no acceptance wiring, no resolver calls, no checkout, no MarkApplied — all P8). Append-only
  migration; existing BE-P1 snapshot + tests intact; build clean.

## 7. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration (3 line tables + aggregate columns)
   applied.
3. DB: `\d payment.payment_offer_line_economics_snapshots` (FK + columns) + the new aggregate columns on
   `payment_economics_snapshots`.
4. Tests green (paste): 8 equalities pass, each fails individually, zero-discount, line-sum-vs-blended commission,
   immutability, rounding/determinism, BE-P1 tests still green.
5. Smoke: `CreateFromLines` on {Service 5000 @0.12 (commission 600, net 4400), Travel 800 exempt (net 800)} + platform fee
   gross → aggregate: OriginalServiceGross 5800, TotalProviderCommission 600, ProviderNetTotal 5200,
   CustomerTotal == Σ line totals + platformFeeGross; all 8 hold.

## 8. Report
`REPORT_BACKEND.md` ("BE-S8"): immutable line snapshots (OfferLineEconomics/CommissionAllocation/DiscountAllocation) +
`CreateFromLines` line-first factory + 8 zero-tolerance equalities + aggregate decomposition fields + BE-P1 commission-rate
reconciliation (reporting-only rate, line-sum binding) + migration. Note: travel snapshot = S4, attribute snapshot = S2,
discount allocation populated = S6, acceptance orchestration + resolver assembly + MarkApplied + checkout = **P8**. Next:
**BE-P8 (CalculateServiceRequestPaymentEconomics — assemble S1/S7 + P3/P5/P6/P7 at acceptance → CreateFromLines → immutable
snapshot → checkout)**. Do not touch CargoDry.
