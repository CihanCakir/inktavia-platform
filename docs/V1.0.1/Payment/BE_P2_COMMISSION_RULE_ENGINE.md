# BE-P2 — Commission Rule Engine: Specificity + Conflict + Line-Level Dims + Seed + Admin CRUD — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P2 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §6 (specificity/conflict), §13.7 (binding order + fail-loud),
> §20.11 (line-level commission dims). Feeds BE-P1 snapshot (`CommissionRateSnapshot`/`CommissionBaseAmountSnapshot`,
> and `BaseCommissionRuleIdSnapshot` reserved for P7).
> **Rule:** EXTEND the existing engine; do NOT rewrite. Inspect first.

## 0. Verified current state (inspected — this is what we extend/fix)
- `CommissionRuleEntity` already has: `RuleType` (Global/Category/Plan/ProviderOverride), `ProviderProfileId`,
  `ProviderPlanId`, `CategoryCode`, `CommissionRate`, `Priority` (Low..EMERGENCY), `Status` (Draft/Active/Scheduled/
  Expired/Inactive), `EffectiveFrom/To`, `RuleCode`, `ResolvedAppliedCount`, **`ContextType`, `ProductCode`,
  `SalesChannel`, `CommercialModel`, `CurrencyCode`, `RuleName`** — the extra dims EXIST but are unused by resolve.
- `ICommissionRuleRepository.ResolveRateAsync(providerProfileId?, providerPlanId?, categoryCode?, atUtc)` — **only 3
  dims**; 4 crude tiers; Plan/Category/Global use `FirstOrDefault` → **silent non-deterministic pick**; **no Priority,
  no ProductCode/ContextType/CommercialModel usage**; and it **mutates** (`IncrementAppliedCount + SaveChanges`) inside
  the resolve — a side effect in the resolution path.
- `ResolveCommissionRateQueryHandler` infers `source` from the **request params**, not the **matched rule** (wrong for
  audit/snapshot).
- `CreateCommissionRuleCommandHandler` / validator: field validation only → **no overlap/conflict guard**.
- Existing commands present: Create, Deactivate (Status=Inactive). Keep them; extend.

## 1. Scope of P2 (and non-scope)
**In:** true specificity resolution + fail-loud conflict (resolve-time + create-time) + line-level dims in resolve
(`LineType`/`ProductCode`/`CommissionEligibility` + existing dims) + **pure resolution** (no writes) + matched-rule
result object + Plan/Global **seed** + admin **Create/Update/Deactivate with conflict validation**.
**Out (later):** writing the rate into the snapshot at acceptance (P8); provider commission BENEFITS/adjustments
(P7 `ProviderCommissionBenefitRule`) — P2 resolves only the BASE rate; per-line application inside offer economics
(ServiceRequest S7). Do not implement benefits or line snapshots here.

## 2. Binding specificity order (§13.7)
```
Provider+Plan+Category > Provider+Category > Provider+Plan > Provider
  > Plan+Category > Plan > Category > Global
```
- A rule is a **candidate** only if EVERY non-null scope dim it declares matches the request context. Scope dims:
  `ProviderProfileId, ProviderPlanId, CategoryCode` (primary matrix) **and** finer dims `ProductCode, ContextType/LineType,
  CommercialModel, SalesChannel, CurrencyCode, CommissionEligibility` (a rule with a non-null finer dim only matches when
  the request supplies the same value).
- **SpecificityRank** = position in the 8-level order (Provider+Plan+Category highest … Global lowest). Finer dims
  (ProductCode/LineType) act as **tie-breakers that increase** specificity within the same base level.
- Selection: highest SpecificityRank → then highest `Priority` → if **still ≥2 active candidates tie on
  (SpecificityRank, Priority)** → **CONFLICT** (§3), never `FirstOrDefault`.

## 3. Fail-loud conflict (§13.7)
- **Resolve-time:** if the top tier has ≥2 rules tied on (specificity, priority) → throw
  `AizenBusinessException(PaymentErrorCode.CommissionRuleConflict)` (new error code) — no silent pick. Surface enough
  detail (rule ids) for admin diagnosis.
- **Create/Update-time:** reject saving a rule that would create an **active overlap** = same scope-key
  (RuleType + all declared dims) **and** same `Priority` **and** overlapping `[EffectiveFrom, EffectiveTo)` with an
  existing Active/Scheduled rule → validation error (`CommissionRuleConflict`). This is the primary guard; resolve-time
  conflict is the safety net.

## 4. Pure resolution + result object (fix the side effect)
- New/extended repo method returns a **result**, not a bare decimal, and performs **NO writes**:
  ```csharp
  public sealed record CommissionResolution(
      long RuleId, string RuleCode, CommissionRuleType RuleType,
      decimal Rate, int SpecificityRank, CommissionRulePriority Priority, string Source);
  Task<CommissionResolution?> ResolveAsync(CommissionResolveContext ctx, DateTime atUtc, CancellationToken ct);
  ```
  where `CommissionResolveContext` carries `ProviderProfileId?, ProviderPlanId?, CategoryCode?, ProductCode?, LineType?,
  CommercialModel?, SalesChannel?, CurrencyCode?, CommissionEligibility?`.
- **Move `IncrementAppliedCount` OUT of resolve.** Applied-count is bumped only when a rule is actually **applied at
  acceptance** (P8), via an explicit `MarkAppliedAsync(ruleId)` repo method / command — never during resolve/preview.
- Keep the existing `ResolveRateAsync(3-dim)` as a **thin backward-compat wrapper** over `ResolveAsync` (maps to context,
  returns `.Rate`), so current callers keep working; update `ResolveCommissionRateQueryHandler` to return the **matched
  rule's** source/id (not request-inferred).

## 5. Seed (duplicate-safe, dev + prod-config aware)
- `RuleType=Plan` rules bound to ProviderPlanId for **FREE %15 (0.15), STANDARD %12 (0.12), PREMIUM_PARTNER %9 (0.09)**
  — `EffectiveFrom=go-live`, `EffectiveTo=null`, `Status=Active`, `Priority=Standard`, unique `RuleCode`.
- One `RuleType=Global` default fallback (rate = a configurable default; document the chosen value as admin-tunable, not
  a hard business constant).
- Idempotent: skip if a rule with the same scope-key already exists.

## 6. Admin CRUD (extend existing commands)
- `CreateCommissionRule` / `UpdateCommissionRule` / `DeactivateCommissionRule`: all run the **create/update conflict
  guard** (§3). Validator: rate in (0,1); EffectiveTo > EffectiveFrom; dims coherent with RuleType (e.g. Plan rule must
  set ProviderPlanId; Category must set CategoryCode; Global sets none).
- Query `ResolveCommissionRate` returns the full `CommissionResolution` (rate + matched ruleId + specificity + source).

## 7. Migration
- Entity dims already exist → likely **no new columns**. Add: supporting index for resolution
  (e.g. `(Status, EffectiveFrom, EffectiveTo)` and `(RuleType, ProviderPlanId)` / `(RuleType, CategoryCode)`), and the
  **seed** (Plan + Global). Append-only, idempotent, duplicate-seed-safe. No backfill of existing rows needed.

## 8. Unit / integration tests
- **Specificity:** each of the 8 levels resolves to the correct rule; finer dim (ProductCode/LineType) wins over a
  broader same-level rule.
- **Conflict:** two active rules tied on (specificity, priority) → resolve throws `CommissionRuleConflict`;
  create/update of an overlapping active rule → rejected.
- **Fallback:** no match → Global; no Global → `CommissionRuleNotFound`.
- **Pure resolve:** `ResolveAsync` performs **no DB writes** (ResolvedAppliedCount unchanged); `MarkAppliedAsync`
  increments exactly once.
- **Seed:** FREE 0.15 / STANDARD 0.12 / PREMIUM 0.09 resolve for the right plan; re-running seed doesn't duplicate.
- **Result object:** returns matched ruleId + specificity + source (not request-inferred).

## 9. Acceptance criteria
- Resolution deterministic via §2 specificity + Priority; **never** silent `FirstOrDefault`; ties → **fail-loud**
  `CommissionRuleConflict` (resolve + create/update).
- Line-level dims (`LineType`/`ProductCode`/`CommissionEligibility` + existing) honored so ServiceRequest S7 can resolve
  per line.
- Resolve is **side-effect-free**; applied-count moved to explicit `MarkAppliedAsync` (acceptance only).
- Plan rates (15/12/9%) + Global seeded idempotently; admin CRUD guards conflict.
- Existing engine/callers untouched except the corrected `source` + pure-resolve refactor; build clean; migration applies.

## 10. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration applied.
3. DB: `SELECT "RuleType","ProviderPlanId","CommissionRate","Priority","Status" FROM payment.commission_rules ORDER BY 1,2;`
   → Plan rules 0.15/0.12/0.09 + Global present.
4. Unit/integration tests green (paste summary): specificity levels, conflict throw (resolve + create), pure-resolve
   (no write), fallback, seed idempotency.
5. (Smoke) `ResolveCommissionRate` for (plan=STANDARD) → 0.12 with matched ruleId + source=Plan; inject a tie → conflict.

## 11. Report
`REPORT_BACKEND.md` ("BE-P2"): specificity resolution + fail-loud conflict (resolve+create), line-level dims, pure
resolve + `MarkAppliedAsync`, corrected matched-rule result, seed (15/12/9 + Global), admin CRUD conflict guard, indexes.
Note: base-rate only (benefits = P7); per-line application = ServiceRequest S7; snapshot write = P8. Next: **BE-P3**
(PlatformFeeRule). Do not touch CargoDry.
