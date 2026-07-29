# BE-P3 — `PlatformFeeRule` (hybrid, configurable) + Resolve + Apply + Seed + Admin CRUD — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P3 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §1, §2.3, §13.3 (net/vat/gross), §19.13 (base =
> CustomerPayableServiceAmount). Produces the values for the **already-existing** BE-P1 snapshot fields
> `PlatformFee{Base,RuleId,Rate,Minimum,Maximum,Net,Vat,Gross}AmountSnapshot`.
> **Rule:** New rule + resolver only; do NOT rewrite existing infra. Reuse the BE-P2 specificity/conflict + `MoneyMath`
> patterns. Inspect first.

## 0. Verified current state
- **No `PlatformFeeRule` exists → create.** The only "platform fee" references are the **snapshot columns** created in
  BE-P1 (ready to be populated). Snapshot write happens at acceptance (P8), NOT here.
- VAT convention (mirror `InvoiceTaxBreakdown`): net (TaxableAmount) × rate = tax; gross = net + tax; money `numeric(18,4)`,
  rate `numeric(9,4)`, `MoneyMath.Round` (BE-P1).
- Customer plan source = `ParticipantPlanEntity` (BASIC/GOLD/PLATINUM) → `CustomerType` dim can reference PlanCode.
- Platform fee is **transaction/offer level** (base = whole offer after discount), **not per line** — simpler than
  commission (no line dims).

## 1. Scope of P3 (and non-scope)
**In:** `PlatformFeeRule` entity (4 models) + effective-date + specificity/conflict resolve + `ApplyPlatformFeeRule`
(net) + VAT net/vat/gross + default seed (%2,5 / 99 / 1.500) + admin Create/Update/Deactivate with conflict guard +
tests.
**Out (later):** writing values into the snapshot at acceptance (P8); customer discounts affecting the base (P6 — P3
computes on whatever `CustomerPayableServiceAmount` it is given; at P6 that becomes post-discount). Do not implement
discounts here.

## 2. `PlatformFeeRule` entity
`Payment.Domain/Entities/PlatformFee/PlatformFeeRuleEntity.cs : AizenEntityWithAudit`:
`Model` (`PlatformFeeModel`: Percentage=1, Fixed=2, PercentageWithBounds=3, Waived=4), `Rate?` (fraction, e.g. 0.025),
`FixedAmount?`, `MinAmount?`, `MaxAmount?`, `CurrencyCode` ("TRY"), `CategoryCode?`, `CustomerType?` (participant PlanCode),
`Priority` (reuse `CommissionRulePriority` or a parallel enum), `EffectiveFrom`, `EffectiveTo?`, `Status` (reuse the
Active/Scheduled/Expired/Inactive/Draft pattern), `RuleCode` (unique), `RuleName?`, `VatRate` (nullable → falls back to
ReferenceData category VAT / policy; see §5). Validation: model-coherent (Percentage needs Rate; Fixed needs
FixedAmount; PercentageWithBounds needs Rate+Min+Max with Min≤Max; Waived needs none).

## 3. Resolution (pure, specificity + fail-loud conflict — mirror BE-P2)
`ResolvePlatformFeeRuleAsync(PlatformFeeResolveContext, atUtc) → PlatformFeeResolution?` (pure, NO writes).
Context: `CurrencyCode, CategoryCode?, CustomerType?`. Candidacy: active + effective window + every declared dim matches.
**Specificity order:** `CustomerType+Category > CustomerType > Category > Global`; then `Priority`; ties on
(specificity, priority) among ≥2 active → **fail-loud** `AizenBusinessException(PaymentErrorCode.PlatformFeeRuleConflict)`
(new code). No `FirstOrDefault`. Result: `(RuleId, RuleCode, Model, Rate, MinAmount, MaxAmount, FixedAmount, VatRate, Source)`.

## 4. `ApplyPlatformFeeRule` (net amount; §19.13 base)
`ApplyPlatformFeeRule(decimal customerPayableServiceAmount, PlatformFeeResolution rule) → decimal feeNet` (via
`MoneyMath.Round`):
```
Percentage            : feeNet = Round(base × Rate)
Fixed                 : feeNet = FixedAmount
PercentageWithBounds  : feeNet = Clamp(Round(base × Rate), MinAmount, MaxAmount)
Waived                : feeNet = 0
```
`base = CustomerPayableServiceAmount` (post-discount at P6; = ServiceAmount today). **Boundary tests:** below-min→Min,
above-max→Max, exact bounds.

## 5. VAT net/vat/gross (§13.3 — YMM-gated)
`PlatformFeeNet = feeNet`; `PlatformFeeVat = Round(PlatformFeeNet × vatRate)`; `PlatformFeeGross = Net + Vat`.
`vatRate` resolution: rule.`VatRate` if set, else **ReferenceData category/default VAT** (R2) — since R2 may not be ready,
fall back to a configurable default (do NOT hardcode a business constant; read from config/policy) and **mark the VAT
source in the result**. Flag YMM dependency (§2.5): the definitive VAT treatment (payer, rate, invoicing) awaits YMM.

## 6. Seed (duplicate-safe)
Default Global rule: `Model=PercentageWithBounds, Rate=0.025, MinAmount=99, MaxAmount=1500, CurrencyCode=TRY,
Status=Active, Priority=Standard, EffectiveFrom=go-live, EffectiveTo=null`, unique RuleCode. Idempotent (skip if a
same-scope active rule exists). Values are launch defaults, admin-tunable — not hard constants.

## 7. Admin CRUD + persistence
- `CreatePlatformFeeRule` / `UpdatePlatformFeeRule` / `DeactivatePlatformFeeRule` with **conflict guard** (same scope-key
  + priority + overlapping window → reject, mirror BE-P2 create-time overlap).
- `IPlatformFeeRuleRepository` (AddAsync/GetById/GetPaged/ResolveAsync-pure/Deactivate/SaveChanges); EF config
  `platform_fee_rules` (`numeric(18,4)`/`numeric(9,4)`, enum int, unique RuleCode, index `(Status,EffectiveFrom,EffectiveTo)`
  + `(CurrencyCode,CategoryCode,CustomerType)`); `PaymentDbContext` DbSet; DI (scoped).
- Query `ResolvePlatformFee` (admin/dev) returning the full resolution.

## 8. Migration (append-only, idempotent)
New table `platform_fee_rules` + indexes + default seed. No changes to existing tables (snapshot fields already exist
from BE-P1). Reversible, duplicate-seed-safe.

## 9. Unit / integration tests
- **Models:** Percentage / Fixed / PercentageWithBounds (clamp below-min→min, above-max→max, within) / Waived=0.
- **VAT:** gross == net + vat; vat == Round(net × rate); rule.VatRate vs default fallback; source flagged.
- **Specificity/conflict:** CustomerType+Category > CustomerType > Category > Global; tie → `PlatformFeeRuleConflict`;
  create overlap rejected; no-match → Global; no Global → null/error (decide + document).
- **Pure resolve:** no DB writes.
- **Rounding:** `MoneyMath.Round` (2-dp AwayFromZero); no kuruş drift; base = CustomerPayableServiceAmount.
- **Seed:** %2,5/99/1500 resolves for TRY; re-seed idempotent.

## 10. Acceptance criteria
- 4 models implemented; hybrid clamp correct; base = CustomerPayableServiceAmount (§19.13); net/vat/gross consistent
  (§13.3), VAT source configurable + YMM-flagged.
- Deterministic specificity + **fail-loud conflict** (resolve + create); pure resolve.
- Default %2,5/99/1.500 seeded idempotently; admin CRUD guards conflict; values admin-tunable (no hardcoded constants).
- Produces exactly the fields BE-P1 snapshot expects (`PlatformFee{Rate,Minimum,Maximum,Net,Vat,Gross,Base,RuleId}`);
  snapshot write itself deferred to P8. Build clean; migration applies; existing infra untouched.

## 11. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + seed applied.
3. DB: `SELECT "Model","Rate","MinAmount","MaxAmount","CurrencyCode","Status" FROM payment.platform_fee_rules;` →
   PercentageWithBounds 0.025/99/1500 TRY.
4. Tests green (paste): 4 models, clamp boundaries, VAT net/vat/gross, specificity+conflict, pure resolve, seed idempotency.
5. Smoke: `ResolvePlatformFee(currency=TRY)` → the bounds rule; `ApplyPlatformFeeRule(base=1000)` → 25 → clamped to 99
   (min) with correct vat/gross.

## 12. Report
`REPORT_BACKEND.md` ("BE-P3"): PlatformFeeRule (4 models) + pure resolve + fail-loud conflict + ApplyPlatformFeeRule +
VAT net/vat/gross (YMM-flagged) + seed (%2,5/99/1500) + admin CRUD + migration. Note snapshot write = P8; discount base
= P6. Next: **BE-P4 (ProviderPlanPrice price-version)**. Do not touch CargoDry.
