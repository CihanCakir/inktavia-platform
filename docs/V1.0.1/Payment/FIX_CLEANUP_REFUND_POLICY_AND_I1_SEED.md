# CLEANUP — RefundAllocationPolicy CRUD (FE) + I1 sub-merchant onboarding seed fix

> Two small, independent follow-ups from the ops-queues phase. **Part A** is FE-only (admin-web); **Part B** is a
> backend dev-seed fix (Payment repository). Do NOT touch anything else (provider-web, CargoDry, Keycloak, other admin
> screens). Do Part A + verify, then Part B.

## Part A — RefundAllocationPolicy admin CRUD (FE-only; the deferred rule-crud follow-up)
The BFF + module are **already complete** — this is just the missing admin screen. It's a **versioned policy** (like
ProfitProtection): a `NegativeBalanceLimit` + an effective window + a **set of per-cause allocation rules**.

**Endpoints (base `/api/v1/admin-panel`, AdminPanelAccess) — already live:**
`GET payment/admin/refund-allocation-policies` (list) · `GET .../resolve` (active preview) ·
`GET .../refund-allocation-policies/{id}` (detail) · `POST .../refund-allocation-policies` (create) ·
`PUT .../refund-allocation-policies/{id}` (update) · `POST .../refund-allocation-policies/{id}/deactivate`.
**Missing: reactivate** — add it (module `RefundAdminController` + BFF) **only if quick**, mirroring the other policies;
otherwise ship without it and note the gap.

**BFF DTOs (`RefundAllocationPolicyBffDtos.cs`) — exact fields:**
- `CreateRefundAllocationPolicyBffRequest`: `CurrencyCode`, `NegativeBalanceLimit`, `EffectiveFrom`, `EffectiveTo?`,
  `PolicyName?`, `Notes?`, `Rules: List<RefundAllocationPolicyRuleBffInput>?` where each rule =
  `{ Cause: string-enum, Mode: string-enum, FixedPlatformFeeAmount: decimal? }`.
- `UpdateRefundAllocationPolicyBffRequest`: `NegativeBalanceLimit`, `EffectiveFrom`, `EffectiveTo?`, `Notes` (currency +
  rules are create-time; not editable on update). Read/response DTOs live in `Aizen.Modules.Payment.Abstraction.Dto`.

**FE (admin-web):** new `RefundAllocationPolicyPage` (+ detail) — a **policy editor + version history**, mirroring the P5
ProfitProtection screen. Endpoints/`paymentApi`/hook set. Reuse the `rule-crud` blocks (`RuleConflictBanner` for
`RefundAllocationPolicyConflict` = **5101**, already in `RULE_CONFLICT_CODES`; `EffectiveDateRangeField`;
`useRuleMutation`) — **no Priority/Specificity** (versioned policy). **Form:** `CurrencyCode` (create-only),
`NegativeBalanceLimit`, `EffectiveFrom/To`, `PolicyName?`, `Notes?`, and a **nested "allocation rules" mini-table**
(add/remove rows; each row = `Cause` enum select → `Mode` enum select + optional `FixedPlatformFeeAmount`). Update form
omits currency + rules. **Resolve preview** shows the active policy for a currency. Int/string enums → labeled maps
(reuse the ops-queues `enumMaps` where the same enums apply, e.g. RefundCause). Routes + payments-dashboard nav. i18n
tr+en full parity.

## Part B — I1 sub-merchant onboarding dev seed (so the KYC queue populates from a real seed)
**Symptom:** the sub-merchant KYC onboarding queue was empty because no provider in the **runtime** DB (`inktavia_store`)
has sub-merchant onboarding data; the ops-queues verification used ad-hoc rows (#990001–990003). **Fix:** add a proper
idempotent dev seed so the queue has realistic demo data in the right DB, and reconcile the ad-hoc rows.

- Add a `SubMerchantOnboardingMockSeed` (or extend an existing Payment `Seed/*MockSeed`) **mirroring
  `PayoutRecordMockSeed`**: dev/Local `EnvironmentGuard`, **idempotent** (find-or-create; skip if a demo onboarding row
  already exists), targeting the Payment `ProviderPaymentProfile` sub-merchant fields — create a few provider payment
  profiles across the onboarding states the queue shows (e.g. `SubMerchantCreated` awaiting verify, plus one already
  `Verified` and one `Rejected`), with **masked** LegalName/TaxNumberMasked/SubMerchantKeyMasked + IsSplitEligible/HasIban.
  Wire it into `SeedPaymentAsync` + DI, no migration.
- Confirm the seed runs against **`inktavia_store`** (the Payment appsettings already point there — Development/Local
  both `Database=inktavia_store`). **Investigate the "landed in `aizen`" report:** find whatever path wrote onboarding
  data to an `aizen` database (a stray connection string, an old `POSTGRES_DB`, or a manual insert) and confirm the real
  seed now targets `inktavia_store`. If a legacy `aizen` DB exists in Postgres, note it (do not drop it) — the fix is
  that the runtime + seed use `inktavia_store`.
- Reconcile the ad-hoc #990001–990003 rows: fold them into the idempotent seed (stable demo ids) or remove them so
  there's a single source of demo truth. Keep everything demo-labelled + reversible.

## Verification (on-screen; keycloak-init ran, fresh admin login admin.user@inktavia.com)
- **Part A:** RefundAllocationPolicy screen lists the seeded policy; create a policy with a couple of per-cause
  allocation rules + a NegativeBalanceLimit; create an overlapping active policy for the same currency → **typed 5101
  conflict banner**, save blocked; update the limit/notes; deactivate (+ reactivate if built); resolve preview returns
  the active policy.
- **Part B:** the sub-merchant KYC queue now lists the **seeded** onboarding providers (SubMerchantCreated/Verified/
  Rejected) from `inktavia_store` — no ad-hoc rows needed; verify/reject still work.
- `npm run typecheck` clean; module/BFF build 0 (if reactivate/seed touched them); existing screens unregressed.

## Report
`docs/V1.0.1/Payment/REPORT_CLEANUP_REFUND_POLICY_AND_I1_SEED.md`: Part A screen + the nested allocation-rules form +
whether reactivate was added; Part B the seed (entities/states/masked fields), the `aizen`-vs-`inktavia_store` finding,
and how the ad-hoc rows were reconciled. After this, the remaining wave items are **P11 offer-boost** (provider) and the
**P12 financial reporting dashboard** (finale).
