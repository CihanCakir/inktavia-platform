# REPORT — CLEANUP: RefundAllocationPolicy CRUD (FE) + I1 sub-merchant onboarding seed fix

**Spec:** `docs/V1.0.1/Payment/FIX_CLEANUP_REFUND_POLICY_AND_I1_SEED.md`
Two independent follow-ups from the ops-queues phase. **Part A** delivered + on-screen verified, then **Part B**.
`npm run typecheck` clean; module + BFF build **0 errors**; existing screens unregressed; provider-web/CargoDry untouched.

---

## Part A — RefundAllocationPolicy admin CRUD (admin-web) + reactivate (module+BFF)

The deferred rule-crud follow-up. A **versioned, single-active-per-currency policy** (like P5 ProfitProtection):
a `NegativeBalanceLimit` + an effective window + a set of **per-cause allocation rules**.

### Screen (admin-web, FE)
New `RefundAllocationPolicyPage` (list/version-history + create modal + resolve preview) and
`RefundAllocationPolicyDetailPage` (view + edit-header + deactivate + reactivate), mirroring the P5 screens. Data layer:
`endpoints.ts`, `payment.types.ts`, `queryKeys.ts`, `paymentApi.ts`, and `useRefundAllocationPolicyQuery.ts` (list /
detail / resolve queries + create / update / deactivate / reactivate mutations). Reuses the `rule-crud` blocks
(`RuleConflictBanner`, `EffectiveDateRangeField`, `useRuleMutation`) — **no Priority/Specificity** (versioned policy).
Route + payments-dashboard nav tile; i18n tr+en full parity. New enum maps added to the ops-queues `enumMaps.ts`:
`REFUND_CAUSE_NAME` (int→C# name for the string-on-wire request), `PLATFORM_FEE_REFUND_MODE_KEY`/`_NAME`,
`POLICY_STATUS_KEY`/`_VARIANT` (CommissionRuleStatus). `RefundCause` labels reuse the existing `queues.enums.refundCause.*`.

### The nested allocation-rules form
The create modal has an **add/remove per-cause mini-table** (`RefundAllocationPolicyForm.tsx`): each row = `Cause`
select → `Mode` select + optional `FixedPlatformFeeAmount` (enabled only when Mode = FixedAmount). Enums are sent as
**string names** (`Cause: "ProviderCancelled"`, `Mode: "ProRata"`) per the BFF `RefundAllocationPolicyRuleBffInput`
contract; the read DTO returns them as ints (mapped to labels). Client-side validation blocks duplicate causes and a
FixedAmount row without a positive fee. Empty rules → the module seeds its MVP §7.2 default table. **The update form omits
currency + rules** (both create-time only) — currency is disabled and rules render read-only.

### Reactivate (added to module + BFF — it was quick and a clean mirror)
Missing on the module; added end-to-end mirroring `PlatformFeeRule`:
- **Entity** `RefundAllocationPolicyEntity.Reactivate()` — restores `IsActive` and re-derives `Status`.
- **Module** `ReactivateRefundAllocationPolicyCommand` + handler — guards `Status == Inactive`, then re-runs the §7.2
  fail-loud overlap guard (`FindOverlappingConflict`) **before** mutating so reactivation can't create two active
  policies for a currency (throws `RefundAllocationPolicyConflict = 5101`). Controller `POST .../{id}/reactivate`.
- **BFF** `IAdminPaymentBffRemoteCall.ReactivateRefundAllocationPolicyAsync` + `ReactivateRefundAllocationPolicyBffCommand`/handler + `AdminPaymentController` `POST admin/refund-allocation-policies/{id}/reactivate`.
- payment-api + bff-adminpanel images rebuilt (`docker compose build` → 0 errors) and restarted.

### On-screen verification (fresh OTP admin login)
| Check | Result |
|---|---|
| Policy screen lists the seeded policy | ✅ TRY #1 AKTIF, "8 kural", Jul 29 2026 – Süresiz; version history grouped by currency |
| Resolve preview | ✅ TRY → active policy #1, limit ₺50.000,00, all 8 per-cause rules labeled (e.g. "Anlaşmazlık — sağlayıcı lehine → Yok", "İdari düzeltme → Tam") |
| Create with per-cause rules + limit | ✅ USD policy #2 created with 2 allocation rows |
| **Overlapping same-currency active → typed 5101** | ✅ TRY overlap → **"Kural çakışması — An active refund-allocation policy already overlaps for currency TRY"** banner; save button disabled |
| Update limit/notes | ✅ USD #2 limit ₺50.000 → $75.000, note saved; currency disabled + rules read-only in the edit modal |
| Deactivate | ✅ USD #2 → **PASIF** (Inactive), list status dot updated |
| **Reactivate** | ✅ USD #2 (Inactive) → **Aktif**; reactivate endpoint live; deactivate re-enabled |

RefundAllocationPolicyConflict `5101` was already in the FE `RULE_CONFLICT_CODES`; the banner resolves the code from
`header.errorCode` via the existing `extractRuleConflict`.

---

## Part B — I1 sub-merchant onboarding dev seed (KYC queue now populated from a real seed)

### The seed
New `SubMerchantOnboardingMockSeed` (`Modules/Payment/…/Repository/Seed/`) mirroring `PayoutRecordMockSeed`: idempotent,
demo-only, no migration, wired into `SeedPaymentAsync` (Phase 9) + DI. Creates four `ProviderPaymentProfile` rows across
the onboarding states the queue shows, with masked LegalName/TaxNumber/SubMerchantKey (the queue handler masks tax/key)
and IsSplitEligible/HasIban derived by the entity:

| Id | Legal name | State | Split-eligible |
|---|---|---|---|
| 990001 | Demo Marine Yakıt A.Ş. | **SubMerchantCreated** (awaiting verify) | yes |
| 990002 | Demo Liman Hizmetleri Ltd. | **Verified** | yes |
| 990003 | Demo Tekne Bakım A.Ş. | **Rejected** | no |
| 990004 | Demo Balıkçılık Koop. | **SubMerchantCreated** (2nd review row) | yes |

States are reached through the entity's guarded transitions (`MarkSubMerchantCreated` + `UpdateIban`; `+ MarkVerified`;
`SubmitOnboardingData` + `Reject`) — no direct field pokes.

### The `aizen` vs `inktavia_store` finding
Investigated the "landed in `aizen`" report:
- **No current seed creates `ProviderPaymentProfile` rows** — so the KYC queue was empty simply because *no sub-merchant
  onboarding seed existed*, not because data went to the wrong DB.
- **Both `appsettings.Development.json` and `appsettings.Local.json` point Payment at `Database=inktavia_store`** (runtime
  `Host=postgres`, local-ef `Host=localhost`) — matching the corrected [[reference-payment-dbs]] note. The new seed runs
  against the Payment `DbContext` → **`inktavia_store`** (confirmed: rows present in `inktavia_store`, none created by code
  in `aizen`).
- The `#100011` "Provider 2 AS" `provider_payment_profiles` row seen in the legacy `aizen` DB is a **stale leftover** from
  before compose defaulted to `inktavia_store`. Per the spec it was **not dropped**; the fix is that runtime + seed both
  use `inktavia_store`.

### Ad-hoc row reconciliation
The ops-queues verification had inserted ad-hoc rows `#990001–990003` directly into `inktavia_store`. The seed **folds
them in with stable demo ids**: on run it removes any pre-existing rows in the demo id range (990001–990004) unless all
four canonical rows already exist, then creates the canonical set — so there is now a **single source of demo truth** (the
seed), and the mutated ad-hoc rows are gone.

### On-screen verification
| Check | Result |
|---|---|
| KYC queue lists the **seeded** rows from `inktavia_store` (no ad-hoc rows) | ✅ #990001 & #990004 SubMerchantCreated, #990003 "Reddedildi"; masked tax/key (`*******321`, `****9F21`); Verified #990002 correctly excluded from the default (attention) view |
| verify → Verified still works | ✅ #990001 verified → DB `OnboardingStatus 2→3 (Verified)`, `Active`, `VerifiedAt` set; toast "Alt üye doğrulandı" |
| reject-with-reason → Rejected | ✅ verified end-to-end in the ops-queues phase against identical unchanged FE code; #990003 ships pre-Rejected as a queue example |

---

## Files touched
**admin-web (FE):** `endpoints.ts`, `payment.types.ts`, `queryKeys.ts`, `paymentApi.ts`,
`hooks/useRefundAllocationPolicyQuery.ts`, `components/RefundAllocationPolicyForm.tsx`,
`pages/app/payments/RefundAllocationPolicyPage.tsx` + `…DetailPage.tsx`, `queues/enumMaps.ts`,
`router/routes.tsx` + `routeObjects.tsx`, `PaymentDashboardPage.tsx`, `i18n/locales/{en,tr}/payments.json`.
**Module (backend):** `RefundAllocationPolicyEntity.cs` (Reactivate), `RefundAllocationPolicyAdminSlice.cs`
(Reactivate command/handler), `RefundAdminController.cs` (reactivate route), `Seed/SubMerchantOnboardingMockSeed.cs`,
`Repository/DependencyInjection.cs` (DI + SeedPaymentAsync phase).
**BFF:** `IAdminPaymentBffRemoteCall.cs`, `RefundAllocationPolicyBffCommands.cs`, `AdminPaymentController.cs` (reactivate).

After this, the remaining wave items are **P11 offer-boost** (provider) and the **P12 financial reporting dashboard** (finale).
