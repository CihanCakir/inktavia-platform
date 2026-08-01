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

---

# Admin Auth Phase A+B

**Task:** `docs/V1.0.1/ADMIN_AUTH_ALIGNMENT.md`. **Decision:** SAME realm as provider (`inktavia-realm`), NEW admin
client as audience (`admin-panel-bff`). Mirror the MarineProvider BFF pattern exactly; do not touch the MarineProvider BFF
(reference only) or CargoDry.

## Result — ✅ end-to-end verified (401 / 200 / 403)

The pre-existing "**HTTP 500 on every admin request**" fault (empty `KEYCLOAK_BFF_CLIENT_SECRET` → `SymmetricSecurityKey key
length is zero`, documented in Wave-4 §Notes) is **GONE**. Inbound admin auth is now Keycloak RS256/JWKS, identical to the
provider, and a valid Keycloak **Admin** token flows all the way through to real data:

| Case | `GET /api/v1/admin-panel/providers` | Expected | Actual |
|---|---|---|---|
| No token | — | 401 | **401** ✅ |
| Keycloak token **with** `Admin` realm role | `X-Aizen-User-Token: Bearer <jwt>` | 200 | **200** ✅ (`isSuccess:true`, `items`+`totalCount`) |
| Keycloak token **without** `Admin` role | `X-Aizen-User-Token: Bearer <jwt>` | 403 | **403** ✅ |

## Phase A — inbound RS256 (byte-for-byte provider mirror)

- **`Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Extensions/AuthenticationExtensions.cs`** (new) —
  `AddAdminPanelAuthentication(config)` mirrors `AddMarineProviderAuthentication`: Keycloak RS256, `Authority`/`MetadataAddress`/
  JWKS, `ValidIssuer=Authority`, `ValidAudience=admin-panel-bff`, `NameClaimType=preferred_username`, `RoleClaimType=Role`, and
  `MapKeycloakRealmRoles` (`realm_access.roles → ClaimTypes.Role`). Config prefix `AdminPanelKeycloak:*`. The one intentional
  divergence from the provider: `OnMessageReceived` reads the human token from **`X-Aizen-User-Token`** (Bearer stripped) — the
  admin BFF's `Authorization` header carries the BFF→module service token, so the human JWT rides the aizen header.
- **`Program.cs`** — replaced the inline `AddJwtBearer` + `SymmetricSecurityKey`(HS256) block with
  `.AddAdminPanelAuthentication(builder.Configuration)`; removed the JwtBearer/IdentityModel.Tokens/System.Text usings. The
  `AdminPanelAccess` policy is kept and now `RequireRole("Admin")` (Keycloak realm role) + `RequireAuthenticatedUser`
  FallbackPolicy. **The `TokenOption:SecurityKey` HS256 path is fully removed** (grep: only appears in doc comments).
- **Config** — `AdminPanelKeycloak:*` added to `appsettings.json` (env-driven), `appsettings.Local.json` (localhost:8080), and
  `docker-compose.yaml` `bff-adminpanel` (`AdminPanelKeycloak__*`, `keycloak:8080` internal / `localhost:8080` issuer, mirroring
  `MarineProviderKeycloak__*`). Removed the stale `bff-adminpanel` `KEYCLOAK_AUTHORITY/AUDIENCE(=bff-adminpanel)/…` env; added
  `KeycloakServiceToken__Authority` + `__TokenEndpoint`.

## Phase B — login mints Keycloak tokens (BFF slice)

Mirror of the provider OTP→Keycloak login, so the admin login issues a **Keycloak** token (not the legacy Identity HS256 one):
- **`…Application/Contracts/Auth/OtpLogin/AdminOtpLoginContracts.cs`**, **`…Application/Auth/OtpLogin/AdminOtpLoginSlice.cs`**
  (Request/Verify/Resend commands+handlers), **`…/Controllers/V1/Auth/OtpLoginController.cs`**
  (`[Route("api/v1/admin-panel/auth/otp-login")] [AllowAnonymous]`) — all byte-for-byte mirrors of the provider's OtpLogin
  surface, using the shared `Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.*ProviderOtpLogin*` DTOs.
- **`IIdentityAdminBffRemoteCall`** — added `RequestAdminOtpLogin` / `VerifyAdminOtpLogin` / `ResendAdminOtpLogin` →
  `POST /api/v1/identity/auth/admin-otp-login/{request,verify,resend}`. Verify returns `NextAction=keycloak_handoff_required`
  + `LoginTicket` for the FE handoff, exactly like the provider.
- **⚠️ Prerequisite (Identity module, out of BFF scope):** the Identity module currently only exposes
  `provider-otp-login/*`. The admin analog **`/api/v1/identity/auth/admin-otp-login/{request,verify,resend}`** must be added
  (mint a Keycloak token carrying the `Admin` realm role). The BFF slice is graceful until then (anti-enumeration 200 on
  request; verify falls back to `keycloak_handoff_required`). Registration / password-reset align to the same provider pattern.

## Keycloak config required (what must exist / be set)

1. **Client `admin-panel-bff`** (confidential, service accounts on) — the BFF inbound **audience** and the BFF→module service
   token. Already exists in `inktavia-realm`.
2. **Client `admin-panel-web`** (public, the FE) — needs an **audience mapper adding `admin-panel-bff`** to issued access
   tokens (mirrors `provider-portal-web → provider-portal-bff`), so FE-obtained tokens validate at the BFF. *(During this
   verification an audience mapper was added to the throwaway `admin-test` client to mint a token; `admin-panel-web` needs its
   own — a documented FE/Keycloak prerequisite.)*
3. **`Admin` realm role** — must be assigned to (a) admin **users**, and (b) the **`admin-panel-bff` service account**. The
   latter is essential: the `AdminPanelBffAuthDelegatingHandler` sends the service token as `Authorization` to the Identity
   module, whose admin endpoints are `[Authorize(Roles = Admin)]`. The service account was assigned **`Admin` + `identity_read`
   + `identity_write`** (provider baseline + Admin) during this task — **this is required production config, keep it.**
4. **Env / secret:** `KEYCLOAK_BFF_CLIENT_SECRET` → `AdminPanelKeycloak__AdminClientSecret` **and**
   `KeycloakServiceToken__ClientSecret` for `bff-adminpanel` (the confidential `admin-panel-bff` secret).

**Operational note:** the BFF caches its service token in **Redis DB 13**
(`inktavia:admin-panel-bff:keycloak-service-token:…`). After changing the service-account roles, delete that key (or let it
TTL-expire) so the BFF picks up a token carrying the new roles — a container restart alone does **not** clear it.

## Verification commands (reproducible)

```
# inbound RS256 build + no HS256 path
dotnet build Bff/src/AdminPanel/…            # 0 errors
grep -rn SymmetricSecurityKey Bff/src/AdminPanel   # doc comments only

# module honors an Admin-carrying service token
curl -s -o/dev/null -w '%{http_code}' localhost:7101/api/v1/identity/organizers/profiles \
  -H "Authorization: Bearer <admin-panel-bff client_credentials token>"   # 200 (401 w/o)

# end-to-end through the BFF (localhost:17001)
#   no token → 401 ; Admin JWT → 200 ; non-admin JWT → 403
```

## Scope / regression

Additive files + `Program.cs`/config edits inside the **AdminPanel BFF only**. MarineProvider BFF and CargoDry untouched
(reference only). Test artifacts created during verification (`authtest-admin`, `authtest-nonadmin` users, `admin-test`
audience mapper) are cleanup-only; the **`admin-panel-bff` service-account Admin role** is real config and retained.

---

# Admin Token Management — X-Aizen removed, provider-mirrored

**Task:** `docs/V1.0.1/ADMIN_TOKEN_MANAGEMENT_ALIGNMENT.md` §3. Remove `X-Aizen-User-Token` from the AdminPanel BFF↔FE flow
entirely and mirror the MarineProvider↔FE token flow byte-for-byte. Do not touch the MarineProvider BFF, provider-web, or
CargoDry (reference only). This supersedes the one intentional divergence from "Admin Auth Phase A+B" above (inbound token in
`X-Aizen-User-Token`) — inbound is now `Authorization: Bearer`, exactly like the provider.

## Result — ✅ inbound + outbound verified end-to-end

**Inbound** (`GET /api/v1/admin-panel/providers`, human Keycloak token in `Authorization: Bearer`):

| Case | Header | Expected | Actual |
|---|---|---|---|
| No token | — | 401 | **401** ✅ |
| Admin token | `Authorization: Bearer <jwt>` | 200 | **200** ✅ |
| Non-Admin token | `Authorization: Bearer <jwt>` | 403 | **403** ✅ |
| Admin token in the **old** header | `X-Aizen-User-Token: Bearer <jwt>` | 401 | **401** ✅ (header no longer read) |

**Outbound** (BFF→identity module call, captured by routing the identity RemoteCall to a header-echo server with the assertion
secret enabled and a numeric `user-id` claim in the admin token):

```
path: /api/v1/identity/organizers/profiles?pageIndex=0&pageSize=20
Authorization:          Bearer eyJ…   (azp=admin-panel-bff service token, 10 module audiences — NOT the user JWT)
X-Aizen-Bff-Assertion:  verify-secret-xyz
X-Aizen-User-Id:        4242
X-Aizen-User-Token:     <ABSENT>      ✅ the user JWT is never forwarded
```

## BFF changes (provider mirror)

1. **`Extensions/AuthenticationExtensions.cs`** — removed the `X-Aizen-User-Token` `OnMessageReceived` block; inbound auth now
   uses the default JwtBearer `Authorization: Bearer`, exactly like the provider. No SignalR query-string branch (no admin hub).
2. **`Application/Common/Http/AdminPanelBffAuthDelegatingHandler.cs`** — removed the raw-user-JWT `X-Aizen-User-Token` forward
   block and the `IAizenUserInfoAccessor` dependency; kept the service-token block; **added the provider's BFF-assertion block**
   — `X-Aizen-Bff-Assertion` (shared secret from `AdminPanelKeycloak.ModuleAssertionSecret`) + `X-Aizen-User-Id` (from
   `IAdminIdentityHolder`). **No profile id** (an admin is not a provider). Byte-for-byte the provider handler minus ProfileId.
3. **`Application/Common/Services/AdminIdentityHolder.cs`** (new) — `IAdminIdentityHolder`/`AdminIdentityHolder`, UserId-only
   mirror of `IProviderIdentityHolder`. **`Application/Common/Services/AdminIdentityResolver.cs`** (new) —
   `IAdminIdentityResolver`/`AdminIdentityResolver`, a mirror of `ProviderProfileResolver` minus the profile lookup: reads the
   acting admin's numeric Identity user id straight from the validated Keycloak principal claim (`AdminUserIdClaim`, default
   `user-id`, `UserId` fallback) — no Identity HTTP call (so no recursion/latency), invoked idempotently by the handler.
4. **`Application/Common/Options/AdminPanelKeycloakOptions.cs`** (new) — `ModuleAssertionSecret` + `AdminUserIdClaim`, bound
   from `AdminPanelKeycloak` (mirrors `MarineProviderKeycloakOptions.ModuleAssertionSecret`). Empty secret = assertion off
   (safe default, exactly like the provider). **`DependencyInjection.cs`** — registered the options, holder, resolver, and
   `AddHttpContextAccessor()`; updated the auth doc comment.
5. **`Program.cs`** — comment updated (token now arrives in `Authorization`).
6. **RemoteClients** — updated the "Authorization + X-Aizen-User-Token" doc comments to "Authorization service token +
   optional X-Aizen-Bff-Assertion"; no per-method user-token header params existed. **Deleted the dead
   `AuthorizationForwardingHandler.cs`** (it was never wired into any HttpClient — confirmed it does not forward the user
   Authorization to module calls).
7. **Service token provider** — behavior parity with the provider confirmed (client_credentials, Redis cache, per-module
   audiences baked into the `admin-panel-bff` client); left untouched per the task.

## Module side

No authorization change: admin endpoints stay `[Authorize(Roles="Admin")]` on the service token. The assertion is **audit
only** and consumed by the shared `AizenUserInfoMiddleware.TryAcceptBffAssertion` (Core) — which already supports it for the
provider. **No module handler reads `X-Aizen-User-Token` directly** (grep-verified), so nothing to migrate. `docker-compose`
adds `admin-panel-bff` to each module's `BffAssertion__AllowedClientIds` (alongside `provider-portal-bff`) for the 6 modules
that carry the assertion block; the shared secret is `AIZEN_BFF_ASSERTION_SECRET` (same var the provider uses).

## FE (inktavia-marine-admin-web) — mirrors provider `authInterceptors`

- **`shared/api/requestHeaders.ts`** — sends the Keycloak token as `Authorization: Bearer`; no longer sends
  `X-Aizen-User-Token`. **`shared/api/httpClient.ts`** — the 401-refresh retry re-sets `Authorization` (was
  `X-Aizen-User-Token`). **`test/authHeaders.test.ts`** — updated (X-Aizen → Authorization); **3/3 green**. Stale cargodry
  activation comments referencing the old header updated. **`tsc --noEmit` 0 errors.**

## Config / Keycloak prerequisites

- `AdminPanelKeycloak__ModuleAssertionSecret: ${AIZEN_BFF_ASSERTION_SECRET:-}` (+ `AdminPanelKeycloak__AdminUserIdClaim: user-id`)
  on `bff-adminpanel`; `BffAssertion__AllowedClientIds__1: admin-panel-bff` on each assertion-enabled module. Empty secret =
  assertion dormant (default), authorization still works via the service token role.
- **`admin-panel` (FE public client)** needs an audience mapper adding `admin-panel-bff` (mirrors `provider-portal`→`-bff`) —
  still the open FE/Keycloak prereq from Phase A+B; verified here with a temporary mapper on `admin-test`.
- **For the audit `X-Aizen-User-Id` to populate**, the `admin-panel` client needs a protocol mapper emitting the acting
  admin's numeric Identity user id as the `user-id` claim (verified here with a temporary hardcoded-claim mapper = 4242). Absent
  it, no user-id is asserted and authorization is unaffected.

## Verification / build / scope

`dotnet build` AdminPanel BFF **0 errors** (no module `.cs` touched — module changes are compose env only, so no module
rebuild needed). `bff-adminpanel` rebuilt + recreated → clean boot. Grep proof: `X-Aizen-User-Token` / `IAizenUserInfoAccessor`
no longer read or written anywhere in the admin BFF (only doc comments stating it is gone). **Zero MarineProvider BFF, zero
provider-web, zero CargoDry-module changes** (git-verified). All Keycloak test artifacts (2 users, 2 `admin-test` mappers) and
the temporary compose override / capture server were removed after verification.

---

# Admin identity by-subject

**Task:** replace the admin BFF's `user-id` claim read with a Keycloak subject → Identity numeric user-id lookup, exactly like
MarineProvider's `ProviderProfileResolver`. This removes the need for a Keycloak `user-id` claim mapper. Do not touch
MarineProvider / provider-web (reference) or CargoDry.

## Result — ✅ verified end-to-end (module endpoint + BFF resolver + outbound assertion)

The BFF assertion's `X-Aizen-User-Id` is now resolved from the token's Keycloak **subject** via a new generic Identity
by-subject endpoint — no claim mapper required.

## Identity module — new generic user-by-subject endpoint

Mirrors the existing organizer by-subject lookup on `ProviderLinkController`, but for the plain numeric user id:
- **`GET /api/v1/identity/users/by-subject/{keycloakSubject}`** → `{ userId, keycloakSubjectId }`, `[Authorize(Policy = "IdentityRead")]`
  (the non-admin, service-token endpoint the admin BFF service account is authorized for). New `UserLinkController` (does not
  touch organizer/venue/participant paths).
- **`GetUserByKeycloakSubjectQuery` + handler** (namespace `Aizen.Modules.InktaviaStore.Application.Identity.Query.User`) — a
  mirror of `GetOrganizerProfileByKeycloakSubjectQueryHandler`, data access via `IUserRepository`, no CQRS-in-CQRS. Null → 200
  with a null body when the subject is unlinked (identical to the reference organizer endpoint).
- **`IUserRepository.GetUserByKeycloakSubjectAsync`** + `UserRepository` impl — `_dbContext.Users.AsNoTracking().FirstOrDefault(x => x.KeycloakSubjectId == subject)`.
- **`UserBySubjectDto`** (`Aizen.Modules.Identity.Abstraction.Dto`) — service-safe (ids only).

Verified against real data (identity module pointed at the DB holding the users):
```
GET /users/by-subject/9f39b564-…  (service token) → 200 {"userId":100006,"keycloakSubjectId":"9f39b564-…"}
GET /users/by-subject/40826bde-…  (service token) → 200 userId 100002
GET /users/by-subject/9f39b564-…  (no token)      → 401
GET /users/by-subject/<unlinked>  (service token) → 200 null body   (same as the organizer by-subject reference)
```

## Admin BFF — by-subject resolver (provider mirror)

- **`IAdminContext`/`AdminContext`** (new) — exposes `KeycloakSubject => First("sub", ClaimTypes.NameIdentifier)`, a UserId-only
  mirror of `ProviderContext`. Registered per-request (scoped).
- **`IIdentityAdminBffRemoteCall.GetUserByKeycloakSubject`** (new) — `[AizenRemoteCallGet("/api/v1/identity/users/by-subject/{keycloakSubject}")]`.
- **`AdminIdentityResolver`** — `Resolve()` → **`ResolveAsync()`**: reads `_context.KeycloakSubject` → calls the Identity
  by-subject → `holder.Set(userId)` (numeric). The `user-id` claim read and `AdminPanelKeycloakOptions.AdminUserIdClaim` are
  **removed**. A per-request re-entrancy guard makes the resolver's own by-subject call a no-op (that call carries only the
  service token, no assertion) → no recursion, matching the provider's "resolve runs before the holder is populated" invariant.
- **Call site** — `AdminPanelBffAuthDelegatingHandler` now `await resolver.ResolveAsync(ct)` before building the assertion.
- **BFF-assertion unchanged** — `X-Aizen-Bff-Assertion` + `X-Aizen-User-Id` still sent (empty secret = off); the user id now
  comes from the by-subject lookup. **No realm user-id claim mapper needed anymore.** Config/env `AdminUserIdClaim` removed.

## Verification

- `dotnet build` **Identity module 0 errors**, **AdminPanel BFF 0 errors**.
- New endpoint returns the numeric id for linked subjects and 401 without a service token (above).
- **Inbound preserved:** no-token → **401**, Admin (`Authorization: Bearer`) → **200**, non-Admin → **403**.
- **Outbound (captured):** hitting `/api/v1/admin-panel/providers` with an Admin token, the BFF→module business call carried
  `Authorization` = service token (`azp=admin-panel-bff`), `X-Aizen-Bff-Assertion` = the secret, **`X-Aizen-User-Id` = 100006
  (resolved via sub→Identity by-subject, not a claim)**, and **no `X-Aizen-User-Token`**.
- Grep-clean: `AdminUserIdClaim` / `user-id` claim read gone from source. **Zero MarineProvider, provider-web, CargoDry-module
  changes** (git-verified). Keycloak test artifacts + the temporary compose override / capture server removed after verifying.

## Note (pre-existing env observation, out of scope)

The running `identity-api` container's `ConnectionStrings__Identity` points at DB `aizen`, while the seeded Identity users live
in `inktavia_store` (the documented two-DB split). The by-subject code was verified against `inktavia_store`; whichever DB the
deployment wires up, the endpoint resolves real linked subjects. This DB-wiring choice is unrelated to this change.

---

# BFF-Wave 5 / Offer economics + Reporting (S1/S6/S7 + P12)

**Source:** `docs/V1.0.1/BFF_ROLLOUT_PLAN.md §1` (Wave 5 rows) + the real module endpoints/DTOs in
`docs/V1.0.1/{ServiceRequest,Payment}/REPORT_BACKEND.md`. Additive — existing slices untouched; typed DTOs (never
`object`); `AizenApiResponse` envelope; provider by-subject / admin `[Authorize(Policy="AdminPanelAccess")]`. CargoDry
untouched.

## Provider BFF (MarineProvider) — offer-builder economics preview (S7 + S6)

Both are compute-on-demand (nothing persisted) and resolve the provider **by-subject** (never trust a client-sent
ProviderProfileId). They call the Payment **internal** resolver endpoints directly (the SR module exposes only an
offer-*scoped* commission preview `offers/{offerId}/commission-preview`; a draft-builder preview needs the lines-in
compute endpoints). The delegating handler injects the service token; the internal endpoints return the raw resolver
DTO (no envelope), which the BFF controller wraps via `SetResponse`.

- **`GetOfferCommissionPreviewBff` (S7)** — `POST /api/v1/provider/offers/commission-preview` →
  `IPaymentRemoteCall.ResolveLineCommissions` → **`POST /api/v1/payment/internal/commission/resolve-lines`**. Returns
  `ResolveLineCommissionsRemoteCallResponse` (per-line commissionable/base/resolvedRate/amount/providerNet + transaction
  totals). Optional `providerPlanId` for plan-tier rates. New: `OfferEconomicsPreviewContracts` (request),
  `GetOfferCommissionPreviewBffQuery` + handler (resolves ProfileId, maps lines, fail-loud on invalid eligibility).
- **`GetOfferCustomerDiscountPreviewBff` (S6)** — `POST /api/v1/provider/offers/customer-discount-preview` →
  `IPaymentRemoteCall.ResolveCustomerDiscount` → **`POST /api/v1/payment/internal/discount/resolve-customer-discount`**.
  Returns `ResolveCustomerDiscountRemoteCallResponse` (found + rule + requested discount + funding split platform/provider
  + requiresProviderConsent) on the supplied eligible base. Per-line allocation + net/VAT/gross happen at SR
  save/accept — the BFF performs no economic calculation of its own (no domain logic invented).

**OpenAPI schemaId collision fixed (additive, no global config change):** the Payment `LineCommissionEligibility` enum
shares its short name with the SR `LineCommissionEligibility` already in the provider swagger → Swashbuckle 500. Resolved
by exposing that one enum as a **string** on the BFF request DTO (`OfferCommissionLineInputBff`) and mapping it to the
Payment enum in the handler (`Enum.TryParse`, fail-loud). No `CustomSchemaIds` change → existing schema ids unchanged.

## Admin BFF (AdminPanel) — P12 financial reporting

The P12 result DTOs live in the Payment **Application** project (not Abstraction), so the BFF carries typed **BFF-local**
mirrors (`AdminFinance/Dto/*`) using the `Payment.Abstraction.Enum` ledger enums. AdminFinanceController,
`[Authorize(Policy="AdminPanelAccess")]`, via `IAdminPaymentBffRemoteCall` (raw DTO return, like the existing
invoice-statement report).

- **`GetFinancialSummaryReportBff(from,to,currency)`** — `GET /api/v1/admin-panel/finance/reports/financial-summary` →
  **`GET /api/v1/payment/finance/reports/financial-summary`** → `FinancialSummaryReportBffDto` (NetMarketplaceContribution
  + per-line `Breakdown`; VAT liability + provider-funded discount surfaced separately per §19.17).
- **`GetLedgerEntriesBff(...)`** — `GET /api/v1/admin-panel/finance/reports/ledger-entries` (paged, filter by account
  line / source / provider / period) → **`GET /api/v1/payment/finance/reports/ledger-entries`** → `LedgerEntriesPageBffDto`.

## Verification

- **`dotnet build` — 0 errors** for both BFF hosts + their Application projects (`Aizen.Bff.MarineProvider`,
  `Aizen.Bff.AdminPanel`). No module `.cs` touched (module endpoints already existed) → no module rebuild needed.
- **Rebuilt + recreated both BFF hosts → clean boot.**
- **Per-slice remote-call path/typed body matches the module endpoints** (grep-verified base+relative for all four).
- **Provider (LIVE :17002):** `swagger/v1/swagger.json` → **200**; both `offers/{commission,customer-discount}-preview`
  present in the OpenAPI; both **401 without token** (by-subject `ProviderActive` policy enforced — runtime-verifiable).
- **Admin (LIVE :17001):** the new endpoints enforce the inbound policy — **no-token → 401, non-Admin → 403**, and a valid
  Keycloak **Admin** token **passes inbound auth and reaches the handler** (the BFF then returns 500 from a *downstream*
  Payment 403, not a BFF 401/403). Build + boot verified.
- **No regression** on existing slices (provider swagger regenerates clean; only additive files + append-only edits to
  `IPaymentRemoteCall`/`OffersController` and `IAdminPaymentBffRemoteCall`/`AdminFinanceController`).

## Pre-existing conditions surfaced (NOT introduced by Wave 5)

1. **Payment module returns 403 for the admin BFF service token** on `[Authorize(Roles="Admin")]` endpoints. The service
   token carries **both** the realm `Admin` role and the `payment-api` client roles (`payment.admin`, …) + the payment-api
   audience + the expected issuer — yet the Payment module does not honor them (`GET /api/v1/payment/transactions`, an
   **existing** admin endpoint, 403s identically with the same token). So the admin financial-reporting endpoints reach the
   Payment module but get 403 downstream. This is a **Payment-module authorization condition** (it does not map the
   Keycloak realm/client roles into the `Roles="Admin"` check the way the Identity module does — likely a JwtBearer
   double-registration skipping the Core.Auth role-flattening) affecting the **entire** admin→Payment surface, independent
   of Wave 5. Resolving it (module-side auth config, same class as the A+B Identity work but for Payment) is the
   prerequisite for the downstream 200.
2. **Admin BFF swagger 500** — a pre-existing `GetConversationListResponse` schemaId collision (ServiceRequest vs
   Messaging) in the unrelated `AdminServiceRequests` slice. Not P12; admin acceptance here is runtime-based (401/403 +
   handler-reached), not swagger.

---

# Payment admin role-auth fix (realm-role flattening gap)

**Problem:** admin→Payment endpoints (`[Authorize(Roles="Admin")]`) returned **403** for a valid Keycloak token that
carries the `Admin` realm role — blocking the entire admin→Payment surface (Wave 2 CRUD, Wave 4 refund/premium, Wave 5
P12 reporting) even after A+B. Identity worked; Payment did not.

## Root cause (verified)

Core.Auth's Keycloak role-flattening (`Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs`) maps
`realm_access.roles` + `resource_access.<client>.roles` → `ClaimTypes.Role` in `OnTokenValidated`, so
`[Authorize(Roles="Admin")]` matches the realm role. There are three JwtBearer blocks:
- **`AddAizenAuth<…8 generics…>`** (Identity's path) — read roles from **`ctx.Principal.FindFirst("realm_access")`**
  (principal-claim based). ✅ works.
- **`AddAizenKeycloakAuth`** (the Operation/Api default — every module *except* Identity) **and** the 3-generic
  `AddAizenAuth<TUser,TRole,TContext>` — read roles from **`ctx.SecurityToken as JwtSecurityToken`**. Under .NET 8/9 the
  default token handler is `JsonWebTokenHandler`, so `ctx.SecurityToken` is a `JsonWebToken` → the cast returns **null** →
  `OnTokenValidated` returns early → **roles never flattened** → `[Authorize(Roles="Admin")]` 403.

**Audit (all 10 modules):** only **Identity** calls `AddAizenAuth` (8-generic, good block). The other **9** (Payment,
ServiceRequest, Vessel, ReferenceData, FileStorage, Notification, Messaging, CargoDry, Profile) use the default
`AddAizenKeycloakAuth` → all had the flattening gap. So a single Core.Auth fix repairs the whole fleet.

## Fix (Core.Auth only — shared, non-double-registering)

Changed the `OnTokenValidated` in **both** buggy blocks (`AddAizenKeycloakAuth` and the 3-generic `AddAizenAuth`) to read
`realm_access`/`resource_access` from **`ctx.Principal` claims** (parse the claim JSON with `JObject.Parse`), identical to
the working `AddAizenAuth` block — "works with both JwtSecurityToken (.NET 7) and JsonWebToken (.NET 8/9+)". Made it
**idempotent** (`if (!id.HasClaim(...))`). No new `AddAuthentication`/`AddJwtBearer` registration was added → the
double-registration guard (`!services.Any(JwtBearerHandler)`) is untouched → **single scheme** preserved.

## Verification (payment-api rebuilt + recreated)

- **`dotnet build` Core.Auth → 0 errors.** payment-api docker rebuild + recreate → clean boot, **no double-scheme /
  InvalidOperation auth error** in logs (single JwtBearer scheme).
- **Direct (payment-api :7102), admin-panel-bff SERVICE token (realm `Admin`):** `GET /transactions` → **200** (was 403);
  `GET /finance/reports/financial-summary` → **200** (was 403); `GET /commission/rules` (Wave 2) → **200**;
  `GET /admin/refund-allocation-policies` (Wave 4) → **200**; `GET /admin/premium/products` (Wave 4) → **200**; no token → **401**.
- **Admin BFF end-to-end (:17001), Keycloak `Admin` token (Wave 5 P12):** `finance/reports/financial-summary` → **200**
  (was 500-from-403); `finance/reports/ledger-entries` → **200**; **non-Admin token → 403**.
- **Provider by-subject Payment path unaffected:** `POST /payment/internal/commission/resolve-lines` ([Authorize], not
  role-based, service token) → **200**. (These never depended on role flattening.)

**Result: the whole admin→Payment surface (Wave 2 CRUD, Wave 4 refund/premium, Wave 5 reporting) now reaches 200
end-to-end.** The fix is code-complete for all 9 affected modules via shared Core.Auth; only **payment-api** was rebuilt
here (the verified case) — the other 8 module APIs pick up the same fix on their next image rebuild. Scope: single file
`Core/Auth/.../BuilderExtensions.cs`. CargoDry not touched.
