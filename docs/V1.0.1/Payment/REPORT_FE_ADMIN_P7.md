# REPORT — FE_ADMIN_P7 · ProviderCommissionBenefitRule + Entitlement admin screens (vertical slice)

**Date:** 2026-08-01 · **Spec:** `docs/V1.0.1/Payment/FE_ADMIN_P7_PROVIDER_COMMISSION_BENEFIT.md`
**Scope:** Two entities, two parts. **Part A — ProviderCommissionBenefitRule** (the most feature-rich rule of the wave:
multi-category, `Stackable`/`Exclusive`, `AdjustmentPercentagePoints`, min-rate floor, GMV/usage caps → full `rule-crud`
reuse incl. Priority + Specificity). **Part B — ProviderCommissionBenefitEntitlement** (grant / revoke / list a benefit
rule to a specific provider). Backend-first vertical-slice template, Part A first then Part B. **This completes the admin
rule-CRUD wave (P2/P3/P5/P6/P7).**

Untouched per constraints: provider-web, CargoDry, Identity, Keycloak, and the Commission / PlatformFee /
ProfitProtection / CustomerDiscount surfaces, plus the mock `EntitlementMonitoringPage` (a different concept —
subscription-entitlement matrix — left as-is; a **new** ProviderCommissionBenefit surface was built). Enum fields stay
**string** on the wire; rates are fractions, percentage-points are pp; envelope-tolerant. Benefit rules only ever LOWER
commission — that framing is kept in the UI copy (a decoupling note on the list + detail).

---

## PART A — ProviderCommissionBenefitRule (full pattern)

### 1 · Module (Payment)
`ProviderCommissionBenefitController` (`api/v1/payment/commission-benefits`) gained the P6-mirror admin surface
(resolve + create/update/deactivate + grant/revoke already existed):

| Verb | Route | Handler |
|------|-------|---------|
| GET  | `rules` (filters `providerProfileId`, `providerPlanId`, `categoryCode`, `currencyCode`, `stackable`, `isActive`) | `GetProviderCommissionBenefitRulesListQuery` → `GetAllAsync`, in-memory filters, sort **currency, then EffectiveFrom desc**, no paging |
| GET  | `rules/{id}` | `GetProviderCommissionBenefitRuleByIdQuery` → `GetByIdAsync`, throws `ProviderCommissionBenefitRuleNotFound` (5071) |
| POST | `rules/{id}/reactivate` | `ReactivateProviderCommissionBenefitRuleCommand` — re-runs `FindOverlappingActiveRuleAsync` → `ProviderCommissionBenefitRuleConflict` (5070); guards `NotInactive` |

- Entity: added `Reactivate()` (IsActive=true, re-derives Status from effective dates) — mirrors P5/P6.
- Application: `ProviderCommissionBenefitRuleDto` + `…DtoMapper` (+ `…ListResult`, `ApplicableCategoryCodes` flattened to
  `List<string>`) shared by list & detail; list/detail query handlers; reactivate command + handler + validator.
- New error code `ProviderCommissionBenefitRuleNotInactive = 5079` (the only free slot after the P7 block 5070–5078).
- No migration, no repo changes for the rule (`GetAllAsync`/`GetByIdAsync` already present). **Module build 0.**
- Repo unit tests (appended to `ProviderCommissionBenefitTests.cs`): rule GetById hit+null, GetAll incl. inactive,
  Reactivate re-derives status, reactivation-overlap conflict via `FindOverlappingActiveRuleAsync` — **passed (12 total).**

### 2 · BFF (AdminPanel)
- `IAdminPaymentBffRemoteCall`: `ListProviderCommissionBenefitRulesAsync` /
  `GetProviderCommissionBenefitRuleDetailAsync(id)` / `ReactivateProviderCommissionBenefitRuleAsync(id)` (Refit, targeting
  `/api/v1/payment/commission-benefits/rules[…]`).
- Typed DTOs (enums as string): `ProviderCommissionBenefitRuleListItemBffDto`, `…ListBffResult`, `…DetailBffDto`
  (Stackable/Exclusive bools; Priority/Status strings; `ApplicableCategoryCodes: List<string>`).
- Query handlers `GetProviderCommissionBenefitRulesListBffQuery` / `…DetailBffQuery`; command
  `ReactivateProviderCommissionBenefitRuleBffCommand`.
- `AdminPaymentController`: `GET commission-benefits/rules`, `GET …/rules/{id}`, `POST …/rules/{id}/reactivate` — all under
  the class-level `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse` envelope, empty-list fallback on the list
  route.
- The generic `AdminPaymentBffFailEnvelopeHandler` carries **5070** to the FE unchanged (no per-code list — it forwards
  the module's numeric `PaymentErrorCode`). **BFF build 0.**

### 3 · FE (inktavia-marine-admin-web)
- Screens `src/pages/app/payments/ProviderCommissionBenefitRulesPage.tsx` (+ `ProviderCommissionBenefitRuleDetailPage.tsx`).
- Hook `src/features/payments/hooks/useProviderCommissionBenefitRulesQuery.ts` (rule list/detail/resolve +
  create/update/deactivate/reactivate, **and** the Part B entitlement list/detail + grant/revoke).
- Wiring: `payment.types.ts` (DTO-mirroring interfaces + resolve/grant params), `endpoints.ts`, `queryKeys.ts`
  (`commissionBenefit`), `paymentApi.ts`, `routes.tsx` + `routeObjects.tsx` (`commission-benefit-rules` + `:ruleId` +
  `commission-benefit-entitlements`), two payments-dashboard nav buttons, i18n `payments.json` **tr + en** parity.
- **rule-crud blocks — FULL reuse:** `RuleConflictBanner`, `EffectiveDateRangeField`, `PriorityField`, `SpecificityHint`,
  `useRuleMutation` (+ `extractRuleConflict`, `RULE_CONFLICT_CODES` — **5070 already registered**).

**Stackable/Exclusive XOR + pp framing.** The create form takes the commission reduction as a positive **pp** value and
stores it as a negative rate (`adjustmentPercentagePoints = −pp/100`), and the minimum rate as a percent (floor). The
**Stackable / Exclusive toggles are mutually exclusive on the client** — turning one on turns the other off, plus a
submit-time guard rejecting both-true (belt-and-suspenders for the domain's `ProviderCommissionBenefitRuleInvalid`).
Multi-category targeting is a comma-separated input parsed to `string[]`. The **detail edit modal locks the structural
targeting** (Provider / Plan / Currency shown read-only under a "TARGETING (LOCKED)" header) and edits the non-structural
fields incl. categories. The **resolve preview** shows base rule/rate → applied benefit rule codes → effective rate + the
benefited / non-benefited GMV split + requested/applied benefit amount (`EffectiveCommissionResolveBffResult`).
**`npm run typecheck` clean (exit 0).**

---

## PART B — ProviderCommissionBenefitEntitlement (grant / revoke / list)

### 1 · Module
- Added `GetAllAsync` to `IProviderCommissionBenefitEntitlementRepository` + impl
  (`AsNoTracking().OrderByDescending(GrantedFrom)`).
- `ProviderCommissionBenefitController`: `GET entitlements` (`GetProviderCommissionBenefitEntitlementsListQuery`, filters
  `providerProfileId`/`benefitRuleId`/`isActive`, enriched with the referenced benefit rule's code/name),
  `GET entitlements/{id}` (`…ByIdQuery`, throws `ProviderCommissionBenefitEntitlementNotFound` = 5073). Grant/Revoke
  already existed; **reserve/consume/release left untouched** (runtime ops).
- `ProviderCommissionBenefitEntitlementDto` + `…DtoMapper` (usage counters + derived `RemainingUsage`/`RemainingGmv`,
  read-only). No migration.
- Repo test: entitlement GetAll (ordered) + GetById hit+null — **passed.**

### 2 · BFF
- Remote-calls `ListProviderCommissionBenefitEntitlementsAsync` /
  `GetProviderCommissionBenefitEntitlementDetailAsync(id)` (Grant/Revoke already existed); typed
  `…EntitlementListItemBffDto` / `…ListBffResult` / `…DetailBffDto`; query handlers; routes
  `GET commission-benefits/entitlements`, `GET …/entitlements/{id}` (grant/revoke routes already existed). Grant conflicts
  carried by the generic handler.

### 3 · FE
- Single screen `src/pages/app/payments/ProviderCommissionBenefitEntitlementsPage.tsx` (own page + nav button; also linked
  from the rule detail's "Entitlements" card and the rules page header). Hook methods live in the shared
  `useProviderCommissionBenefitRulesQuery.ts`.
- **rule-crud blocks — reused:** `RuleConflictBanner` + `useRuleMutation` for the grant (grant conflicts → typed banner,
  save blocked). List shows provider, benefit rule (code + name), granted window, used/limit + consumed/max GMV, and
  status; **revoke** goes through a confirm dialog; **usage counters render read-only**. **Deliberately NOT built:**
  reserve/consume/release controls (runtime). **`npm run typecheck` clean (exit 0).**

---

## Verification (on-screen, full stack)

Stack: `payment-api` + `bff-adminpanel` images **rebuilt with the new code** (`docker compose build` → 0 errors) and
recreated, alongside the already-running `identity-api`, `keycloak`, `redis`, Postgres, RabbitMQ, etc. FE dev
`localhost:3000` → BFF `:17001` (`/api/v1/admin-panel/payment/commission-benefits/*` — both new list routes returned
**401** unauth, i.e. registered, not 404) → `payment-api` → Postgres (`inktavia_store`, schema `payment`). Signed in as
Admin User, language **tr**. BE-P7 seed present: `PCB-EXAMPLE-1PP` (−1pp / min 0.08 / stackable / **Inactive**).

### Part A — ProviderCommissionBenefitRule

| # | Step | Result |
|---|------|--------|
| 1 | Open **Komisyon Fayda Kuralları** | ✅ tr i18n + the decoupling note ("…yalnızca komisyon oranını düşürür… asla premium öne çıkarma sağlamaz"). KPI **0 Aktif · 1 Toplam · 1 Para Birimi**. Catalog lists the seeded **PCB-EXAMPLE-1PP / −1.00 pp / MIN 8.00% / Global / BİRLEŞTİRİLEBİLİR / STANDARD / INACTIVE** |
| 2 | New rule: plan 2, categories **TOWAGE, PILOTAGE**, −2pp, min 8%. Real-click **Özel** (Exclusive) | ✅ **Stackable auto-unchecked** (client XOR); saved → **PCB-2026-B002 / −2.00 pp / 8.00% / Plan + Kategori / ÖZEL / ACTIVE**, KPI **1 Aktif / 2 Toplam** |
| 3 | Create a 2nd rule at the same specificity (plan 2, same categories, Standard, overlapping window) | ✅ **Typed 5070 conflict banner** — "Kural çakışması", *"A conflicting active benefit rule already exists (Id=2, Code=PCB-2026-B002)"*, parsed conflicting **#2**, guidance list; **save button greyed/blocked** (`conflict !== null`) |
| 4 | Open **PCB-2026-B002**, **Edit** (min 8 → 9 %) | ✅ Edit modal shows **"HEDEFLEME (KILITLI)"** read-only (Sağlayıcı — / Plan #2 / TRY); save → detail refetch shows **MIN ORAN 9.00%** (no self-conflict) |
| 5 | **Deactivate** | ✅ Confirm dialog → navigates to list; KPI **0 Aktif**; PCB-2026-B002 → **INACTIVE** (9.00% persisted) |
| 6 | **Reactivate** | ✅ Confirm dialog (notes the overlap guard re-runs) → re-runs `FindOverlappingActiveRuleAsync` (unique scope → no collision) → DURUM back to **ACTIVE**, Deactivate re-enabled |
| 7 | **Resolve preview** (provider 1, plan 2, category TOWAGE, 10000 TRY) | ✅ **Temel Oran 12.00%** → **Uygulanan Faydalar PCB-2026-B002** → **Efektif Oran 10.00%** (= max(0.12−0.02, 0.09)); GMV split **Faydalanan 10,000.00 / Faydalanmayan 0.00**; **Talep/Uygulanan Fayda 200.00** |

### Part B — ProviderCommissionBenefitEntitlement

| # | Step | Result |
|---|------|--------|
| 1 | Open **Komisyon Fayda Yetkileri** | ✅ tr i18n; KPI **0 Aktif · 0 Toplam · 0 Sağlayıcı**; empty ("Henüz yetki verilmedi.") + the usage-read-only footer note |
| 2 | **Grant**: provider 1 + rule ID 2, usage limit 5, max GMV 100000 | ✅ Appears → **PCE-2026-A001 / #1 / PCB-2026-B002 (P7 Verify Exclusive Rule — rule code+name enriched) / Aug 01 2026 – Devam ediyor / 0 / 5 · 0 / 100,000 (read-only) / ACTIVE**, KPI **1 Aktif / 1 Toplam / 1 Sağlayıcı** |
| 3 | **Revoke** (İptal Et → confirm) | ✅ DURUM flips to **REVOKED**; revoke control replaced by "—"; usage counters still render **read-only**; KPI **0 Aktif** |
| 4 | Payments-dashboard nav | ✅ New buttons **"Komisyon Faydaları"** + **"Fayda Yetkileri"** render alongside the existing ones |

The typed 5070 banner confirms the fail-loud envelope carries the module's numeric `PaymentErrorCode` (5070) to the FE and
the `RuleConflictBanner` renders the conflict-specific title + guidance + parsed conflicting-rule id — not a generic
reject. Reserve/consume/release were **not** exposed (runtime concern); the entitlement usage columns are purely
read-only. The mock `EntitlementMonitoringPage` was left untouched — this is a separate ProviderCommissionBenefit surface.

**Verification artifacts left in place** (admin-tunable records): benefit rule **PCB-2026-B002** (plan 2, categories
TOWAGE/PILOTAGE, Exclusive, −2pp, min 9%, reactivated → ACTIVE) and entitlement **PCE-2026-A001** (provider #1 →
PCB-2026-B002, REVOKED).

---

## Result

All three layers complete and green for both entities — **module build 0 · repo unit tests pass (12) · BFF build 0 · FE
typecheck 0**. Commission / PlatformFee / ProfitProtection / CustomerDiscount and existing screens (incl. the mock
`EntitlementMonitoringPage`) unregressed; provider-web / CargoDry / Identity / Keycloak untouched. **This completes the
admin rule-CRUD wave (P2/P3/P5/P6/P7).**

**Next options:** provider Wave B (finance transparency), admin operational queues (P10 refund/chargeback + I1 sub-merchant
KYC), or P12 financial reporting dashboard.
