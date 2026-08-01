# FE_ADMIN_P3 — PlatformFeeRule admin CRUD (VERTICAL SLICE: module list + BFF passthrough + FE screen)

> **Second admin rule screen of the P1–P12 FE wave.** Unlike P2 (CommissionRule, which already had the full
> list/detail/stats surface), **P3's PlatformFeeRule was built with only resolve/create/update/deactivate — there is NO
> list/detail/reactivate endpoint**, so a CRUD *list* screen cannot be built FE-only. This is therefore a **thin
> vertical slice**: expose the already-existing repository list methods through the module + BFF (mirroring
> CommissionRule), then build the FE screen **reusing the P2 `rule-crud` blocks**. The same "add list/detail/reactivate"
> step will be needed for P5/P6/P7.
>
> **Do NOT touch** provider-web, CargoDry, Identity, Keycloak, or the CommissionRule surface. Mirror the CommissionRule
> pattern exactly; don't invent new conventions. Enum-typed fields stay **string** on the wire (BFF Newtonsoft contract).

## 0. Ground truth (confirmed)
**Module (Payment) — data layer ALREADY done:** `PlatformFeeRuleRepository` has `GetPagedAsync(filters)→(Items,Total)`,
`GetByIdAsync(id)`, `GetAllAsync()`, plus `ResolveAsync`, `FindOverlappingActiveRuleAsync`, `Add/Update/Remove`,
`GenerateRuleCodeAsync`. **Controller (`Controllers/PlatformFeeRuleController.cs`, `api/v1/payment/platform-fee`) exposes
only:** `GET resolve`, `POST rules`, `PUT rules/{id}`, `POST rules/{id}/deactivate`. **Missing: `GET rules` (list),
`GET rules/{id}` (detail), `POST rules/{id}/reactivate` (+ optional `GET rules/stats`).**

**Reference to mirror — CommissionRule (already full):** module `CommissionRuleController` (`api/v1/payment/commission`)
has `GET rules` (paged+filters), `GET rules/{id}`, `GET rules/stats`, `GET resolve`, `POST rules`, `PUT rules/{id}`,
`DELETE rules/{id}` (deactivate) + reactivate. BFF `AdminPaymentController` `payment/commission/*` + the
`AdminPayment/Query/GetCommissionRulesList|GetCommissionRuleDetail|GetCommissionRuleStats` slices +
`IAdminPaymentBffRemoteCall` commission methods.

**BFF PlatformFee (existing, `IAdminPaymentBffRemoteCall` + `AdminPaymentController`):** `ResolvePlatformFeeAsync`,
`CreatePlatformFeeRuleAsync`, `UpdatePlatformFeeRuleAsync`, deactivate; DTOs in `PlatformFeeRuleBffDtos.cs`
(`CreatePlatformFeeRuleBffRequest`, `UpdatePlatformFeeRuleBffRequest`, `PlatformFeeResolveBffResult`,
`PlatformFeeRuleCreateBffResult`, `PlatformFeeRuleMutateBffResult`). **No list/detail/reactivate.**

**FE reusable blocks (from P2, confirmed) — REUSE, do not re-implement:** `src/features/payments/rule-crud/`:
`RuleConflictBanner`, `EffectiveDateRangeField`, `PriorityField`, `SpecificityHint`, `useRuleMutation`,
`extractRuleConflict`, `ruleCrudTypes`, `index.ts`. See `docs/V1.0.1/Payment/REPORT_FE_ADMIN_P2.md`.

**PlatformFee domain (BE-P3):** 4 models `Percentage` (Rate), `Fixed` (FixedAmount), `PercentageWithBounds`
(Rate+Min+Max), `Waived` (none); base = `CustomerPayableServiceAmount`; specificity `CustomerType+Category >
CustomerType > Category > Global` + `Priority`; VAT source Rule→ReferenceData `PLATFORM_FEE_VAT_RATE`→config
(**YMM-flagged**); seed 2.5% / 99 / 1500 TRY. Conflict fail-loud `PlatformFeeRuleConflict` via envelope.

## Part 1 — Module (Payment): add list / detail / reactivate (repo already supports it)
Mirror `CommissionRuleController` + its application queries. In `PlatformFeeRuleController` add:
- `GET rules` — paged list with filters (model, customerType, categoryCode, active/inactive, search); use
  `GetPagedAsync`. Return a paged DTO mirroring the commission list-item shape (id, ruleCode, model, rate/fixed/bounds,
  currency, categoryCode, customerType, priority, effectiveFrom/To, isActive, ruleName, vatRate, specificityRank).
- `GET rules/{id}` — detail via `GetByIdAsync`.
- `POST rules/{id}/reactivate` — mirror deactivate (repo `Update`; guard already-active; effective-window re-conflict
  check via `FindOverlappingActiveRuleAsync` → `PlatformFeeRuleConflict` if reactivation would collide).
- (Optional, if cheap) `GET rules/stats` — counts by model/active for the KPI strip.
Add the application query handlers (mirror `GetCommissionRulesList`/`GetCommissionRuleDetail`). No new migration, no repo
changes. Unit tests mirroring the commission list/detail/reactivate tests (paging, filter, reactivate-conflict).

## Part 2 — BFF (AdminPanel): passthrough (mirror the commission BFF slice)
- `IAdminPaymentBffRemoteCall`: add `[AizenRemoteCallGet("/api/v1/payment/platform-fee/rules")] ListPlatformFeeRulesAsync(...)`,
  `[AizenRemoteCallGet(".../rules/{id}")] GetPlatformFeeRuleDetailAsync(id)`,
  `[AizenRemoteCallPost(".../rules/{id}/reactivate")] ReactivatePlatformFeeRuleAsync(id)` (+ stats if added).
- `AdminPayment/PlatformFeeRule/PlatformFeeRuleBffQueries.cs`: add `GetPlatformFeeRulesListBffQuery` +
  `GetPlatformFeeRuleDetailBffQuery` handlers (+ stats). New result DTOs: `PlatformFeeRuleListItemBffDto`,
  `PlatformFeeRulesListBffResult` (Items + Total/paging), `PlatformFeeRuleDetailBffDto` — typed, not `object`.
- `AdminPaymentController`: add `GET payment/platform-fee/rules`, `GET payment/platform-fee/rules/{id}`,
  `POST payment/platform-fee/rules/{id}/reactivate` (+ stats), `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse`
  envelope. Conflict stays fail-loud through the envelope. Build 0; no provider/commission changes.

## Part 3 — FE (admin-web): new PlatformFeeRules screen, reusing the P2 blocks
- Endpoints (`endpoints.ts`): `PAYMENT_PLATFORM_FEE_{RESOLVE, RULES, RULE_BY_ID, RULE_CREATE(=RULES), RULE_UPDATE(id),
  RULE_DEACTIVATE(id), RULE_REACTIVATE(id), RULE_STATS?}` under `/payment/platform-fee/...`.
- `paymentApi.ts` + a `usePlatformFeeRulesQuery.ts` hook set: list/detail/stats queries + create/update/deactivate/
  reactivate mutations, mirroring `useCommissionRulesQuery`. Types from the BFF DTOs above (Model/Priority string enums).
- **Screen** (`src/pages/app/payments/PlatformFeeRulesPage.tsx` + detail, sibling to CommissionRules; add routes to
  `routeObjects.tsx` + nav). Reuse the `rule-crud` blocks: `RuleConflictBanner` (PlatformFeeRuleConflict → guided
  banner, block save), `EffectiveDateRangeField`, `PriorityField`, `SpecificityHint` (CustomerType+Category > … >
  Global), `useRuleMutation`.
- **Model-driven form:** a `Model` select (`Percentage|Fixed|PercentageWithBounds|Waived`) that conditionally shows:
  Percentage→`Rate`; Fixed→`FixedAmount`+`CurrencyCode`; PercentageWithBounds→`Rate`+`MinAmount`+`MaxAmount`+currency;
  Waived→none. Plus `CustomerType?`, `CategoryCode?`, `Priority`, `EffectiveFrom/To`, `RuleName?`, `Notes?`,
  `VatRate?` (label it "override; else ReferenceData/config — YMM"). Update form limits to the mutable set
  (`UpdatePlatformFeeRuleBffRequest`: Model, Rate/Fixed/Min/Max, Priority, dates, RuleName, Notes, VatRate).
- **Resolve preview:** pick customerType/category → shows resolved model + FeeNet/FeeVat/FeeGross + which rule
  (`PlatformFeeResolveBffResult`), noting base = CustomerPayableServiceAmount.
- i18n `payments.json` (tr+en) extend with platform-fee keys (models, bounds, VAT note, specificity) — full parity.

## Don't-break / QA
Stitch design; envelope-tolerant (real DTO names above); reuse blocks not forks; `npm run typecheck` + module/BFF
build 0; existing CommissionRule + payment screens unregressed; provider-web/CargoDry git-clean. Seed note: BE-P3 seed
is 2.5%/99/1500 TRY — the list should show these three global rules.

## Verification (on-screen; ensure `docker compose up keycloak-init` ran so the token aud carries admin-panel-bff, then fresh login)
Log in as `admin.user@inktavia.com` (OTP from identity-api logs). On the new Platform Fee screen: list shows the seeded
rules; create each of the 4 models (validation per model); create an overlapping rule at the same specificity →
**conflict banner**, save blocked; update mutable fields; deactivate + reactivate; resolve-preview returns the winning
model + net/vat/gross. typecheck/build/tests clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_P3.md`: module/BFF endpoints added, the reused rule-crud blocks (confirming the P2
pattern transfers), the model-driven form, on-screen transcript. Note whether the "add list/detail/reactivate" step is
now the confirmed template for P5/P6/P7.
