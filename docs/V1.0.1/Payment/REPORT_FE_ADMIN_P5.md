# REPORT — FE_ADMIN_P5 · ProfitProtectionPolicy admin editor (vertical slice)

**Date:** 2026-08-01 · **Spec:** `docs/V1.0.1/Payment/FE_ADMIN_P5_PROFIT_PROTECTION.md`
**Scope:** Payment module list/detail/reactivate + AdminPanel BFF passthrough + new admin-web
policy-editor screen. Same vertical-slice template as P3 (PlatformFee), **backend-first**. Key difference:
ProfitProtectionPolicy is a **single-active-per-currency VERSIONED policy** (no specificity/priority) → a
**policy-editor + version-history** screen, not a rule list.

Untouched per constraints: provider-web, CargoDry, Identity, Keycloak, Commission/PlatformFee surfaces.

---

## Part 1 — Module (Payment): list / detail / reactivate

`ProfitProtectionPolicyController` (`api/v1/payment/profit-protection`) now exposes the full surface (resolve +
create/update/deactivate were already present; **list / detail / reactivate** complete the P3-mirror):

| Verb | Route | Handler |
|------|-------|---------|
| GET  | `policies` (filters: `currencyCode`, `status`, `isActive`) | `GetProfitProtectionPoliciesListQuery` → `GetAllAsync`, **no paging**, sort **currency, then EffectiveFrom desc** |
| GET  | `policies/{id}` | `GetProfitProtectionPolicyByIdQuery` → `GetByIdAsync` |
| POST | `policies/{id}/reactivate` | `ReactivateProfitProtectionPolicyCommand` — re-checks `FindOverlappingActivePolicyAsync` → `ProfitProtectionPolicyConflict` on collision |

- Application: `GetProfitProtectionPoliciesList` / `GetProfitProtectionPolicyById` query handlers + `ProfitProtectionPolicyDto`/`ProfitProtectionPolicyDtoMapper` (list & detail share one mapper). No migration, no repo changes.
- **Build 0.** Unit tests mirror P3 (`ProfitProtectionTests.cs`): `GetByIdAsync` (hit + null), `GetAllAsync` version history, `FindOverlappingActivePolicyAsync` reactivation-conflict, `Reactivate` re-derives status — **9 passed / 0 failed.**

## Part 2 — BFF (AdminPanel): passthrough

- `IAdminPaymentBffRemoteCall`: `ListProfitProtectionPoliciesAsync` / `GetProfitProtectionPolicyDetailAsync(id)` / `ReactivateProfitProtectionPolicyAsync(id)` (Refit, targeting `/api/v1/payment/profit-protection/policies[...]`).
- `ProfitProtectionPolicyBffQueries.cs`: `GetProfitProtectionPoliciesListBffQuery` + `GetProfitProtectionPolicyDetailBffQuery` handlers.
- `ProfitProtectionPolicyBffCommands.cs`: `ReactivateProfitProtectionPolicyBffCommand` handler.
- Typed DTOs (already present, exact fields): `ProfitProtectionPolicyListItemBffDto`, `…ListBffResult`, `…DetailBffDto`, `…ResolveBffResult`, create/update requests, create/mutate results.
- `AdminPaymentController`: `GET payment/profit-protection/policies`, `GET .../policies/{id}`, `POST .../policies/{id}/reactivate` — all `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse` envelope.
- The existing `AdminPanelBffFailEnvelopeHandler` already carries `ProfitProtectionPolicyConflict` (**5050**) to the FE. **Build 0.**

## Part 3 — FE (inktavia-marine-admin-web): policy editor (vs rule list)

New / edited:
- **Screens** `src/pages/app/payments/ProfitProtectionPolicyPage.tsx` (+ `ProfitProtectionPolicyDetailPage.tsx`).
- **Shared form** `src/features/payments/components/ProfitProtectionPolicyForm.tsx` — one grouped, labeled form body reused by the create modal and the detail edit modal (currency shown only in `mode="create"`; edit omits it), plus `ADJUSTMENT_ORDER_VALUES`, `policyFormFromDto`, `validatePolicyForm`, `buildCreate/UpdateBody`.
- **Hooks** `src/features/payments/hooks/useProfitProtectionPolicyQuery.ts` (list/detail/resolve queries; create/update/deactivate/reactivate mutations) mirroring `usePlatformFeeRulesQuery`.
- Wiring: `payment.types.ts` (DTO-mirroring interfaces), `endpoints.ts`, `queryKeys.ts` (`profitProtection`), `paymentApi.ts` methods, `routes.tsx` + `routeObjects.tsx` (list + `:policyId`), payments-dashboard nav button, i18n `payments.json` **tr + en** (`profitProtectionPolicies` / `profitProtectionPolicyDetail`, full parity).

**Policy-editor vs rule-list shape.** Layout is a **version-history list grouped by currency** (active policy highlighted + past versions, effective window, policyCode) + a **policy editor** (create new version / edit active), a **resolve preview**, and a read-only **decision-state legend**. Form groups: (1) Currency (create-only, disabled on edit); (2) 3 contribution gates customer/provider/transaction × amount+rate with the hint *"Required = max(amount, base × rate)"*; (3) expected expenses (PaymentProcessing rate+fixed, RefundRiskReserve, OtherVariable rate+fixed); (4) `CustomerSideVariableCostShareRate`; (5) `AdjustmentOrder` string-enum select (values read from the module enum, human labels); (6) `EffectiveDateRangeField` + PolicyName? + Notes?. Rates are entered as **percent** and converted to **fractions** on the wire (0.029 = 2.9%).

**rule-crud blocks — reused vs skipped.** Reused (import-only): `RuleConflictBanner`, `EffectiveDateRangeField`, `useRuleMutation` (+ `extractRuleConflict`, `RULE_CONFLICT_CODES` — **5050 already registered**). **Skipped by design:** `PriorityField`, `SpecificityHint` (a versioned single-active policy has no specificity/priority tie-break).

**`npm run typecheck` clean (exit 0).**

---

## Verification (on-screen, full stack)

Stack: `payment-api` + `bff-adminpanel` images **rebuilt with the new code** (built 08:23 UTC, after the 07:25 UTC source edits), `keycloak-init` had run, plus `identity-api` + `reference-data-api` + `keycloak` + `redis` + Postgres. FE dev `localhost:3000` → BFF `:17001` → `payment-api` → Postgres. Signed in as `admin.user@inktavia.com` (persisted admin session), language **tr**.

| # | Step | Result |
|---|------|--------|
| 1 | Open **Kâr Koruma Politikaları** | ✅ Renders, tr i18n; KPI strip **1 Aktif · 1 Para Birimi · 1 Toplam Sürüm** (derived client-side; no stats endpoint) |
| 2 | Seeded policy lists | ✅ Version history `TRY — 1 SÜRÜM` → **#1 AKTIF** "Default profit-protection policy (placeholder)", Jul 29 2026 – Devam ediyor, **ACTIVE** |
| 3 | **Decision-state legend** | ✅ All 4 render: ONAYLANDI / DÜZELTMELI / REDDEDİLDİ / YAPILANDIRMA HATASI (§19.11), each with description |
| 4 | **Resolve preview** (TRY) | ✅ Returns active #1 + all thresholds: gates 0·0.00% / 0·0.00% / **10·1.00%**, expenses **2.90% + 0.25**, refund **0.50%**, var-share **50.00%**, order "Önce platform indirimi, sonra komisyon avantajı" — exactly the seed values |
| 5 | Create new TRY version (default open-ended window overlapping #1) | ✅ **Typed conflict banner (5050)** — "Kural çakışması", message *"A conflicting active profit-protection policy already exists (Id=1, Code=)…"*, conflicting **#1** parsed out, guidance list; **save blocked** (submit disabled while `conflict !== null`, modal stays open) |
| 6 | Open detail **#1** | ✅ KPI (TRY / adjustment order / Jul 29 2026 / ACTIVE) + gates (with max-formula hint) + expenses + audit + actions render |
| 7 | **Update** thresholds (txn rate 1→2 %, var-share 50→55 %) | ✅ Detail refetch shows **İşlem 10 · 2.00%** and **Müşteri Değişken Maliyet Payı 55.00%** (no self-conflict) |
| 8 | **Deactivate** #1 | ✅ Navigates to list; KPI **0 Aktif**; #1 → **INACTIVE**, active badge gone |
| 9 | **Reactivate** #1 | ✅ Re-runs overlap guard (no other active TRY policy → no collision) → DURUM back to **ACTIVE**, Deactivate re-enabled, updated values persist |

**Typed conflict banner confirms the conflict-envelope fix carries 5050.** Backend logs on step 5 show the full round-trip: `payment-api` throws `AizenBusinessException` (ProfitProtectionPolicyConflict, "Id=1") → BFF Refit client → `CreateProfitProtectionPolicyBffCommandHandler` → `AdminPaymentController.CreateProfitProtectionPolicy` (line 816) → `AdminPanelBffFailEnvelopeHandler` maps **5050** → FE `RuleConflictBanner` with `isConflict = true` (conflict-specific title + guidance, not the generic "rejected" notice).

> **Note vs P3:** the P3 report saw the resolve preview 500 (standalone `payment-api` missing `ISystemParameterReferenceService`) and the conflict surfaced as a generic HTTP-500 banner. **Neither recurs here:** `reference-data-api` was in the stack, so the resolve preview is fully live, and the conflict came through as a **typed 5050 envelope** → conflict-specific banner. The verification `update` mutated the seeded TRY policy's placeholder thresholds (2.00% / 55%) — expected per the "update the active policy" step; values are admin-tunable placeholders, left as-is.

## Result

All three parts complete and green — **module build 0 · 9 unit tests pass · BFF build 0 · FE typecheck 0** — and the full policy lifecycle (list → create/conflict-5050 → update → deactivate → reactivate → resolve → legend) verified on-screen. Commission/PlatformFee + existing payment screens unregressed; provider-web/CargoDry untouched.

**Next in the wave:** P6 — CustomerDiscount + BenefitBudget.
