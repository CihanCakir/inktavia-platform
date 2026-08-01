# REPORT — FE_ADMIN_P6 · CustomerDiscountRule + CustomerBenefitBudgetPolicy admin screens (vertical slice)

**Date:** 2026-08-01 · **Spec:** `docs/V1.0.1/Payment/FE_ADMIN_P6_CUSTOMER_DISCOUNT.md`
**Scope:** Two entities, two parts. **Part A — CustomerDiscountRule** (full `rule-crud` reuse incl. Priority +
Specificity + funding). **Part B — CustomerBenefitBudgetPolicy** (lighter per-plan policy, create + list + view only).
Same backend-first vertical-slice template as P3/P5. Both parts built + verified on-screen.

Untouched per constraints: provider-web, CargoDry, Identity, Keycloak, and the Commission / PlatformFee /
ProfitProtection surfaces. Enum fields stay **string** on the wire; rates are fractions; envelope-tolerant.

---

## PART A — CustomerDiscountRule (full pattern)

### 1 · Module (Payment)
`CustomerDiscountRuleController` (`api/v1/payment/customer-discounts`) gained the P3/P5-mirror admin surface (resolve +
create/update/deactivate already existed):

| Verb | Route | Handler |
|------|-------|---------|
| GET  | `rules` (filters `customerPlanId`, `categoryCode`, `currencyCode`, `fundingMode`, `isActive`) | `GetCustomerDiscountRulesListQuery` → `GetAllAsync`, in-memory filters, sort **currency, then EffectiveFrom desc**, no paging |
| GET  | `rules/{id}` | `GetCustomerDiscountRuleByIdQuery` → `GetByIdAsync`, throws `CustomerDiscountRuleNotFound` (5061) |
| POST | `rules/{id}/reactivate` | `ReactivateCustomerDiscountRuleCommand` — re-runs `FindOverlappingActiveRuleAsync` → `CustomerDiscountRuleConflict` (5060); guards `NotInactive` |

- Entity: added `Reactivate()` (IsActive=true, re-derives Status from effective dates) — mirrors ProfitProtection.
- Application: `CustomerDiscountRuleDto` + `CustomerDiscountRuleDtoMapper` (+ `…ListResult`) shared by list & detail;
  list/detail query handlers; reactivate command + handler + validator.
- New error code `CustomerDiscountRuleNotInactive = 5069` (only free slot before the P7 block at 5070).
- No migration, no repo changes (`GetAllAsync`/`GetByIdAsync` already present). **Module build 0.**
- Repo unit tests (`CustomerDiscountRuleRepositoryTests.cs`, mirror P5): GetById hit+null, GetAll incl. inactive,
  reactivation-overlap conflict, Reactivate re-derives status — **passed.**

### 2 · BFF (AdminPanel)
- `IAdminPaymentBffRemoteCall`: `ListCustomerDiscountRulesAsync` / `GetCustomerDiscountRuleDetailAsync(id)` /
  `ReactivateCustomerDiscountRuleAsync(id)` (Refit, targeting `/api/v1/payment/customer-discounts/rules[…]`).
- Typed DTOs (enums as string): `CustomerDiscountRuleListItemBffDto`, `…ListBffResult`, `…DetailBffDto`.
- Query handlers `GetCustomerDiscountRulesListBffQuery` / `…DetailBffQuery`; command `ReactivateCustomerDiscountRuleBffCommand`.
- `AdminPaymentController`: `GET customer-discounts/rules`, `GET …/rules/{id}`, `POST …/rules/{id}/reactivate` — all
  `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse` envelope, empty-list fallback on the list route.
- The generic `AdminPaymentBffFailEnvelopeHandler` carries **5060** to the FE unchanged. **BFF build 0.**

### 3 · FE (inktavia-marine-admin-web)
- Screens `src/pages/app/payments/CustomerDiscountRulesPage.tsx` (+ `CustomerDiscountRuleDetailPage.tsx`).
- Hook `src/features/payments/hooks/useCustomerDiscountRulesQuery.ts` (list/detail/resolve + create/update/deactivate/reactivate).
- Wiring: `payment.types.ts` (DTO-mirroring interfaces + `CustomerDiscountType`/`CustomerDiscountFundingMode`),
  `endpoints.ts`, `queryKeys.ts` (`customerDiscount`), `paymentApi.ts`, `routes.tsx` + `routeObjects.tsx`
  (`customer-discount-rules` + `:ruleId`), payments-dashboard nav button, i18n `payments.json` **tr + en** parity.
- **rule-crud blocks — FULL reuse:** `RuleConflictBanner`, `EffectiveDateRangeField`, `PriorityField`, `SpecificityHint`,
  `useRuleMutation` (+ `extractRuleConflict`, `RULE_CONFLICT_CODES` — **5060 already registered**).

**Funding-mode + consent UX.** The create form's `FundingMode` select (Platform / Provider / Shared / Supplier) is
model-driven: **Shared** reveals a single **Platform funding %** input with the **Provider %** derived read-only
(`= 100% − platform`, guaranteeing the domain's sum-to-1.0 rule) plus a **Requires-provider-consent** toggle carrying
the note *"Without consent, the provider share is dropped — never shifted to the platform."* `ProviderFunded` also shows
the consent toggle; `PlatformFunded`/`SupplierFunded` show neither. `DiscountType` toggles Percent(`DiscountRate` %→
fraction) vs Fixed(`FixedDiscountAmount`). The **detail edit modal locks the structural targeting** (Plan / Category /
Currency shown read-only under a "TARGETING (LOCKED)" header) and edits only the non-structural fields. The **resolve
preview** shows the funding split (platform-funded / provider-funded / unapplied-due-to-consent), applied discount, and
budget remaining. **`npm run typecheck` clean (exit 0).**

---

## PART B — CustomerBenefitBudgetPolicy (lighter per-plan screen)

### 1 · Module
- Added `GetAllAsync` to `ICustomerBenefitBudgetPolicyRepository` + impl (`OrderByDescending(EffectiveFrom)`).
- `CustomerBenefitBudgetController` (`api/v1/payment/benefit-budget`): `GET policies` (`GetCustomerBenefitBudgetPoliciesListQuery`,
  filters `customerPlanId`/`currencyCode`/`isActive`, sort **plan, then EffectiveFrom desc**), `GET policies/{id}`
  (`…ByIdQuery`, reuses `CustomerBenefitBudgetNotFound` = 5063). Create already existed.
- **Deliberately NOT added** (per spec): update / deactivate / reactivate — not in the module today. Reserve/consume/
  release left untouched (runtime ops). DTO + mapper (`CustomerBenefitBudgetPolicyDto`/…Mapper/…ListResult). No migration.
- Repo test `CustomerBenefitBudgetPolicyRepositoryTests.cs`: GetById hit+null, GetAll incl. inactive — **passed.**

### 2 · BFF
- Remote-calls `ListCustomerBenefitBudgetPoliciesAsync` / `GetCustomerBenefitBudgetPolicyDetailAsync(id)`; typed
  `…ListItemBffDto` / `…ListBffResult` / `…DetailBffDto`; query handlers; routes `GET benefit-budget/policies`,
  `GET …/policies/{id}` (create route already existed). **5068 carried by the generic handler.**

### 3 · FE
- Single screen `src/pages/app/payments/CustomerBenefitBudgetPage.tsx` (no detail route). Hook
  `useCustomerBenefitBudgetQuery.ts` (list/detail/create only). Wiring parallels Part A (`customerBenefitBudget` query
  keys, `benefit-budget/policies` endpoints, route, nav button, tr+en i18n).
- **rule-crud blocks — reused:** `RuleConflictBanner` (5068), `EffectiveDateRangeField`, `useRuleMutation`.
  **Skipped by design:** `PriorityField`, `SpecificityHint` (a per-plan policy has no specificity/priority).
- List grouped by plan; each row shows the configured rate + per-period/category/transaction limits **read-only**.
  Create modal: plan / currency / `BenefitBudgetRate` (%→fraction) / optional limits / `RefundRestorePolicy`
  (Restore|Consume) / dates / notes.
- **Deferred-edit gap surfaced honestly** in a "Scope Notes" card (no fake controls): (1) live budget usage/remaining is
  a runtime concern managed by reserve→consume/release, not this screen; (2) editing/deactivating an existing budget
  policy is deferred — the screen supports **create + view only** today. **`npm run typecheck` clean (exit 0).**

---

## Verification (on-screen, full stack)

Stack: `payment-api` + `bff-adminpanel` images **rebuilt with the new code** and recreated, alongside `identity-api`,
`reference-data-api`, `keycloak`, `redis`, Postgres, RabbitMQ. FE dev `localhost:3000` → BFF → `payment-api` → Postgres
(`inktavia_store`, schema `payment`). Signed in as `admin.user@inktavia.com` (OTP `927099` from `identity-api` logs),
language **tr**. BE-P6 seed present: discount rules plan 2 = 0.05 / plan 3 = 0.10 (PlatformFunded); budget policies
plans 1–3 = 0.10.

### Part A — CustomerDiscountRule

| # | Step | Result |
|---|------|--------|
| 1 | Open **Müşteri İndirim Kuralları** | ✅ tr i18n; KPI **2 Aktif · 2 Toplam · 1 Para Birimi** (derived client-side). Catalog lists seeded **#2 YÜZDE 10.00% / Plan / PLATFORM KARŞILAR / ACTIVE** (PLATINUM) and **#1 YÜZDE 5.00% / Plan / ACTIVE** (GOLD) |
| 2 | **Resolve preview** (Plan 2, base 10000, consent on) | ✅ Winning rule **#1**, requested **500.00**, funding split **Platform 500.00 / Provider 0.00 / Unapplied-no-consent 0.00**, applied **500.00**, budget remaining **0.00**, plan-revenue allocation **0.00** |
| 3 | Create rule at same specificity as seeded plan-2 (Percent, PlatformFunded, Standard, TRY, overlapping window) | ✅ **Typed 5060 conflict banner** — "Kural çakışması", message *"A conflicting active customer discount rule already exists (Id=1, Code=)"*, conflicting **#1** parsed out, guidance list; **save blocked** (button disabled while `conflict !== null`) |
| 4 | Switch FundingMode → **Shared** | ✅ Reveals **Platform Fonlama % 50 / Sağlayıcı Fonlama % 50** (derived read-only) + **consent toggle** with the note *"Onay yoksa sağlayıcı payı düşürülür — asla platforma kaydırılmaz"*; changing a field **cleared the conflict banner** |
| 5 | Change plan → 5, **create** (Percent, Shared) | ✅ Succeeds → **CDR-2026-C003** (generated code) appears, KPI **3 Aktif / 3 Toplam** |
| 6 | Open detail **CDR-2026-C003** | ✅ KPI (5.00% / Paylaşımlı / STANDARD / ACTIVE) + Definition + Targeting&Funding (Plan #5, 50%/50%) + Actions render |
| 7 | **Edit** (targeting locked; rate 5→7 %) | ✅ Edit modal shows **"HEDEFLEME (KILITLI)"** read-only (Plan #5 / — / TRY); save → detail refetch shows **7.00%** (no self-conflict) |
| 8 | **Deactivate** | ✅ Navigates to list; KPI **2 Aktif**; CDR-2026-C003 → **INACTIVE** |
| 9 | **Reactivate** | ✅ Re-runs overlap guard (plan 5 unique → no collision) → DURUM back to **ACTIVE**, Deactivate re-enabled, 7.00% persists |

### Part B — CustomerBenefitBudgetPolicy

| # | Step | Result |
|---|------|--------|
| 1 | Open **Müşteri Fayda Bütçeleri** | ✅ tr i18n; KPI **3 Aktif · 3 Plan · 3 Sürüm**; seeded policies **Plan #1/#2/#3 — Oran 10.00% / İade Geri Yükle / ACTIVE** grouped by plan, limits read-only |
| 2 | **Scope Notes** card | ✅ Renders both notes: live usage/remaining is a runtime op (reserve→consume/release), and edit/deactivate is deferred (create + view only) — **no fake controls** |
| 3 | Create policy for plan 1 (already active) | ✅ **Typed 5068 conflict banner** — "Kural çakışması", message *"A conflicting active benefit budget policy already exists (Id=1, Code=)"*, conflicting **#1** parsed, guidance list; **save blocked** |
| 4 | Change plan → 5, **create** | ✅ Succeeds → **Plan #5 / CBP-2026-D004** (generated code, Oran 10.00%, Geri Yükle, ACTIVE), KPI **4 Aktif / 4 Plan / 4 Sürüm** |
| 5 | Payments-dashboard nav | ✅ New buttons **"Müşteri İndirimleri"** + **"Fayda Bütçeleri"** render alongside Kâr Koruma |

Both typed conflict banners (**5060** discount, **5068** budget) confirm the fail-loud envelope carries the module's
numeric `PaymentErrorCode` to the FE; the `RuleConflictBanner` renders the conflict-specific title + guidance + parsed
conflicting-rule id, not a generic reject. Backend logs on steps A3/B3 show the round-trip: `payment-api` throws
`AizenBusinessException` (5060 / 5068 with the conflict id) → BFF Refit → fail-envelope handler → FE typed banner.

**Verification artifacts left in place** (admin-tunable records, per the P5 precedent): discount rule **CDR-2026-C003**
(plan 5, Shared 50/50, consent-required, 7.00%, reactivated → ACTIVE) and budget policy **CBP-2026-D004** (plan 5,
10.00%, Restore, ACTIVE).

## Result

All three layers complete and green for both entities — **module build 0 · repo unit tests pass · BFF build 0 · FE
typecheck 0** — and the full lifecycle verified on-screen: Part A (list → resolve split → create/conflict-5060 → update →
deactivate → reactivate) and Part B (list → create/conflict-5068 → usage/remaining read-only + deferred-edit note).
Commission / PlatformFee / ProfitProtection and existing screens unregressed; provider-web / CargoDry / Identity /
Keycloak untouched.

**Next in the wave:** P7 — ProviderCommissionBenefit rule.
