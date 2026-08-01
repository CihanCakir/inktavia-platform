# REPORT — FE_ADMIN_P3: PlatformFeeRule admin CRUD (vertical slice)

**Scope delivered:** module list/detail/reactivate(+stats) → BFF passthrough → admin-web PlatformFeeRules screen
(+ detail), reusing the P2 `rule-crud` blocks. Mirrors the CommissionRule surface exactly; no new conventions
invented. Enum-typed fields stay **string** on the BFF wire (Newtonsoft contract). No provider-web / CargoDry /
Identity / Keycloak / CommissionRule surface touched.

---

## Part 1 — Module (Payment): list / detail / reactivate (+ stats)

Repository already exposed `GetPagedAsync / GetByIdAsync / GetAllAsync / FindOverlappingActiveRuleAsync`; **no repo
changes, no migration**. Added only application queries + a command + controller routes (mirroring
`CommissionRuleController`).

**New application files**
- `Application/Dto/PlatformFeeRuleDto.cs` — `PlatformFeeRuleDto` (enum-typed; module API's `StringEnumConverter`
  serializes them as strings), `PlatformFeeRuleListResult`, `PlatformFeeRuleStatsDto`. List-item shape mirrors the
  commission list item: id, ruleCode, model, rate/fixed/min/max, currency, category, customerType, priority,
  effectiveFrom/To, status, isActive, ruleName, notes, vatRate, **specificityRank**, audit.
- `Application/Dto/PlatformFeeRuleDtoMapper.cs` — single entity→DTO map (shared by list + detail so they stay in
  lock-step). `SpecificityRank` delegated to `PlatformFeeRuleResolver.ComputeSpecificityRank`.
- `Queries/GetPlatformFeeRulesList/*` — paged list, filters `model / status / currencyCode / categoryCode /
  customerType` via `GetPagedAsync`.
- `Queries/GetPlatformFeeRuleById/*` — detail via `GetByIdAsync`; throws `PlatformFeeRuleNotFound`.
- `Queries/GetPlatformFeeRuleStats/*` — KPI counts (total / active / per-model) computed from `GetAllAsync`
  (no repo change).
- `Commands/ReactivatePlatformFeeRule/*` — mirrors deactivate: guards `Status != Inactive` →
  `PlatformFeeRuleNotInactive`, then **re-runs the §7 conflict guard** (`FindOverlappingActiveRuleAsync` →
  `PlatformFeeRuleConflict`) before flipping `IsActive` back on. Validator mirrors the commission reactivate validator.

**Enum** — added `PaymentErrorCode.PlatformFeeRuleNotInactive = 5047` (free slot in the BE-P3 block; mirrors
`CommissionRuleNotInactive = 5037`).

**Controller** (`Controllers/PlatformFeeRuleController.cs`, `api/v1/payment/platform-fee`) — added
`GET rules`, `GET rules/{id}`, `GET rules/stats`, `POST rules/{id}/reactivate`. (Existing `resolve / POST rules /
PUT rules/{id} / POST rules/{id}/deactivate` untouched.)

**Tests** (`PlatformFeeRuleRepositoryTests.cs`, mirroring the commission repo tests) — added
`GetByIdAsync` (hit + null), `GetPagedAsync` filter-by-model, paging+total, filter-by-customerType, and
`FindOverlappingActiveRuleAsync_Detects_Reactivation_Conflict` (an inactive rule that would collide when
reactivated). **9/9 repository tests pass.**

Module Web project builds **0 errors** (100 pre-existing warnings only).

## Part 2 — BFF (AdminPanel): passthrough (mirror the commission BFF slice)

- `IAdminPaymentBffRemoteCall` — added `ListPlatformFeeRulesAsync` (`GET .../rules`),
  `GetPlatformFeeRuleStatsAsync` (`GET .../rules/stats`), `GetPlatformFeeRuleDetailAsync` (`GET .../rules/{id}`),
  `ReactivatePlatformFeeRuleAsync` (`POST .../rules/{id}/reactivate`). Enum query params are plain `string?`.
- `AdminPayment/Dto/PlatformFeeRuleBffDtos.cs` — added **typed** result DTOs (not `object`):
  `PlatformFeeRuleListItemBffDto`, `PlatformFeeRulesListBffResult` (Items + Total/Page/PageSize),
  `PlatformFeeRuleDetailBffDto`, `PlatformFeeRuleStatsBffDto`. Model/Priority/Status are `string`.
- `AdminPayment/PlatformFeeRule/PlatformFeeRuleBffQueries.cs` — added `GetPlatformFeeRulesListBffQuery`,
  `GetPlatformFeeRuleDetailBffQuery`, `GetPlatformFeeRuleStatsBffQuery` handlers (alongside the existing resolve).
- `AdminPayment/PlatformFeeRule/PlatformFeeRuleBffCommands.cs` — added `ReactivatePlatformFeeRuleBffCommand`
  handler; conflict/NotInactive stays fail-loud through the envelope.
- `AdminPaymentController` — added `GET payment/platform-fee/rules`, `.../rules/stats`, `.../rules/{id}`,
  `POST payment/platform-fee/rules/{id}/reactivate`, all under the controller's `[Authorize(Policy =
  "AdminPanelAccess")]` with the `AizenApiResponse` envelope via `SetResponse(...)`.

BFF AdminPanel builds **0 errors**.

## Part 3 — FE (admin-web): PlatformFeeRules screen (reusing the P2 blocks)

**Reused `src/features/payments/rule-crud/` blocks verbatim — NOT re-implemented:** `RuleConflictBanner`,
`EffectiveDateRangeField`, `PriorityField`, `SpecificityHint`, `useRuleMutation`, `extractRuleConflict`. This confirms
the P2 pilot's claim that the blocks are rule-agnostic — the PlatformFee screen dropped them in with only
platform-fee-specific props (dimension set = CustomerType > Category; the conflict banner/mutation are unchanged).

**New / changed FE files**
- `shared/api/endpoints.ts` — `PAYMENT_PLATFORM_FEE_{RESOLVE, RULES, RULE_STATS, RULE_BY_ID, RULE_UPDATE,
  RULE_DEACTIVATE, RULE_REACTIVATE}`. (Create = `RULES` POST; deactivate/reactivate are POST, matching the module.)
- `shared/api/queryKeys.ts` — `payments.platformFee.{resolve, rules, ruleById, stats}`.
- `shared/api/types/payment.types.ts` — `PlatformFeeModel`, `PlatformFeeRuleDto`, `…ListResult`, `…StatsDto`,
  `PlatformFeeResolveResult`, `Create…Request`, `Update…Request` (mutable set), list filters, resolve params.
  Envelope-tolerant: uses the real BFF DTO field names; Model/Priority/Status typed as `string`.
- `features/payments/api/paymentApi.ts` — `resolvePlatformFee`, `getPlatformFeeRules`, `getPlatformFeeRuleStats`,
  `getPlatformFeeRuleById`, `createPlatformFeeRule`, `updatePlatformFeeRule`, `deactivatePlatformFeeRule`,
  `reactivatePlatformFeeRule`.
- `features/payments/hooks/usePlatformFeeRulesQuery.ts` — list/detail/stats/resolve queries + create/update/
  deactivate/reactivate mutations, mirroring `useCommissionRulesQuery` (same invalidation keys pattern).
- `features/payments/utils/platformFee.ts` — `formatModelValue` (per-model economic display), shared by list + detail.
- `pages/app/payments/PlatformFeeRulesPage.tsx` — KPI strip (active/total + per-model), rules table
  (RuleCode / Model / Value / Scope via `SpecificityHint` / Effective dates / Priority / Status), model-driven
  **Add Rule** modal, and a **Resolve Preview** panel.
- `pages/app/payments/PlatformFeeRuleDetailPage.tsx` — KPI strip, definition + targeting + audit cards, and the
  edit / deactivate / reactivate actions with the model-driven **Edit** modal (mutable set only).
- `app/router/routes.tsx` + `routeObjects.tsx` — `PLATFORM_FEE_RULES` (`/app/payments/platform-fee-rules`) and
  `PLATFORM_FEE_RULE_DETAIL(:ruleId)`.
- `pages/app/payments/PaymentDashboardPage.tsx` — **nav:** a "Platform Fee Rules" header link (sibling to the payout
  queue shortcut). The commission surface was not touched, so the entry point lives on the payments hub.
- i18n `payments.json` (**tr + en, full parity — 0 mismatched keys**): `platformFeeRules.*`,
  `platformFeeRuleDetail.*`, `dashboard.platformFeeRules`.

**Model-driven form** — a `Model` select conditionally reveals: `Percentage → Rate`; `Fixed → FixedAmount +
Currency`; `PercentageWithBounds → Rate + Min + Max + Currency`; `Waived → none` (with an explainer). Plus
`CustomerType? / CategoryCode? / Priority / EffectiveFrom-To / RuleName? / Notes? / VatRate?` (labelled "override;
else ReferenceData/config — YMM"). Per-model client validation (rate>0, fixed≥0, min≤max, half-open window). The
**Edit** modal exposes exactly the mutable set of `UpdatePlatformFeeRuleBffRequest` (Model, Rate/Fixed/Min/Max,
Priority, dates, RuleName, Notes, VatRate) — CustomerType/Category/Currency are immutable after create, matching the
module's `Update`.

**Resolve preview** — pick currency / customerType / category + base amount → shows resolved **Model**, **winning
rule** (RuleCode), **specificity source**, and **FeeNet / FeeVat (with rate + source) / FeeGross**, noting base =
CustomerPayableServiceAmount.

`npm run typecheck` **clean**; ESLint on the new files **clean**. (`npm run build` surfaces pre-existing TS errors in
`ProviderDetailPage.tsx`, `ProvidersPage.tsx`, `rule-crud/ruleCrud.test.tsx` — all outside this change and present on
the working baseline; none in the P3 files. typecheck is the required gate and passes.)

---

## Confirmed template for P5 / P6 / P7

**Yes — the "add list/detail/reactivate at the module + BFF, then reuse the `rule-crud` blocks on the FE" step is now
the confirmed template.** Each of P5 (ProfitProtectionPolicy), P6 (CustomerDiscountRule), P7
(ProviderCommissionBenefitRule) currently exposes only resolve/create/update/deactivate at the module + BFF (same
shape P3 started from). To give each a CRUD list screen:

1. **Module** — add `GetXxxRulesList` / `GetXxxRuleById` (+ optional stats) queries + a `ReactivateXxx` command
   (guard non-Inactive → `XxxNotInactive`; re-run `FindOverlappingActiveRuleAsync` → `XxxConflict`), and the three
   controller routes. Repos already have the paged/by-id reads → no repo change, no migration.
2. **BFF** — add the three `IAdminPaymentBffRemoteCall` methods, **typed** list/detail/stats DTOs, the query handlers
   + reactivate command, and the `AdminPaymentController` routes under `AdminPanelAccess` with the envelope.
3. **FE** — endpoints + queryKeys + paymentApi methods + a `useXxxRulesQuery` hook set + a page/detail pair that
   **reuses the same `rule-crud` blocks**, with a policy-specific form and resolve preview + tr/en i18n.

The only per-phase variation is the domain-specific form (the model/funding/share fields) and the specificity
dimension set passed to `SpecificityHint`; the conflict UX, effective-window field, priority select, and
conflict-aware mutation are shared unchanged.

---

## Verification (on-screen)

`payment-api` + `bff-adminpanel` images were **rebuilt with the new code**; `docker compose up keycloak-init` ran
(realm reconfigured: Admin role assigned to `admin.user@inktavia.com`, `audience-admin-panel-bff` mapper present,
admin OTP flow bound). Fresh OTP login as `admin.user@inktavia.com` (code from `identity-api` DEV-ONLY logs). Live
run against the full stack (FE `localhost:3000` → `bff-adminpanel:17001` → `payment-api` → Postgres):

| # | Step | Result |
|---|------|--------|
| 1 | Log in (OTP) → open **Platform Ücreti Kuralları** | ✅ Screen renders; tr i18n; KPI strip from `rules/stats` |
| 2 | List seeded rule | ✅ `#1` — PercentageWithBounds **2.50% (99–1500 TRY)**, Global, Active (BE-P3 seed) |
| 3 | Create **Percentage** (GOLD, 2.5%) | ✅ `PFR-2026-BA002`, CustomerType scope, Active |
| 4 | Create **Fixed** (SILVER, 99 TRY) | ✅ `PFR-2026-CA003` — currency field shown for Fixed |
| 5 | Create **PercentageWithBounds** (PLATINUM, 2.5% / 99–1500) | ✅ `PFR-2026-DA004` — rate+min+max+currency shown |
| 6 | Create **Waived** (category PROMO) | ✅ `PFR-2026-EA005` — no amount fields, "waived" note; value `0` |
| 7 | Create overlapping GOLD/Percentage/Standard rule | ✅ **Conflict guard fired, save blocked** — `payment-api` log: *"A conflicting active platform fee rule already exists (Id=2, RuleCode=PFR-2026-BA002) with the same scope, priority, and an overlapping effective window."* Fail-loud banner, modal stays open |
| 8 | Update rule #2 (rate 2.5→3.0%, priority Standard→High) | ✅ Detail reflects **3.00% / HIGH** after refetch |
| 9 | Deactivate rule #2 | ✅ DURUM → **INACTIVE**; Deactivate disabled; Reactivate appears |
| 10 | Reactivate rule #2 | ✅ Re-runs conflict guard (no collision) → DURUM → **ACTIVE** |

KPI counts tracked correctly throughout (5 rules → 1 Percentage / 1 Fixed / 2 Bounded / 1 Waived). Rule codes are
module-generated (`PFR-YYYY-XXX`). The model-driven form's conditional fields, the reused `EffectiveDateRangeField` /
`PriorityField` / `SpecificityHint` / `RuleConflictBanner` / `useRuleMutation` blocks, and the specificity explainer
(`CustomerType+Category > CustomerType > Category > Global`) all render and behave correctly.

**Two notes (both independent of the P3 CRUD slice):**
1. **Resolve preview** returns "no rule / rejected" because the **pre-existing** `ResolvePlatformFeeQueryHandler →
   PlatformFeeCalculationService` needs `ISystemParameterReferenceService` (ReferenceData module), which is **not
   registered in the standalone `payment-api` container** — Autofac `DependencyResolutionException` → 500. This is a
   cross-module DI wiring gap on the resolve path I never touched (I only added list/detail/reactivate/stats, none of
   which use that service); the UI degrades gracefully via the envelope-tolerant path. To make the preview live, the
   `payment-api` host must register the ReferenceData system-parameter service.
2. **Conflict envelope shape:** the module surfaces `AizenBusinessException(PlatformFeeRuleConflict)` as an HTTP 500
   rather than a typed envelope, so `extractRuleConflict` shows the generic "rejected" banner instead of the
   conflict-specific guidance. This is the Payment module's shared exception-handling behavior (the mirrored
   CommissionRule path behaves the same) — the save is still correctly fail-loud and blocked.

**Environment:** the first bring-up crashed Docker Desktop twice (its VM was capped at 8 GB; the full service graph
OOM-cascaded on restart). Raising the Docker VM to 11 GB (host has 16 GB) stabilized it; only the minimal set
(infra + `identity-api` + `payment-api` + `bff-adminpanel`) was run for the verification.

typecheck ✅ · ESLint (new files) ✅ · module build 0 ✅ · module repo tests 9/9 ✅ · BFF build 0 ✅ · existing
CommissionRule + payment screens unregressed · provider-web / CargoDry untouched.
