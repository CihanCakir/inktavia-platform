# BE-P8 — `CalculateServiceRequestPaymentEconomics` (acceptance-time economy core → immutable snapshot → escrow) — Backend Prompt

> **Module:** `Aizen.Modules.Payment` (domain service + internal remote-call), invoked by `Aizen.Modules.ServiceRequest`
> `AcceptServiceRequestOfferCommandHandler` at offer acceptance. **Phase:** Payment P8 — **the convergence of P1–P7 + SR
> S1/S7/S8.**
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §19.8 (one combiner, independent resolvers), §19.9 (12-step
> binding calc order), §19.11 (decision final-before-checkout, no silent change), §20.16 (16-step flow), §5/§13.4
> (immutable snapshot), §10 (iyzico split — hardened in P9).
> **Rule:** ORCHESTRATE existing pieces; do NOT rewrite any resolver. Reuse: S7 line-commission resolver, P3
> `PlatformFeeRule` resolver + `ApplyPlatformFeeRule`, P5 `ProfitProtectionEngine`, P2 `MarkAppliedAsync`, S8
> `CreateFromLines`, BE-P4 provider-plan resolution, the escrow remote-call pattern. Inspect first.
> **Narrow-core scope (the user deferred S6/S2–S5 + P7-application):** customer discount (P6) + its line funding
> allocation (S6) and provider commission-benefit APPLICATION (P7) are **consumed as context inputs = 0 / base** in P8 now
> (exactly as P5 was built with 0 inputs; benefits are OFF by default per BE-P7 seed). The resolvers exist; wiring their
> non-zero application into P8 is the immediate fast-follow once S6 lands. P8 fully wires: **plan resolution → S7 line
> commission → P3 platform fee → P5 profit-protection gate → S8 immutable snapshot → escrow/checkout initiation +
> MarkApplied.** Do NOT compute discounts/benefits at aggregate without line allocation (would break S8's 8 equalities).

## 0. Verified current state
- `AcceptServiceRequestOfferCommandHandler` (SR): `offer.Accept()` → `sr.ChangeStatus(OfferAccepted)` → calls
  `IPaymentModuleRemoteCall.CreateEscrowAsync({GrossAmount=offer.TotalAmount, DiscountAmount=0, ProviderPlanId=null,
  CategoryCode=sr.ServiceCategoryCode, PayerProfileId=sr.OwnerUserId, RecipientProfileId=offer.ProviderProfileId})`.
  **Escrow failure currently logs and continues (offer stays accepted).** ← P8 changes this for the gate (below).
- Escrow gross today = `offer.TotalAmount` (the provider price). **Marketplace economics require** the customer to be
  charged `CustomerTotal = CustomerPayableServiceAmount + PlatformFeeGross` and the split `subMerchantPrice = ProviderNet`
  — P8 produces these; the iyzico split hardening/verification is P9.
- Ready pieces: S7 `ResolveLineCommissions` (per-line base/rate/amount/providerNet, Σ=transaction, PURE, plan passed in),
  P3 platform-fee resolver + `ApplyPlatformFeeRule` (base = CustomerPayableServiceAmount), P5 `ProfitProtectionEngine`
  (context-in → decision-out, three gates + safe-max), S8 `CreateFromLines` (line snapshots + 8 equalities, immutable),
  P2 `MarkAppliedAsync` (applied-count — **this is the acceptance moment**), BE-P4 provider-plan resolution,
  `PaymentEconomicsSnapshot` + `transactions.EconomicsSnapshotId` FK (BE-P1).
- **S7 note (binding here):** the S7 resolver is pure and takes `ProviderPlanId` as input. **P8 MUST resolve the provider's
  ACTIVE plan authoritatively and pass it in** — do not pass null and rely on dynamic fallback.

## 1. The P8 domain service (§19.8/§19.9) — one combiner, independent resolvers
`Application/Services/ServiceRequestPaymentEconomicsCalculationService.cs`
`CalculateAsync(input) → PaymentEconomicsCalculationResult` — pure orchestration over the resolvers (no resolver mutates
another's output; §19.8). `input`: `ContextId (SR+offer)`, `ProviderProfileId`, `CustomerProfileId`, `CustomerPlanId?`,
`CategoryCode?`, `CurrencyCode`, and the **offer line set** (per line: LineRef, ItemType, PricingMethod,
CommissionEligibility, `LineGrossBeforeDiscount`, `LineVat`, `LineProviderRevenue`, `CommissionBaseAmount`). Binding order
(§19.9, narrow-core specialization):
1. `OriginalServiceGrossAmount = Σ LineGrossBeforeDiscount`.
2–4. **Customer discount + funding (P6):** narrow core → `TotalCustomerDiscount = 0`, per-line customer/provider-funded/
   platform-funded discount = 0 (S6 fills later). (Structure the call so plugging P6/S6 later is additive.)
5. Platform-funded-discount safe cap (P5 §19.10) — trivially 0 now.
6. `ProviderEconomicServiceAmount = OriginalServiceGrossAmount − ProviderFundedDiscount(0)`.
7. **Resolve provider ACTIVE plan** (BE-P4) → `ProviderPlanId`. Then **S7 line commission** with that plan → per-line
   `{Commissionable, CommissionBase, ResolvedRate, RuleId/Code, CommissionAmount, ProviderNet}`, `TransactionCommission =
   Σ`, `TransactionProviderNet = Σ`.
8–9. **Provider commission benefit (P7):** narrow core → benefit = 0 (OFF by default); effective rate = base rate; keep the
   seam to apply per-line P7 effective rate later. Assert each `EffectiveRate ≥ plan floor` (already true at base).
10. Compute (MoneyMath, §19.9-10):
    `CustomerPayableServiceAmount = OriginalServiceGrossAmount − TotalCustomerDiscount(0)`;
    `PlatformFeeBaseAmount = CustomerPayableServiceAmount`; resolve **P3** → `PlatformFee{net,vat,gross}` via
    `ApplyPlatformFeeRule`; `CustomerTotalAmount = CustomerPayableServiceAmount + PlatformFeeGross`;
    `PlatformGrossShare = TransactionCommission + PlatformFeeGross = CustomerTotal − ProviderNetTotal`.
11. **Expected expenses + three contributions** → build `ProfitProtectionContext` (ServiceAmount, CustomerPayable,
    CustomerTotal, ProviderNet, ProviderCommissionNetRevenue=TransactionCommission, CustomerPlatformFeeNetRevenue=
    PlatformFeeNet, discount/benefit/budget inputs = 0) → run **P5 `ProfitProtectionEngine`** → `decision`.
12. **Gate (§19.9-12, zero tolerance):** BE-P1 aggregate invariants **+** the three profit-protection gates. If decision
    `Rejected`/`ConfigurationError` → **do NOT create snapshot, do NOT create escrow, do NOT accept** (return the decision
    + reason). If `ApprovedWithAdjustment` → use the adjusted amounts (narrow core: no advantage to reduce, so effectively
    Approved). If `Approved` → proceed.
- On success: call **S8 `CreateFromLines`** (line inputs assembled above + platform fee) → immutable
  `PaymentEconomicsSnapshot` + line snapshots (8 equalities enforced there — a second, structural guarantee). Result:
  `{ Decision, SnapshotId?, CustomerTotalAmount, ProviderNetTotal, PlatformFeeGross, TransactionCommission, Reason? }`.

## 2. Acceptance wiring (SR → Payment) + escrow using snapshot amounts
- Extend the internal remote-call surface (mirror `IPaymentModuleRemoteCall`): a single
  `CalculateAndCreateEscrowAsync(request)` (or extend `CreateEscrowAsync`) that, in ONE idempotent Payment operation:
  runs §1 `CalculateAsync` → on `Approved/ApprovedWithAdjustment` creates the escrow transaction with **gross =
  CustomerTotalAmount**, **split target = ProviderNetTotal**, links `transaction.EconomicsSnapshotId = SnapshotId`
  (BE-P1 settable-once FK), calls P2 `MarkAppliedAsync` for the resolved commission rules; returns `{Decision,
  TransactionId?, SnapshotId?, CustomerTotal, ProviderNet, Reason?}`. On `Rejected/ConfigurationError` → no transaction,
  return the decision.
- Request carries the offer **line set** + provider/customer/category/currency (SR maps from the offer; LineType as raw SR
  enum int, as S8 stores). Typed DTOs (never `object`).
- **Idempotency (§8):** key `SR-{srId}-OFFER-{offerId}` — a retry returns the existing snapshot/transaction, never a second
  snapshot or double MarkApplied (immutable snapshot + FK + processed-key guard).
- **SR handler change (behavior):** run P8 BEFORE committing acceptance side-effects, OR compensate: if P8 returns
  `Rejected/ConfigurationError`, the offer is **not** left "accepted with no escrow" — surface the reason and block
  acceptance (replace today's "log and continue"). `ApprovedWithAdjustment` proceeds with the snapshot's amounts (§19.11
  final-before-checkout — the customer/provider see the snapshot figures; no silent later change).
- **Do NOT start the real iyzico auth/split here** — escrow creation uses the existing gateway path; the auth-mode policy +
  `item/approve` split + sandbox split verification are **P9**. P8 = economics + snapshot + escrow record with correct
  amounts.

## 3. Provider active-plan resolution (S7→P8 requirement)
Resolve the provider's active plan subscription (BE-P4 domain) at acceptance → its `ProviderPlanId`; pass to the S7
resolver so commission uses the provider's real plan rate (FREE 15 / STANDARD 12 / PREMIUM 9), not a default. If no active
subscription → the FREE/default rule resolves via CommissionRule global (document; no crash).

## 4. Persistence / migration
- Likely **no new tables** (snapshot + line tables from BE-P1/S8; transaction FK from BE-P1). Only: the P8 service, the
  remote-call method + typed DTOs, internal controller→MediatR command, the SR handler wiring, DI. If a `Processed
  acceptance-economics key` guard needs a row, reuse the existing idempotency/`ProcessedGatewayEvent` mechanism rather than
  a new table. Any migration must be append-only.

## 5. Tests (rigorous — the convergence)
- **Happy path:** offer {Service 5000 eligible, Travel 800 exempt}, provider on STANDARD (0.12) → plan resolved →
  commission 600, providerNet 5200; platform fee via P3 (e.g. %2.5 min99 → 125 gross on 5000, clamped per rule);
  `CustomerTotal = 5000 + platformFeeGross`; P5 gates pass → snapshot created (8 equalities hold) → escrow gross =
  CustomerTotal, split = 5200, `transaction.EconomicsSnapshotId` linked; MarkApplied incremented once.
- **Plan resolution:** provider on PREMIUM → rate 0.09 used (not default); no active sub → global/FREE rule; assert the
  rate that flows into commission.
- **Profit-protection reject:** a context that fails a gate → Decision `Rejected`, **no snapshot, no escrow**, offer not
  left half-accepted; reason returned.
- **ConfigurationError:** no active profit-protection policy → `ConfigurationError`, no snapshot/escrow.
- **Idempotency:** same `SR-x-OFFER-y` twice → one snapshot, one transaction, MarkApplied once.
- **Amounts to gateway:** escrow gross == CustomerTotal (NOT offer.TotalAmount raw when a platform fee applies); split ==
  ProviderNet.
- **8 equalities via S8:** the assembled line set satisfies all 8 (structural double-check).
- **No resolver mutation (§19.8):** discount resolver doesn't change commission and vice-versa (narrow core: both 0, but
  assert the call order + independence).
- **Purity of calc:** `CalculateAsync` computes deterministically; MarkApplied + persistence happen only in the
  create-escrow path, not in a pure preview call.

## 6. Acceptance criteria
- Single Payment combiner runs the §19.9 order over independent resolvers; provider active plan resolved authoritatively
  and fed to S7; platform fee via P3 on `CustomerPayableServiceAmount`; P5 three gates + BE-P1 invariants enforced
  zero-tolerance; **on failure no snapshot/escrow and the offer is not left half-accepted**; on success one immutable
  snapshot (8 equalities) linked to the transaction, escrow gross = CustomerTotal, split = ProviderNet, MarkApplied once,
  idempotent.
- Narrow-core: customer discount (P6) + line funding (S6) + commission-benefit application (P7) consumed as 0/base with an
  additive seam for the fast-follow; iyzico auth/split hardening deferred to P9; no CargoDry. Build clean; existing escrow
  path + all prior tests green.

## 7. Verify — run and PASTE output
1. `dotnet build` Payment + ServiceRequest: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api service-request-api`; clean boot; migration (if any)
   append-only applied.
3. Tests green (paste): happy path (amounts + snapshot + FK + MarkApplied-once), plan resolution (STANDARD/PREMIUM/none),
   profit-protection reject (no snapshot/escrow), ConfigurationError, idempotency (one snapshot/txn), gateway amounts
   (CustomerTotal/ProviderNet), 8 equalities, resolver independence, calc purity.
4. Smoke: accept an offer end-to-end in sandbox data → `transaction.EconomicsSnapshotId` set; snapshot CustomerTotal ==
   escrow gross; provider net == split; a rejecting context → offer acceptance blocked with reason.

## 8. Report
`REPORT_BACKEND.md` ("BE-P8"): `CalculateServiceRequestPaymentEconomics` combiner (§19.9 order: plan → S7 commission → P3
fee → P5 gate → S8 snapshot), acceptance wiring (idempotent calculate-and-create-escrow with CustomerTotal gross /
ProviderNet split / snapshot FK / MarkApplied-once), provider active-plan resolution, reject/ConfigError blocks acceptance.
Note: customer discount (P6) + line funding (S6) + commission-benefit application (P7) wired as 0/base now (additive seam);
iyzico auth-mode + item/approve split + **sandbox split verification = P9 (production gate)**; refund/chargeback = P10.
Next: **BE-P9 (iyzico gateway auth-mode + item-level approve/split + sandbox split test = PRODUCTION GATE)**. Do not touch
CargoDry.
