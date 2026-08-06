# REPORT — BE_S9: line-level profit protection (before the transaction gates)

> **Repo:** `addesso-project` — implemented in the **Payment module** (SR-numbered, Payment-coded, like S7/S8), consumed at
> **P8 acceptance**. Extends the Profit-Protection Engine from transaction-level (P5) to **line-level** (§20.12): each line
> must independently clear its own floor — **a line's loss can NOT be hidden in another line's profit (no netting)**. S9 adds
> a **stricter rejection path only**; it does **not** change the numbers or the 8-equality of an offer that passes.
> **NOT committed — working tree left for review.**

## Result

- **Build:** full solution `Aizen.sln` — **0 errors**.
- **Tests:** Payment Domain **324 pass** (307 pre-existing + **17 new** S9), Payment Repository **78 pass**. The pre-S9
  suite is green **unchanged** (regression: S9 is a no-op for a passing offer).
- **Migration:** one append-only migration (`AddLineProfitProtectionS9`) — 11 `AddColumn` (all with back-compat defaults),
  1 `CreateTable`, 2 `CreateIndex`; idempotent SQL verified. No destructive op in `Up()`.

## S9a — line-level thresholds (inputs)

Per line the floor set is resolved from two cost-free sources:

- **Part lines (Product = ItemType 2 / Consumable = 9):** `providerMinimumReceivable`, `allowedProviderFundedDiscount`,
  `allowedPlatformFundedDiscount` come from the **S5 `PartLineAllowanceDto`** (`MinimumProviderReceivable`,
  `Funding.ProviderFundedAmount`, `Funding.PlatformFundedAmount`) — **consumed, never re-derived**. The confidential
  `SupplierListPrice` / `ProviderDealerMargin` never leave Payment (S5's cost-free projection is reused verbatim).
- **Non-part lines:** from the **versioned `ProfitProtectionPolicyEntity`**, extended with 8 admin-tunable fields (NO
  hardcoded constant in the engine):
  `DefaultLineMinProviderReceivableRate` / `…Amount`, `DefaultAllowedProviderFundedDiscountRate`,
  `DefaultAllowedPlatformFundedDiscountRate`, `LineCommissionFloorRate`, `MinLinePlatformContributionRate`,
  `StrategicLossExceptionEnabled` (default **false**), `StrategicLossExceptionMaxLineDeficit` (the versioned limit).

The policy fields are threaded through the whole admin surface: entity `Create`/`Update`/`Validate` (optional trailing
params → no caller break), DTO + mapper (read), Create/Update command + handler (write, admin-tunable), EF configuration,
seed, and the migration. **Launch defaults are a NO-OP**: caps 100% (rate `1`), floors `0`, min line contribution `0`,
exception off — so an offer whose lines already clear is **byte-identical to pre-S9**. Migration column defaults backfill
existing rows to the same no-op; the seed sets them explicitly for a fresh DB (**idempotent reseed** — the seed still
skips an existing row, and the migration default has already backfilled it).

## S9b — pure `LineProfitProtectionEngine` (Payment domain)

`LineProfitProtectionEngine.Evaluate(lines, policy, partAllowances, transactionPlatformFeeNetRevenue,
transactionProviderCommissionBenefitCost)` → per-line `LineProfitProtectionResult` + an overall
`LineProfitProtectionEvaluation` (Approved / Rejected / ConfigurationError). **Pure, deterministic, exact decimal
(`MoneyMath`), no persistence.** Per line, in order (first breach fails the line):

1. **Provider min-receivable** — `ProviderNet ≥ providerMinimumReceivable` (part: S5; else `Max(policyAmount, lineBase ×
   policyRate)`). Breach → `LineProfitProtectionProviderReceivableBelowFloor` (**5131**).
2. **Funded-discount caps** — `ProviderFundedDiscount ≤ allowedProviderFundedDiscount` **and** `PlatformFundedDiscount ≤
   allowedPlatformFundedDiscount`. Breach → `LineProfitProtectionFundedDiscountExceedsCap` (**5132**).
3. **Commission floor** — `ResolvedRate ≥ policy.LineCommissionFloorRate`. Breach **reuses the P7
   `ProviderCommissionBelowFloor` contract (5078)** — not duplicated.
4. **Line platform contribution (negative-contribution ban)** —
   `linePlatformContribution = CommissionNetRevenue + proRataPlatformFeeNetRevenue − PlatformFundedDiscount −
   proRataProviderCommissionBenefitCost ≥ minLinePlatformContribution`. The transaction platform fee net and provider-
   commission-benefit cost are attributed **pro-rata by line commission base** (documented allocation; reporting-only —
   it does **not** alter the transaction fee). Breach → **fail unless** `StrategicLossExceptionEnabled` **and** the deficit
   `≤ StrategicLossExceptionMaxLineDeficit` (then allowed + flagged, never silent). Hard breach →
   `LineProfitProtectionNegativeContribution` (**5133**).

Missing policy, or a part line with no resolved S5 allowance → `LineProfitProtectionConfigurationError` (**5134**).
**No netting:** each line is judged on its own — a positive line never offsets a failing one; any breach → overall fail
with a per-line reason.

## S9c — wiring (order: line then transaction)

`ServiceRequestPaymentEconomicsCalculationService` (P8 combiner orchestrator) now resolves the **cost-free S5 allowance
per part line** (mirrors `ResolvePartLineAllowancesQueryHandler`: load active terms once → `PartCommercialTermResolver`
→ project the cost-free floors only), marks each combiner line `IsPartLine`, and threads the allowance map into the pure
`ServiceRequestEconomicsCombiner.Combine`.

Inside the combiner the line gate runs **as a stricter rejection that a transaction-level pass cannot override**. On a
line breach → short-circuit to **Rejected** (or **ConfigurationError**) with the per-line reasons → the existing P8 gate
produces **no snapshot / no escrow** and SR rolls the acceptance back. On a pass → the transaction economics proceed
exactly as today.

**Design note — which numbers the line gate judges.** The line gate is evaluated on the **final committed** per-line
economics (identical to the first pass when there is no adjustment; equal to the safe-max-recomputed lines on an
`ApprovedWithAdjustment`). This is required to honour both spec constraints simultaneously: (a) "no netting" — a line loss
on the committed numbers is rejected regardless of transaction-level netting; and (b) "must not change an offer that
passes" — the P8b **safe-max discount adjustment** (§19.10) is a legitimate *pass* (`ApprovedWithAdjustment`), and judging
the line on the pre-adjustment discount (which is never committed) would wrongly reject it. Because the adjustment only
**lowers** the platform-funded discount (→ higher `ProviderNet`, smaller funded discounts), evaluating on the final numbers
is strictly *more lenient* than the pre-adjustment state and never rejects an offer that should pass, while still catching
every genuine line loss. The regression proof below and the previously-failing `P5_UnsafeDiscount_AdjustsToSafeMax` test
(now green) confirm this.

New error codes (Payment 51xx): **5130** `LineProfitProtectionRejected` (aggregate), **5131** provider-receivable,
**5132** funded-cap, **5133** negative-contribution, **5134** configuration-error; commission-floor **reuses 5078**.

## S9d — per-line record

- **On approval:** the immutable S8 `OfferLineEconomicsSnapshotEntity` gains three **descriptive** columns —
  `LineMinProviderReceivableApplied`, `LinePlatformContribution`, `LineProfitProtectionPassed` — folded on by
  `CreateFromLines`. Self-contained; **enters no sum or invariant** (the 8-equality is untouched).
- **On non-Approved:** an insert-only **`LineProfitProtectionEvaluationLog`** (mirrors P5's log, which records the
  non-Approved decision without a snapshot) is written by the escrow handler on a line-level failure — SR / offer /
  currency / decision / failing-line count + refs / primary breach code / reason. On failure **no snapshot is created**, so
  this log is the record.

## No-netting proof (the headline invariant)

Unit test `Netting_ProfitableLinePlusLossLine_StillRejected` (engine) + `Netting_TransactionAggregatePasses_ButLineFails`
(combiner): a profitable line (contribution +600) and a line below its own floor are evaluated together. The **transaction
aggregate passes** (the +600 covers the shortfall), yet S9 **Rejects** because the failing line does not clear its own
floor — the positive line's `Passed = true`, the failing line's `Passed = false`. A profitable line cannot rescue a loss
line.

## Regression proof (a passing offer is byte-identical)

- The entire pre-S9 Payment suite (324 domain incl. the P8/P8b combiner + snapshot + P5 tests) passes **unchanged** — S9 is
  a no-op for offers whose lines already clear (no-op policy defaults).
- `AllLinesClear_Approved_AmountsUnchanged_RecordAdded`: the happy path keeps `OriginalServiceGross 5800 / Commission 600 /
  ProviderNet 5200 / CustomerTotal 5974`, the 8-equality structural checks hold, and the new descriptive columns are
  populated (`Passed = true`, `LinePlatformContribution = 745` for the commissionable line, `0` for the exempt line) —
  additive only.

## Tests (17 new)

| # | Test | Asserts |
|---|---|---|
| 1 | `AllLinesClear_Approved` (engine) + `AllLinesClear_Approved_AmountsUnchanged_RecordAdded` (combiner) | all clear → Approved; amounts + 8-equality unchanged; S9 record added |
| 2 | `PartLine_BelowS5MinReceivable_Rejected_NoSnapshot` | part line `ProviderNet 880 < S5 min 1000` → Rejected, no snapshot, code 5131, per-line reason |
| 3 | `ProviderFundedDiscountOverCap_Rejected` / `PlatformFundedDiscountOverCap_Rejected` | funded discount over cap → Rejected, 5132 |
| 4 | `CommissionRateBelowFloor_Rejected_ReusesP7Code` | below commission floor → Rejected, **reuses 5078** |
| 5 | `Netting_ProfitableLinePlusLossLine_StillRejected` + combiner netting | profit line + loss line net-pass at transaction level → **still Rejected** at line level |
| 6 | `StrategicLossException_{EnabledWithinLimit_Allowed, Disabled_Rejected, OverLimit_Rejected}` | loss line allowed+flagged within limit; rejected when off / over limit |
| 7 | `NullPolicy_ConfigurationError` + `PartLine_{NoResolvedAllowance→ConfigurationError, MissingAllowance}` | missing policy / part allowance → ConfigurationError, no snapshot |

Plus `NegativeLineContribution_Rejected`, `PartLine_UsesS5AllowanceFloor` (part line takes the S5 floor, not the policy).

## Verify (per the doc)

1. ✅ An offer with all lines above their floors accepts with the same economics as before S9 (regression — 324 pre-S9
   tests unchanged + explicit byte-identical assertion).
2. ✅ A part line below its S5 `MinimumProviderReceivable` → acceptance Rejected, no escrow/snapshot, per-line reason
   surfaced (combiner test + the escrow-handler early-return path writes the log and returns `Decision = Rejected`).
3. ✅ A profitable line cannot rescue a loss-making line — a mix that net-passes at transaction level is Rejected at line
   level unless a versioned strategic-loss exception is active (netting + strategic-loss tests).
4. ✅ Commission-floor / funded-cap breaches reject with distinct codes (5131/5132, commission-floor reuses 5078).
5. ✅ The transaction-level gates + the 8-equality for a passing offer are unchanged (regression).

**On-stack note:** the domain + wiring are exhaustively unit-tested and the full solution builds; the end-to-end live
acceptance round-trip (SR accept → Payment P8 → escrow rollback on a line breach, on the running stack) is the remaining
on-environment smoke and was not run here (no live infra in this pass).

## Files touched (all in the Payment module)

- **Abstraction:** `PaymentErrorCode` (+5130–5134).
- **Domain:** `ProfitProtectionPolicyEntity` (8 line fields + Create/Update/Validate); new `LineProfitProtection.cs`
  (value objects) + `LineProfitProtectionEngine.cs`; new `LineProfitProtectionEvaluationLogEntity`;
  `ServiceRequestEconomicsCombiner` (line gate + `partAllowances` + `IsPartLine` + result fields);
  `LineEconomicsInput` + `PaymentEconomicsSnapshotEntity.CreateFromLines` + `OfferLineEconomicsSnapshotEntity` (3
  descriptive columns); repository interfaces.
- **Application:** `ServiceRequestPaymentEconomicsCalculationService` (resolve S5 allowances + wire); P5 policy DTO +
  mapper + Create/Update command + handler; `CalculateServiceRequestEconomicsCommandHandler` (line log on non-Approved).
- **Repository:** policy + offer-line snapshot + new-log EF configurations; seed line defaults; `PaymentDbContext` DbSet;
  repository impl + DI; migration `AddLineProfitProtectionS9`.
- **Tests:** `LineProfitProtectionEngineTests`, `ServiceRequestEconomicsCombinerS9Tests`.

## Next

- **FE:** admin policy line-level fields (the 8 new `ProfitProtectionPolicyDto` fields are already exposed for the admin
  panel) + provider-facing rejection-reason surfacing for the S9 line breaches.
- **Next SR phase:** S12 recurring / S13 dispute.
