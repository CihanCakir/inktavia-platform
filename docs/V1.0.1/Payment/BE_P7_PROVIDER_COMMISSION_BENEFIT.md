# BE-P7 — `ProviderCommissionBenefitRule` + `Entitlement` (two-stage effective rate, min floor, stackable/exclusive, GMV/usage caps) — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P7 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §19.4 (boost ≠ commission benefit), §19.5 (two-stage
> resolution + 9 binding controls), §6 (base commission specificity — BE-P2). Produces the P5-engine input
> `ProviderCommissionBenefitCost` and the **effective** commission rate that P8 will apply.
> **Rule:** EXTEND on top of BE-P2's `CommissionRuleResolver`/`CommissionResolution`; do NOT rewrite it and do NOT touch
> the base-rate resolution. Reuse BE-P2..P6 versioned/effective-date + fail-loud + `MoneyMath` + resolver +
> reserve/consume/release (BE-P6 budget) conventions. Inspect first.

## 0. Verified current state
- **No `ProviderCommissionBenefit` infra → create.** (`ProfitProtectionEvaluationLog` already references
  `RequestedCommissionBenefitCost`/`AppliedCommissionBenefit`, and `ProfitProtectionContext.ProviderCommissionBenefitCost`
  already exists — P7 is the producer of that number; the P5 engine is the consumer.)
- BE-P2 `CommissionRuleResolver.ResolveAsync → CommissionResolution(RuleId, RuleCode, RuleType, Rate, SpecificityRank,
  Priority, Source)` gives the **base** rate. P7 layers a **second stage** on top; it must NOT modify BE-P2.
- `PaymentErrorCode`: 5060–5068 used by P6. **P7 uses 5070+.**
- No premium/boost `OFFER_BOOST_7D` entitlement exists yet (that is P11). P7's entitlement is a **separate, commission-only**
  entitlement and must not be conflated with the premium/boost entitlement.
- `MoneyMath` (BE-P1), `RoundRate` (4dp), fail-loud conflict + single-active effective-date patterns (BE-P2/P4/P6) reused.

## 1. Scope of P7 (and non-scope)
**In:** `ProviderCommissionBenefitRule` (versioned, effective-date, admin-configurable) + `ProviderCommissionBenefitEntitlement`
(granted-to-provider, usage/GMV tracked) + a **pure two-stage resolver** producing `EffectiveCommissionResolution`
(base → allowed adjustment → effective rate + monetary benefit cost, with all 9 §19.5 controls) + `ReserveCommissionBenefit`/
`ConsumeCommissionBenefit`/`ReleaseCommissionBenefit` command set (usage/GMV counters, concurrency-safe like BE-P6) +
admin CRUD + migration + tests.
**Out (later, this phase only produces inputs):** wiring into acceptance / snapshot write / reserve-consume **ordering**
relative to the customer benefit budget = **P8**; refund-driven usage restore = **P10**; the premium `OFFER_BOOST_7D`
purchase/entitlement = **P11** (P7 must stay decoupled from it); showing the effective rate in provider FE = FE_PROVIDER.

## 2. `ProviderCommissionBenefitRule` entity (§19.5)
`Payment.Domain/Entities/CommissionBenefit/ProviderCommissionBenefitRuleEntity.cs : AizenEntityWithAudit`:
`RuleCode` (unique), `RuleName?`, `ProviderProfileId?` (null = broad/campaign), `ProviderPlanId?`,
`ApplicableCategoryCodes` (null/empty = all; else set membership), `AdjustmentPercentagePoints` (decimal, **negative =
discount**, e.g. −0.01 = −1pp; expressed as a rate fraction so −1pp = −0.0100), `MinimumCommissionRate` (floor the
effective rate cannot go below — rule-level), `MaximumDiscountAmount?` (cap on monetary benefit per transaction),
`MaximumEligibleGMV?` (benefit not applied to service volume beyond this, per entitlement window), `UsageLimit?`
(max applications), `Stackable` (bool), `Exclusive` (bool), `Priority`, `EffectiveFrom`, `EffectiveTo?`, `CurrencyCode`,
`Status` (Active/Scheduled/Expired/Inactive). Validation (`ProviderCommissionBenefitRuleInvalid`): `AdjustmentPercentagePoints`
finite; `MinimumCommissionRate` ∈ [0,1]; caps ≥ 0; `Exclusive` ⇒ cannot also be relied on to stack; a rule may not set both
a positive adjustment (surcharge) unless explicitly allowed — default to discount/zero semantics, reject incoherent combos.

## 3. `ProviderCommissionBenefitEntitlement` entity
`...CommissionBenefit/ProviderCommissionBenefitEntitlementEntity.cs : AizenEntityWithAudit`:
`EntitlementCode` (unique), `ProviderProfileId`, `BenefitRuleId` (FK), `GrantedFrom`, `GrantedTo?`, `UsageLimit?`,
`UsedCount`, `MaximumEligibleGMV?`, `ConsumedGMV`, `ReservedGMV`, `Status` (Active/Exhausted/Expired/Revoked),
**`Version`** (optimistic concurrency). Domain methods: `Reserve(gmv, amount, contextRef)` (guards remaining usage AND
remaining GMV, increments `Reserved*`), `Consume(contextRef)` (Reserved→Consumed, `UsedCount++`), `Release(contextRef)`
(Reserved→released). **Not a wallet** — a commercial-advantage control ledger (mirror BE-P6 budget semantics). Unique
`(EntitlementId, ContextRef)` so a duplicate webhook can't double-consume and two concurrent offers can't double-reserve.
`ProviderCommissionBenefitUsageEntity` (`EntitlementId, ContextRef, GmvAmount, BenefitAmount, Status Reserved/Consumed/Released`).

## 4. Two-stage resolver (pure) — §19.5
`Domain/Entities/CommissionBenefit/ProviderCommissionBenefitResolver.cs`:
`ResolveEffectiveCommissionAsync(baseResolution: CommissionResolution, ctx: {ProviderProfileId, ProviderPlanId?,
CategoryCode?, ServiceAmount, EligibleGmvRemaining, CurrencyCode, atUtc}, candidateRules) → EffectiveCommissionResolution`:
1. **Base** rate from BE-P2 `CommissionResolution.Rate` (do not recompute).
2. **Candidate benefits** = active, effective-at-`atUtc`, scope-matching rules (provider/plan/category set membership).
3. **Combination controls (§19.5):**
   - If any candidate is `Exclusive=true` → only that one may apply; if it coexists with others, either it wins alone by
     specificity/priority or → fail-loud `ProviderCommissionBenefitConflict` when two `Exclusive` (or Exclusive+others that
     cannot be reconciled) tie. Non-stackable multiples that both match and neither is a clear specificity/priority winner
     → `ProviderCommissionBenefitConflict`.
   - Multiple benefits combine **only if all are `Stackable=true`**; summed `AllowedCommissionAdjustment` = Σ points.
4. **Effective rate:** `EffectiveCommissionRate = Max(BaseRate + AllowedCommissionAdjustment, RuleMinimumCommissionRate)`,
   then also floor by the **plan/system minimum** (control 8: `PREMIUM_PARTNER` auto rate never < 0.09; general floor
   never < 0 and never below the applicable plan minimum). `RoundRate` 4dp.
5. **Monetary benefit cost** = `Round(ServiceAmount × (BaseRate − EffectiveCommissionRate))` (≥ 0), then **clamp to
   `MaximumDiscountAmount`** (control 6). If a cap forces a lower benefit, recompute the **effective rate that the capped
   amount implies** so rate and amount stay consistent (report both requested and applied).
6. **GMV cap (control 5):** benefit applies only to `Min(ServiceAmount, MaximumEligibleGMV − ConsumedGMV − ReservedGMV)`;
   volume beyond the cap gets **base** rate. Split reported (`BenefitedAmount` / `NonBenefitedAmount`).
7. **Below-floor commercial rate (control 9):** any effective rate below the plan floor (e.g. < 0.09 for PREMIUM_PARTNER,
   or below the resolved plan minimum) is permitted **only** if the winning path is an explicit admin `ProviderOverride`
   base rule (§6) — a benefit rule may not by itself push below the floor. Otherwise clamp at the floor.
8. Output `EffectiveCommissionResolution { BaseRuleCode, BaseRate, AppliedBenefitRuleCodes[], RequestedAdjustment,
   AppliedAdjustment, EffectiveCommissionRate, RequestedBenefitAmount, AppliedBenefitAmount (= P5's
   ProviderCommissionBenefitCost), BenefitedServiceAmount, NonBenefitedServiceAmount, AdjustmentReason?, EntitlementIds[] }`.
   **Pure — no writes.** Purely a function of (baseResolution, context, candidate rules/entitlements).

> **Binding (control 1):** `OFFER_BOOST_7D` (premium, P11) MUST NOT be a candidate here and MUST NOT alter the commission
> rate. Enforce by construction: P7 only reads `ProviderCommissionBenefitRule`/`Entitlement`, never premium products.

## 5. Reserve / consume / release (usage + GMV counters, concurrency-safe)
Mirror BE-P6 budget lifecycle on the **entitlement**: `ReserveCommissionBenefit(entitlementId, gmv, amount, contextRef)`
(increments `UsedCount`-reserve + `ReservedGMV`; rejects if over `UsageLimit`/`MaximumEligibleGMV` →
`ProviderCommissionBenefitExhausted`) → `ConsumeCommissionBenefit(contextRef)` (on successful payment) or
`ReleaseCommissionBenefit(contextRef)` (on failure/timeout/cancel). Optimistic concurrency on `Version` + unique
`(EntitlementId, ContextRef)`: **same entitlement cannot be double-reserved/consumed by concurrent offers; duplicate webhook
cannot double-consume** (`ProviderCommissionBenefitConcurrencyConflict`). The **resolver stays pure**; these commands are
the side-effecting counterpart, invoked by P8 in the acceptance flow (ordering vs customer-benefit-budget reserve = P8).

## 6. P5-engine input production
Expose a service that, given (provider, plan, category, base commission resolution, service amount, entitlement state),
returns the `AppliedBenefitAmount` as `ProviderCommissionBenefitCost` plus the `EffectiveCommissionRate` for the snapshot.
Actual population of `ProfitProtectionContext.ProviderCommissionBenefitCost` and the reserve/consume call ordering =
**P8**. P7 provides resolver + entitlement ops + commands only.

## 7. `PaymentErrorCode` additions (5070+)
`ProviderCommissionBenefitRuleConflict = 5070` · `...RuleNotFound = 5071` · `...RuleInvalid = 5072` ·
`ProviderCommissionBenefitEntitlementNotFound = 5073` · `ProviderCommissionBenefitExhausted = 5074`
(usage/GMV limit) · `ProviderCommissionBenefitConcurrencyConflict = 5075` · `ProviderCommissionBenefitUsageNotFound = 5076`
· `ProviderCommissionBenefitUsageInvalidState = 5077` · `ProviderCommissionBelowFloor = 5078` (effective rate would fall
below the plan/system floor without an admin ProviderOverride).

## 8. Admin CRUD + persistence
`CreateProviderCommissionBenefitRule` / `Update` / `Deactivate` (single-active-overlap + coherence guard) ·
`GrantProviderCommissionBenefitEntitlement` / `Revoke` · queries `GetBenefitRules`, `ResolveEffectiveCommission` (dev/admin
what-if), `GetEntitlement(provider)`. Repos (pure resolve; entitlement ops with `Version` concurrency), EF configs
(`provider_commission_benefit_rules`, `provider_commission_benefit_entitlements`, `provider_commission_benefit_usages`;
`numeric(18,4)` money / `numeric(9,4)` rate; unique `RuleCode`/`EntitlementCode`; unique `(EntitlementId, ContextRef)`;
`Version` rowversion; scope+effective indexes), DbSets, DI.

## 9. Migration (append-only, idempotent)
New tables above + indexes. **Seed:** one **disabled/example** benefit rule documented as admin-tunable (do NOT bake a live
commercial commission discount — leave benefits off by default so base rates from BE-P2 stand until admin enables one). No
change to existing commission tables. Reversible, duplicate-seed-safe.

## 10. Tests (rigorous)
- **Two-stage rate:** base 0.09 + (−0.01) benefit → effective 0.08; benefit amount = `ServiceAmount × 0.01`.
- **Floor (control 4/8):** base 0.09 + (−0.02) with rule min 0.08 → clamped 0.08; PREMIUM_PARTNER never < 0.09 unless
  admin ProviderOverride base path → `ProviderCommissionBelowFloor` otherwise.
- **Stackable vs exclusive:** two `Stackable` → adjustments sum; an `Exclusive` alongside another → only exclusive (or
  conflict if it ties another exclusive) → `ProviderCommissionBenefitConflict`; non-stackable tie → conflict.
- **MaximumDiscountAmount clamp (control 6):** benefit amount capped; effective rate recomputed to match; both reported.
- **MaximumEligibleGMV (control 5):** ServiceAmount beyond remaining GMV → benefited/non-benefited split; benefit only on
  eligible portion.
- **Usage/GMV entitlement:** reserve decrements remaining usage+GMV; over-limit → `ProviderCommissionBenefitExhausted`;
  consume on success; release on failure; **concurrent reserve on same entitlement → one wins** (optimistic concurrency);
  duplicate consume idempotent.
- **Boost decoupling (control 1):** a premium/boost concept never appears as a candidate; resolver ignores non-benefit
  inputs (constructed so premium is not even queryable here).
- **Purity/determinism:** resolver performs no writes; same inputs → same `EffectiveCommissionResolution`.
- **Rounding:** `MoneyMath`/`RoundRate`; zero-tolerance on amount = ServiceAmount×(base−effective).

## 11. Acceptance criteria
- Two-stage `EffectiveCommissionRate = Max(Base + AllowedAdjustment, MinimumCommissionRate)`; all 9 §19.5 controls
  enforced; boost cannot create a commission benefit; floor respected (PREMIUM ≥ 0.09 except explicit admin override).
- Benefit monetary cost correct, clamped to `MaximumDiscountAmount`, applied only within `MaximumEligibleGMV`; entitlement
  usage/GMV tracked, concurrency-safe reserve/consume/release (no double-spend), fail-loud conflict.
- Resolver pure; produces `ProviderCommissionBenefitCost` + effective rate for P5/P8; BE-P2 base resolution untouched.
- Admin CRUD guards overlap/coherence; benefits off by default (seed disabled example). Build clean; migration applies;
  existing commission/premium infra untouched.

## 12. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration applied.
3. DB: `SELECT "RuleCode","AdjustmentPercentagePoints","MinimumCommissionRate","Stackable","Exclusive","Status" FROM payment.provider_commission_benefit_rules;`
   (example rule disabled) + entitlement/usage tables present.
4. Tests green (paste): two-stage rate, floor + below-floor error, stackable sum / exclusive conflict, discount clamp, GMV
   split, entitlement reserve/consume/release + **concurrent reserve one-wins**, exhausted, duplicate consume idempotent,
   boost decoupling, purity, rounding.
5. Smoke: resolve effective commission for a provider with a −1pp stackable benefit → base 0.09 → effective 0.08 + benefit
   amount; reserve against entitlement → remaining usage/GMV decrement; concurrent reserve → one-wins.

## 13. Report
`REPORT_BACKEND.md` ("BE-P7"): ProviderCommissionBenefitRule + Entitlement (usage/GMV, concurrency) + two-stage pure
resolver (base→adjustment→effective, 9 controls, floor, discount/GMV clamps) + reserve/consume/release + admin CRUD +
migration (benefits off by default). Note: acceptance wiring + reserve/consume **ordering** vs customer benefit budget =
P8; refund usage restore = P10; premium `OFFER_BOOST_7D` = P11 (kept decoupled). Next: **BE-P8
(CalculateServiceRequestPaymentEconomics — acceptance-time economy core + snapshot)**. Do not touch CargoDry.
