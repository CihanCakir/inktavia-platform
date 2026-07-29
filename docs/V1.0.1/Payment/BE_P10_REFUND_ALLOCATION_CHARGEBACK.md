# BE-P10 — RefundAllocationPolicy + release-before/after + ProviderBalance/negative-balance + chargeback/clawback + benefit restore — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P10 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §7 (refund allocation + release-before/after + negative
> balance + §7.5 record), §13.5 (provider recovery), §19.15 (benefit restore runs ONCE), §21.2 (chargeback 13-month
> clawback).
> **Rule:** EXTEND the existing refund infra; do NOT rewrite it. Reuse the immutable `PaymentEconomicsSnapshot` (P1/S8) as
> the **source of truth for every reversal amount** (never recompute), `MoneyMath`, fail-loud + versioned-policy +
> reserve/consume/release patterns. Inspect first.
> **Non-goal:** line-level partial refund allocation beyond pro-rata (fast-follow); the dispute case UI/workflow (SR S13);
> live gateway refund call is P9-fix-corrected already (`/payment/refund` item `paymentTransactionId` + reason) — P10 wires
> the allocation + recovery around it. Do NOT touch CargoDry.

## 0. Verified current state (extend these)
- `PaymentTransactionEntity`: `TotalRefundedAmount`, `RemainingRefundableAmount (= Gross − TotalRefunded)`, `LastRefundedAt`,
  `DisputedAt`, `DisputeResolution`, `RefundRecords` collection, `ApplyRefund(record)`, `ReverseRefund(...)`,
  `EconomicsSnapshotId` FK (P1).
- `TransactionRefundRecord` (RefundCode `REF-YYYYMMDD-XXXX`, `RefundType`, `RefundReason`, `TransactionRefundStatus`,
  `GatewayRefundReference`, `Create`, `MarkProcessed`). `GetTransactionRefundHistoryQuery` exists.
- P9-fix: gateway `RefundAsync` now sends the item `paymentTransactionId` + `reason ∈ {OTHER,FRAUD,BUYER_REQUEST,
  DOUBLE_PAYMENT}` + `retryable`/response-signature.
- `CustomerBenefitBudgetPolicy.RefundRestorePolicy` (BenefitRefundRestorePolicy Restore/Consume) + `CustomerBenefitBudgetService`
  reserve/consume/release (P6); `CommissionBenefitEntitlementService` usage (P7); `ProfitProtectionPolicy.RefundRiskReserveRate`.
- **Missing (this phase):** the allocation POLICY + the 9-amount §7.5 breakdown, release-before vs release-after handling,
  ProviderBalance/negative-balance ledger + clawback + future-payout offset + PlatformAdvancedRefundAmount, chargeback, and
  the **once-only** benefit restore.

## 1. Enums + `RefundAllocationPolicy` (versioned) — §7.1
- `RefundCause { ProviderCancelled, CustomerCancelledBeforeWork, CustomerCancelledAfterWorkStarted, DisputeCustomerFavoured,
  DisputeProviderFavoured, TechnicalFailure, DuplicatePayment, AdministrativeCorrection }` (reconcile with the existing
  `RefundReason` — map/extend, don't duplicate).
- `ReleaseState { BeforeProviderRelease, AfterProviderRelease }` (derive from the transaction: has the escrow been
  approved/released — the §6 transactionStatus 2 / payout record exists).
- `PlatformFeeRefundMode { Full, ProRata, None, FixedAmount, RuleBased }`.
- `RefundAllocationPolicyEntity` (versioned, single-active per currency, admin-configurable, NO baked constants): per
  `RefundCause` → service-refund rule + `PlatformFeeRefundMode` (default MVP table §7.2: ProviderCancelled/TechnicalFailure/
  DuplicatePayment → service Full + fee Full; CustomerCancelledBeforeWork → per cancellation policy + fee Full;
  CustomerCancelledAfterWorkStarted → approved amount + fee RuleBased; DisputeCustomerFavoured → decided amount + fee
  RuleBased/Full; DisputeProviderFavoured → none/decided + fee None). `RefundRiskReserveRate` interplay noted.

## 2. Allocation calculator (pure, snapshot-driven) — §7.5
`ResolveRefundAllocation(snapshot, refundServiceAmount, cause, releaseState, policy) → RefundAllocation` producing the
**9 §7.5 amounts** from the immutable snapshot proportions (never recompute rates):
`ServiceRefundAmount, ProviderNetReversalAmount, CommissionRevenueReversalAmount, PlatformFeeNetRefundAmount,
PlatformFeeVatRefundAmount, PlatformFeeGrossRefundAmount, GatewayRefundExpenseAmount, ProviderRecoveryAmount,
PlatformAdvancedRefundAmount, RemainingProviderNegativeBalance`.
- Proportional reversal: `ProviderNetReversal = snapshot.ProviderNet × (refundService / snapshot.ServiceAmount)` (pro-rata),
  `CommissionRevenueReversal = snapshot.Commission × sameRatio`; platform-fee refund per `PlatformFeeRefundMode` on the
  snapshot fee net/vat/gross. `MoneyMath` rounding, **reverse from the snapshot so parts sum exactly**.
- **Binding invariant (§7.5, zero tolerance):** `total refund sent to gateway == ServiceRefundAmount + PlatformFeeGrossRefundAmount`
  (the customer-facing refund) and the internal breakdown sums are consistent; throw `RefundAllocationMismatch` otherwise.
- Gateway refund expense is a **separate expense**, not deducted from the customer refund.

## 3. Release-before path (§7.2)
Provider not yet released (`BeforeProviderRelease`): no settlement created; cancel the refunded provider-net portion;
reverse commission revenue proportionally; platform-fee refund per policy; record `GatewayRefundExpenseAmount`. Extend
`ApplyRefund` to carry the `RefundAllocation`. Idempotent.

## 4. Release-after path + provider recovery order (§7.3) + ProviderBalance (§7.4)
Provider already paid (`AfterProviderRelease`): recover in order — (1) gateway/sub-merchant refund capability (if the
window allows), (2) provider's unsettled available balance, (3) offset from next provider payouts, (4) create
`ProviderNegativeBalance`, (5) manual collection flag. If the customer must be refunded immediately before recovery →
record `PlatformAdvancedRefundAmount` (Inktavia's receivable from the provider, auto-offset from future settlements).
- **`ProviderBalanceEntity` / negative-balance ledger** (separate ledger, §7.4): `ProviderProfileId`, `Currency`, `Balance`
  (can go negative), movements (clawback/offset/manual-adjust, audited). **Offset before any new payout** (next settlement
  closes negative balance first). `NegativeBalanceLimit` exceeded → per risk policy, block payouts and/or new offer
  acceptance (surface to BE-P8 acceptance gate + payout flow); admin manual override requires audit log.

## 5. Benefit/entitlement restore (once) — §19.15
On a refund, restore the customer benefit budget + commission entitlement usage **exactly once** per refund
(idempotent — a duplicate webhook/retry must not double-restore), per `CustomerBenefitBudgetPolicy.RefundRestorePolicy`
(Restore → give back the platform-funded portion; Consume → keep consumed). Use the reserve/consume/release +
unique-constraint concurrency (P6/P7). Provider-funded discount reversal follows the provider recovery path, not the
platform budget.

## 6. Chargeback (§21.2)
- `ChargebackExpense` as a distinct expense line (reporting = P12). A chargeback (up to **13 months** post-transaction) →
  the **release-after recovery path** (§4) against the provider (clawback → negative balance → future offset), plus the
  `ChargebackExpense` record. Mark `DisputedAt`/status. Idempotent on the gateway chargeback reference.
- Reserve/collateral policy = **open decision** (leave a policy hook, do not bake).

## 7. Persistence / migration (append-only)
New: `refund_allocation_policies`, `refund_allocations` (9-amount breakdown, FK to the refund record + snapshot),
`provider_balances` + `provider_balance_movements`, `chargeback_records`. Extend `TransactionRefundRecord` with the
allocation FK + `RefundCause`/`ReleaseState`. `PaymentErrorCode` additions (next free block, e.g. 5100+):
`RefundAllocationMismatch`, `RefundAllocationPolicyConflict`, `ProviderNegativeBalanceLimitExceeded`,
`RefundRestoreAlreadyApplied`, `ChargebackAlreadyProcessed`. DbSets, EF configs (numeric(18,4)/(9,4), indexes, unique
codes), DI. Append-only; seed a default RefundAllocationPolicy (MVP §7.2 table, values documented as admin-tunable).

## 8. Tests
- **Allocation from snapshot:** provider-cancel full refund → ProviderNetReversal + CommissionReversal + PlatformFeeFull
  sum to the snapshot; total sent == ServiceRefund + PlatformFeeGrossRefund; mismatch → `RefundAllocationMismatch`.
- **Release-before:** no settlement; provider net + commission reversed; fee per policy; gateway expense separate.
- **Release-after recovery order:** unsettled balance → next-payout offset → negative balance → PlatformAdvancedRefund;
  `ProviderBalance` goes negative; next payout closes it first; `NegativeBalanceLimit` exceeded → payout/acceptance blocked.
- **Partial + pro-rata:** partial refund reverses proportional provider net/commission/fee (pro-rata); `RemainingRefundable`
  decrements; multiple partials sum correctly.
- **Benefit restore once:** refund restores platform-funded budget + entitlement per policy; duplicate refund/webhook →
  `RefundRestoreAlreadyApplied` (no double restore).
- **Chargeback:** 13-month chargeback → recovery path + `ChargebackExpense`; idempotent on reference.
- **Dispute causes:** DisputeCustomerFavoured / DisputeProviderFavoured allocate per policy (decided amount / none).
- **Idempotency & snapshot-truth:** all amounts derived from the immutable snapshot; refund idempotent by
  cause+reference; rounding zero-tolerance.

## 9. Acceptance criteria
- Refund allocation is **snapshot-driven** (9 §7.5 amounts, total == gateway refund, zero tolerance); release-before and
  release-after handled distinctly; provider recovery follows the §7.3 order; `ProviderBalance` negative-balance ledger
  offsets before new payout with a `NegativeBalanceLimit` guard wired into payout + BE-P8 acceptance; benefit/entitlement
  restore runs **once** per policy; chargeback (13-month) uses the recovery path + `ChargebackExpense`; all versioned/
  admin-configurable, no baked constants.
- Existing `TransactionRefundRecord`/`ApplyRefund`/gateway `RefundAsync` extended not rewritten; append-only migration +
  default policy seed; build clean; existing refund-history + P8/P9 paths green. No CargoDry.

## 10. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + policy seed applied.
3. DB: `SELECT "Cause","PlatformFeeRefundMode" FROM payment.refund_allocation_policies;` + `provider_balances` +
   `refund_allocations` tables present.
4. Tests green (paste): allocation-from-snapshot + total-equality, release-before, release-after recovery order + negative
   balance + limit block, partial pro-rata, benefit-restore-once, chargeback, dispute causes, idempotency.
5. Smoke: full provider-cancel refund on a snapshot → 9 amounts sum + gateway total matches; a release-after refund →
   ProviderBalance negative + PlatformAdvancedRefund + next-payout offset.

## 11. Report
`REPORT_BACKEND.md` ("BE-P10"): RefundAllocationPolicy + snapshot-driven 9-amount allocation + release-before/after +
ProviderBalance negative-balance ledger + recovery order + PlatformAdvancedRefund + benefit-restore-once + chargeback
(13-month) clawback + migration/seed. Note: line-level partial allocation beyond pro-rata = fast-follow; reserve/collateral
policy = open decision; reporting lines (ChargebackExpense etc.) = P12; dispute case workflow = SR S13; live gateway refund
verified at the P9 sandbox gate. Next: **BE-P11 (premium OFFER_BOOST_7D)** or **BE-P12 (reporting)** or the live P9 gate.
Do not touch CargoDry.
