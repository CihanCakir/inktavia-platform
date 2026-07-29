# REPORT_BFF — BFF-Wave 1 / I1 (provider payment-profile eligibility + admin sub-merchant KYC review)

**Source of truth:** `docs/V1.0.1/BFF_ROLLOUT_PLAN.md §1` + BE-I1's real DTOs
(`docs/V1.0.1/ServiceRequest/REPORT_BACKEND.md` → "BE-I1"). **EXTENDED** the existing provider payment-profile slice and the
admin CommissionRule-CRUD-shaped slice — nothing rewritten. Additive throughout; the `AizenApiResponse` envelope and
by-subject (provider) / admin-policy (admin) auth are preserved. CargoDry untouched.

## What was built

### Provider BFF (`Bff/src/MarineProvider`) — payment-profile eligibility (single query, no over-fetch)
The BFF `GetProviderPaymentProfileBff` handler is a **pass-through of the module `ProviderPaymentProfileDto`**, so the six
BE-I1 fields were surfaced by **additively extending the module read model + query handler** (the module is the data owner;
the FE_PROVIDER_I1 spec §58/§70 prefers a single payment-profile query over a separate `GetProviderSplitEligibility` call):
- `ProviderPaymentProfileDto` += `isSplitEligible`, `onboardingStatus`, `subMerchantType`, `subMerchantKeyMasked`,
  `ibanRequired`, `rejectionReason` — the seven existing fields (`gatewayProvider/hasIban/ibanMasked/legalName/
  taxNumberMasked/status/verifiedAt`) are **unchanged**.
- `GetProviderPaymentProfileQueryHandler` populates them from the entity: `IsSplitEligible` (the §21.4 gate signal),
  `OnboardingStatus.ToString()`, masked sub-merchant key (`****last4`), `IbanRequired = !HasIban` (BE-P9-fix §5 — no IBAN ⇒
  never split-eligible). `subMerchantType`/`rejectionReason` are nullable (BE-I1 does not persist them on the profile —
  reserved; the FE treats all six as optional/null-safe, so an old BFF simply hides the new section).
- **Upsert (type-varied KYC):** `UpsertProviderPaymentProfileRequest` + `Command` + the BFF `UpsertProviderPaymentProfileBffCommand`
  gained `subMerchantType` (PERSONAL / PRIVATE_COMPANY / LIMITED_OR_JOINT_STOCK_COMPANY) + the conditional
  `identityNumber` / `taxOffice` / `legalCompanyTitle`. **The legacy body (`iban/legalName/taxNumber`) stays 100% valid** —
  every new field is optional. The upsert already advances onboarding to `DataSubmitted`
  (`UpdateProfileAndResetVerification`); the KYC-type fields are carried for the sub-merchant registration flow.

### Admin BFF (`Bff/src/AdminPanel`) — sub-merchant KYC review slice (mirrors the CommissionRule CRUD pattern)
- **Module (additive):** the review endpoints existed (verify/reject on `PaymentAdminController`) but no way to *list* the
  queue, so added `GetProviderSubMerchantOnboardingQueueQuery` + handler + `IProviderPaymentProfileRepository.GetOnboardingQueueAsync`
  (paged, optional status filter; default = items needing attention, i.e. not NotStarted/Verified) + a paged
  `ProviderSubMerchantOnboardingQueueDto` (masked IBAN/tax/sub-merchant key) + `GET
  /api/v1/payment/admin/providers/sub-merchant/onboarding-queue`.
- **BFF remote-call:** `IAdminPaymentBffRemoteCall` += `GetSubMerchantOnboardingQueueAsync` / `VerifySubMerchantAsync` /
  `RejectSubMerchantAsync` → the module `/api/v1/payment/admin/providers/...` endpoints (typed bodies/results, never `object`).
- **BFF slice:** `GetSubMerchantOnboardingQueueBffQuery` + `VerifySubMerchantBffCommand` + `RejectSubMerchantBffCommand`
  (+ handlers, + typed `RejectSubMerchantBffRequest` / response wrappers) → three `AdminPaymentController` endpoints under the
  class-level `[Authorize(Policy = "AdminPanelAccess")]` (the admin gate; the module's own `PaymentAdminController` is
  `[Authorize(Roles = "Admin")]`). Verify/reject return the module's `ProviderSubMerchantOnboardingResult`
  (`ProviderProfileId, OnboardingStatus, IsSplitEligible`) so the UI refreshes the row without a second fetch.

## Verification

**1. Build — 0 errors** for both BFF hosts + their Application projects + the extended Payment module:
`Aizen.Bff.MarineProvider`, `Aizen.Bff.AdminPanel`, `Aizen.Modules.Payment` all `0 Error(s)`.

**2. Extended provider payment-profile — proven against the LIVE BFF OpenAPI** (`bff-marineprovider` :17002, rebuilt +
recreated, clean boot):
```
ProviderPaymentProfileDto fields (live /swagger/v1/swagger.json):
  gatewayProvider, hasIban, ibanMasked, legalName, taxNumberMasked, status, verifiedAt        ← 7 existing, UNCHANGED
  isSplitEligible, onboardingStatus, subMerchantType, subMerchantKeyMasked, ibanRequired, rejectionReason  ← 6 new (BE-I1)
UpsertProviderPaymentProfileRequest fields:
  iban, legalName, taxNumber                                                                  ← legacy, still valid
  subMerchantType, identityNumber, taxOffice, legalCompanyTitle                               ← new type-varied KYC
```
Envelope intact (`AizenApiResponse<ProviderPaymentProfileDto?>` unchanged). `GET/PUT payment-profile` return **401 without a
token** (by-subject provider auth) — route matched, auth enforced.

**3. Admin KYC queue + verify/reject — build-verified end-to-end** (remote-call → query/command handlers → controller →
module endpoint; mirrors the working CommissionRule slice). **Runtime round-trip is blocked in this environment by a
pre-existing admin-BFF JWT config fault** (`IDX10703: SymmetricSecurityKey key length is zero` — `KEYCLOAK_BFF_CLIENT_SECRET`
is an empty placeholder), which returns **500 on _every_ admin-BFF request — the new endpoints, the existing
`commission/rules` endpoint, and even `/swagger` alike** — so it is not introduced by this slice and does not indicate a
wiring problem. A live 200 needs a valid admin JWT (env config, same constraint as all existing admin slices). Typed reject
body (`{"reason": "..."}`) is modelled by `RejectSubMerchantBffRequest`.

**4. Additive / no regression.** Only appended DTO fields + new endpoints; no existing slice signature changed. Payment module
tests green — **Domain 259, Repository 56**. No BFF test projects exist (nothing to break). CargoDry untouched.

## Notes / deferred
- **`rejectionReason` / `subMerchantType` persistence:** BE-I1 stores neither on the profile; surfaced as null now (FE
  null-safe). Persisting the admin reject reason (a small additive entity column + migration) is the one follow-up that would
  light up the provider-facing rejection banner with real text.
- **Admin BFF JWT key** (`KEYCLOAK_BFF_CLIENT_SECRET`) must be set for any admin-BFF runtime call in this environment — an
  infra/config task, independent of this slice.
- **Next (per the rollout plan):** BFF-Wave 2 (admin rule CRUD P3/P4/P5/P6/P7 — replicate the CommissionRule template), then
  Wave 3–5; FE last (FE_PROVIDER_I1 can consume this slice now that the contract is live).

---

# REPORT_BFF — BFF-Wave 2 / Admin rule CRUD (P3–P7)

**Source of truth:** `docs/V1.0.1/BFF_ROLLOUT_PLAN.md §1 (admin table) + §3 template`, each slice aligned to the phase's
**real** module admin CRUD endpoints/DTOs (`Modules/Payment/.../Controllers/*`). **MIRRORED the CommissionRule CRUD slice**
(IAdminPaymentBffRemoteCall `[AizenRemoteCall*]` → `AdminPayment/<X>` handlers → `AdminPaymentController` endpoints,
`AizenApiResponse` envelope, **typed DTOs never `object`**, enum fields as `string` per the BFF Newtonsoft contract). Additive
only — no existing slice rewritten. **BFF-only: no module and no CargoDry code was touched.**

## Slices added (each bound to the module's actual endpoints)

| Slice | Module route (payment-api) | BFF operations |
|---|---|---|
| **P3 PlatformFeeRule** | `/api/v1/payment/platform-fee` | resolve · create · update · deactivate |
| **P4 ProviderPlanPrice** | `/api/v1/payment/plan-prices` | list-for-plan · **resolve (dev query)** · upcoming-changes · create · update · deactivate |
| **P5 ProfitProtectionPolicy** | `/api/v1/payment/profit-protection` | resolve · create · update · deactivate (single-active) |
| **P6 CustomerDiscountRule** | `/api/v1/payment/customer-discounts` | resolve · create · update · deactivate |
| **P6 CustomerBenefitBudgetPolicy** | `/api/v1/payment/benefit-budget` | create-policy (per plan) |
| **P7 ProviderCommissionBenefitRule + entitlement** | `/api/v1/payment/commission-benefits` | resolve · rule create/update/deactivate · **grant** · **revoke** |

> The module exposes exactly these endpoints for these entities (no paged-list/by-Id/reactivate exist module-side for the
> rule/policy tables — resolution is date-driven), so each BFF slice mirrors the real surface rather than inventing endpoints.

Per slice: one `Dto/<X>BffDtos.cs` (request + result records, all enums `string`), one `<X>/<X>BffCommands.cs` (command +
response + handler per mutation) and, where the module has a read, one `<X>/<X>BffQueries.cs`. Each handler forwards to a new
`IAdminPaymentBffRemoteCall` `[AizenRemoteCall*]` method whose path/verb/body match the module endpoint; each
`AdminPaymentController` endpoint sits under the class-level `[Authorize(Policy = "AdminPanelAccess")]` and returns
`AizenApiResponse<...>`.

### Fidelity details baked in from the module maps
- **Model-varied P3** (`Percentage/Fixed/PercentageWithBounds/Waived`) sent as a `Model` string; conflict
  `PlatformFeeRuleConflict (5041)` surfaces through the envelope.
- **P4 launch/list + effective dates**; the `plan-prices/resolve` point-in-time query is exposed as the resolver dev query;
  `ProviderPlanPriceConflict (5044)` / `…Gap (5045)` are fail-loud through the envelope.
- **P5** carries all min-contribution amount/rate + expected-expense rate fields + `AdjustmentOrder` (string);
  `ProfitProtectionPolicyConflict (5050)`.
- **P6** keeps discount `FundingMode` (Platform/Provider/Shared/Supplier) + `DiscountType` distinct; the budget policy is
  scoped per plan via `CreateCustomerBenefitBudgetPolicy`; `CustomerDiscountRuleConflict (5060)` /
  `CustomerBenefitBudgetPolicyConflict (5068)`.
- **P7** `Stackable`/`Exclusive` bools + `MaximumEligibleGMV`/`UsageLimit`; entitlement `grant`/`revoke` return the module's
  typed `Grant/RevokeEntitlementResult`.
- **PUT id-match guard:** the P3–P7 module `Update` endpoints validate `route id == body.Id` (unlike CommissionRule which
  overrides it). Each BFF Update request record carries `Id`, and the handler forces `body with { Id = route id }` so the
  route and body can never disagree.

## Verification

**1. Build — 0 errors** for the AdminPanel BFF host **and** its Application project (the Application build first confirmed
every handler ↔ remote-call method signature aligns). MarineProvider BFF also rebuilt clean (unchanged). 10 pre-existing
nullability warnings, no new errors.

**2. Per-slice path/typed-body alignment** — every `[AizenRemoteCall*]` method's verb + path + `[AizenRemoteCallBody]` type
matches the mapped module endpoint (table above); responses are typed BFF result records (never `object`); enum fields are
`string` (BFF Newtonsoft has no `StringEnumConverter`; the payment-api deserializer accepts string enum names on the request
path, as the CommissionRule slice already relies on).

**3. Envelope + auth** — all endpoints return `AizenApiResponse<T?>` and inherit the controller's
`[Authorize(Policy = "AdminPanelAccess")]`.

**4. Clean runtime boot** — `docker compose build/up bff-adminpanel` boots cleanly (`Now listening` / `Application started`),
proving all new command/query handlers are discovered and DI resolves. The new endpoints respond **identically to the existing
`commission/rules` endpoint** (HTTP 500 without a token) because of the **pre-existing admin-BFF JWT fault**
(`KEYCLOAK_BFF_CLIENT_SECRET` empty → `SymmetricSecurityKey key length is zero`), which short-circuits *every* admin-BFF
request before routing — so it is not introduced here. **Runtime CRUD smoke (conflict fail-loud round-trip) is deferred until
the secret is set**; the wiring is build- and boot-verified now.

**5. No regression** — only appended to `IAdminPaymentBffRemoteCall.cs` + `AdminPaymentController.cs`; every other file is new
(the 6 slice folders + DTOs). No existing BFF slice (CommissionRule, transactions, payouts, invoices, subscriptions…) file was
modified. No module / CargoDry change.

## Next
BFF-Wave 3 (provider read enrichment P4/P8/P10), Wave 4 (P10 refund/chargeback/negative-balance + P11 premium), Wave 5 (offer
economics preview + P12 financial reporting → AdminFinance). FE waves last. Set `KEYCLOAK_BFF_CLIENT_SECRET` for admin-BFF
runtime smoke across all waves.

---

# REPORT_BFF — BFF-Wave 3 / Provider read enrichment (P4, P8/S8, P10)

**Source of truth:** `docs/V1.0.1/BFF_ROLLOUT_PLAN.md §1 (provider table)`, each field aligned to the phase's real module
read-model (`docs/V1.0.1/*/REPORT_BACKEND.md`: BE-P4, BE-P8/P8b/S8, BE-P10). **The provider BFF queries are pure pass-throughs
of the module provider DTOs** (`GetProviderTransactionsBff/Subscription/Plans/Payouts` just return the module DTO `.Body`), so
— exactly as in Wave 1 — the enrichment was done by **additively extending the MODULE provider read-models** (the shared
`Aizen.Modules.Payment.Abstraction.Dto` types the BFF returns). **No BFF code changed**; the `AizenApiResponse` envelope +
by-subject auth are untouched; every new field is optional (envelope-tolerant, null on legacy rows). CargoDry untouched.

## What was extended (module read-models; the BFF carries them through)

### P4 — launch-vs-list price + upcoming change (`GetProviderSubscription`, `GetProviderPlans`)
- `ProviderSubscriptionDto` += `ActivePrice` (a `ProviderPlanActivePriceDto`: `PriceType` "Launch"/"List" + `PriceAmount` +
  effective window + `PriceCode`), `HasUpcomingPriceChange`, `UpcomingPriceAmount`, `UpcomingPriceChangeAt`.
- `ProviderPlanDto` += `ActivePrice`.
- Handlers inject `IProviderPlanPriceRepository`: `ResolveAsync(planId, currency, Monthly, now)` → the price active **now**
  (launch vs list is date-driven); `ResolveRenewalPriceAsync(sub, PeriodEnd)` → upcoming flag when it differs from `PaidAmount`.

### P8/S8 — immutable snapshot breakdown (`GetProviderTransactions`)
- `ProviderTransactionDto` += `EconomicsSnapshotId` + `EconomicsBreakdown` (a `ProviderTransactionEconomicsBreakdownDto`:
  ServiceAmount/ServiceVat, CommissionBase/Rate/Amount, ProviderNet, PlatformFee net/vat/gross, CustomerTotal,
  PlatformGrossShare, TotalCustomer/ProviderFunded/PlatformFunded discount) — read **straight from the linked
  `PaymentEconomicsSnapshot`** via `IPaymentEconomicsSnapshotRepository.GetByIdAsync`, **never recomputed**. Null for legacy
  transactions with no snapshot. (Commission-benefit is not persisted on the snapshot — BE-S8 defers it — so it is not
  surfaced; the discount **funding** lines are.)

### P10 — refund/dispute + negative-balance/clawback transparency (`GetProviderTransactions`, `GetProviderPayouts`)
- `ProviderTransactionDto` += `DisputedAt` (+ existing `TotalRefundedAmount`) + `RefundSummary` (a
  `ProviderTransactionRefundSummaryDto`: `Cause`/`ReleaseState` names, ServiceRefund, ProviderNetReversal,
  CommissionRevenueReversal, `PlatformAdvancedRefundAmount`, `ProviderRecoveryAmount`, `RemainingProviderNegativeBalance`),
  read from the latest `RefundAllocation` (via `IRefundAllocationRepository.GetByRefundRecordIdAsync`) only for refunded rows.
- `ProviderPayoutPagedResultDto` += `NegativeBalance` (a `ProviderBalanceSummaryDto`: Balance, NegativeAmount,
  NegativeBalanceLimit, IsOverLimit) from `IProviderBalanceRepository.GetByProviderAsync` — null when the provider has no
  ledger row (never clawed back).

## Verification

**1. Build — 0 errors** for the Payment module (Abstraction + Application + host) **and** the MarineProvider BFF host +
Application (recompiled against the extended shared DTOs; no BFF source change).

**2. Rebuilt + recreated** `payment-api` (new handler logic) and `bff-marineprovider` (extended DTOs) — both boot clean
(`Now listening` / `Application started`).

**3. LIVE BFF OpenAPI (`:17002/swagger/v1/swagger.json`) shows the extended provider DTOs — existing fields UNCHANGED, new
fields additive/optional:**
```
ProviderTransactionDto:      …15 existing… + disputedAt, economicsSnapshotId, economicsBreakdown, refundSummary
ProviderTransactionEconomicsBreakdownDto: serviceAmount…platformGrossShare, total{Customer,ProviderFunded,PlatformFunded}Discount
ProviderTransactionRefundSummaryDto: cause, releaseState, …, platformAdvancedRefundAmount, providerRecoveryAmount, remainingProviderNegativeBalance
ProviderPayoutPagedResultDto: items,total,page,pageSize + negativeBalance   (ProviderBalanceSummaryDto: balance,negativeAmount,negativeBalanceLimit,isOverLimit,currencyCode)
ProviderSubscriptionDto:     …15 existing… + activePrice, hasUpcomingPriceChange, upcomingPriceAmount, upcomingPriceChangeAt
ProviderPlanDto:             …13 existing… + activePrice   (ProviderPlanActivePriceDto: priceType,billingPeriod,priceAmount,…,priceCode)
```

**4. Auth** — `GET transactions|payouts|subscription|plans` all return **401 without a token** (by-subject provider auth
enforced; route matched).

**5. Smoke against seeded data (runtime `aizen`):**
- **P4 populated** — `provider_plan_prices` is seeded (plan 2 = Launch 499 + List 1490 TRY, etc.), so `GetProviderPlans`
  resolves `ActivePrice` (launch/list, date-driven) for those plans.
- **P8/S8 + P10 null-safe legacy** — all 19 mock transactions have `EconomicsSnapshotId = NULL`, and there are 0
  `provider_balances` / 0 `refund_allocations`, so `EconomicsBreakdown` / `RefundSummary` / `NegativeBalance` come back **null**
  (the handlers' guarded paths) — proving the additive fields are null-safe on legacy rows.
- The **populated** P8/P10 path needs a real P8 acceptance snapshot / P10 refund, which the mock env never produced (it requires
  the authenticated SR-acceptance + refund flow, the same JWT/seed constraint noted in prior waves). The handlers read directly
  from the snapshot/allocation/balance repositories (build- and boot-verified), so the fields populate whenever those immutable
  records exist.

**6. No regression** — only additive DTO fields + additive handler reads; no existing provider field or endpoint changed; no
BFF source touched. Payment **Domain 259 / Repository 56** tests green. No module/CargoDry breakage.

## Next
BFF-Wave 4 (P10 admin refund/chargeback queue + negative-balance ledger + RefundAllocationPolicy CRUD; P11 premium
product/price CRUD + provider boost command/status), Wave 5 (offer economics preview S1/S6/S7 + P12 financial reporting).
FE waves last. Provider JWT/seed needed for an authenticated data smoke of the populated snapshot/refund paths.

---

# REPORT_BFF — BFF-Wave 4 / Refund + Premium (P10 admin refund/chargeback/negative-balance + P11 premium/boost)

**Source of truth:** `docs/V1.0.1/BFF_ROLLOUT_PLAN.md §1`, aligned to BE-P10/BE-P11 (`docs/V1.0.1/ServiceRequest/REPORT_BACKEND.md`).
**MIRRORED** the established patterns — admin CRUD/query = the Wave-2 CommissionRule/PlatformFeeRule slice; provider command =
the payment slice + by-subject auth. Because **no module HTTP surface existed** for these P10/P11 features (only application
handlers), this Wave built the module admin+provider endpoints **additively** and then the BFF slices. Typed DTOs (never
`object`); `AizenApiResponse` envelope + `[Authorize]`/by-subject preserved. CargoDry untouched.

## Module surface added (additive; new controllers, no existing endpoint changed)
- **P11 provider (`PaymentProviderController`, by-subject):** `POST /provider/offer-boost` (→ existing `PurchaseOfferBoostCommand`)
  and `GET /provider/offers/{offerId}/boost-status` (→ existing `GetActiveBoostForOfferQuery`, mapped to a new
  `OfferBoostStatusDto`). New request `PurchaseOfferBoostRequest`.
- **P11 admin (`PremiumAdminController`, `/api/v1/payment/admin/premium`):** PremiumProduct CRUD (list/byId/create/update/
  activate/deactivate — added a `PremiumProductEntity.Update` mutator) + PremiumProductPrice CRUD (list/resolve/create/update/
  deactivate, point-in-time overlap → `PremiumProductPriceConflict`). New commands/queries + `PremiumProductAdminDto` /
  `PremiumProductPriceAdminDto` / `PremiumMutateResultDto`.
- **P10 admin (`RefundAdminController`, `/api/v1/payment/admin`):**
  - **RefundAllocationPolicy CRUD** (list/byId/resolve/create/update/deactivate). Create seeds the MVP §7.2 default rules when
    none supplied; overlap → `RefundAllocationPolicyConflict`; added a `RefundAllocationPolicyEntity.UpdateHeader` mutator.
  - **refund-queue** (paged over `TransactionRefundRecord`, filter by `cause`/`releaseState`/`status`) + **chargeback-queue**
    (paged over `ChargebackRecord`) — read-only via `PaymentDbContext` (precedent: `FinancialLedgerBackfillService`).
  - **provider-balances** ledger (paged list + by-provider with movements) + **audited manual adjust** (→
    `ProviderBalanceEntity.ManualAdjust`, records `AdminUserId`). New `ProviderBalanceAdminDto`/`…MovementDto`/`…AdjustResultDto`.

## BFF slices added (mirror Wave-2 exactly)
- **Provider (MarineProvider):** `IPaymentRemoteCall` += `PurchaseOfferBoost`/`GetOfferBoostStatus`; `PurchaseOfferBoostBffCommand`
  + `GetOfferBoostStatusBffQuery` (resolve provider identity → remote-call); two `PaymentController` endpoints.
- **Admin (AdminPanel):** `IAdminPaymentBffRemoteCall` += 24 methods; BFF request DTOs (string enums per the BFF Newtonsoft
  contract) + command/query handlers under `AdminPayment/{PremiumAdmin,RefundAllocationPolicy,RefundQueue,ProviderBalance}`;
  24 `AdminPaymentController` endpoints under the class-level `[Authorize(Policy="AdminPanelAccess")]`. All READ responses reuse
  the module `Abstraction.Dto` types directly (no BFF copies).

## Verification
**1. Build — 0 errors** for the Payment module host, the AdminPanel BFF host, and the MarineProvider BFF host (+ their
Application projects). Module **Domain 259 / Repository 56** tests green — no regression from the two additive entity mutators.

**2. Module endpoints LIVE** — payment-api OpenAPI (`:7102`) exposes all **20 new P10/P11 endpoints** (premium products/prices,
refund-allocation-policies + resolve, refund-queue, chargeback-queue, provider-balances + adjust, provider offer-boost +
boost-status).

**3. P11 provider boost — RUNTIME VERIFIED** (rebuilt+recreated `payment-api` + `bff-marineprovider`, clean boot). LIVE BFF
OpenAPI (`:17002`) shows both endpoints + `PurchaseOfferBoostRequest`/`PurchaseOfferBoostResult`/`OfferBoostStatusDto`; both
`POST /offer-boost` and `GET /offers/{id}/boost-status` return **401 without a token** (by-subject auth enforced, route matched).
Existing 10 provider endpoints intact (no regression).

**4. Admin slices — build + boot verified** (rebuilt+recreated `bff-adminpanel`, clean boot → all new command/query handlers
discovered + DI resolves). The new endpoints respond **identically to the existing admin endpoints** (HTTP 500 without a token)
because of the **pre-existing admin-BFF JWT fault** (empty `KEYCLOAK_BFF_CLIENT_SECRET` → `SymmetricSecurityKey key length is
zero`) that short-circuits every admin request before routing — not introduced here. **Runtime admin CRUD smoke is deferred
until the secret is set** (per the task); the per-slice remote-call path/body/response-type all match the module endpoints
(build-verified end-to-end).

**5. No regression** — only additive files + append-only edits to the two shared BFF files (`IAdminPaymentBffRemoteCall.cs`,
`AdminPaymentController.cs`) and the two shared module controllers/entities; no existing slice signature changed; no CargoDry.

## Notes / deferred
- **RefundAllocationPolicy per-rule editing** — Create sets the per-cause rules (defaults to §7.2); Update tunes the header
  (limit/dates/notes). Editing individual rules on an existing policy is a follow-up.
- Set `KEYCLOAK_BFF_CLIENT_SECRET` for the authenticated admin-BFF CRUD smoke across all admin waves.

## Next
BFF-Wave 5 (offer economics preview S1/S6/S7 + P12 financial reporting → AdminFinance). FE waves last.
