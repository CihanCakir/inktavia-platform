# FE_ADMIN — operational queues: P10 refund/chargeback + provider-balance ledger, and I1 sub-merchant KYC (admin-web, FE-only)

> **A different shape from the rule-CRUD wave:** these are **operational queues** — paged lists an admin *works*
> (process a refund, review a chargeback, adjust a balance, verify/reject a sub-merchant), not config rules with
> conflict banners. **The BFF + module are already done** (BFF-Wave 1 for I1, BFF-Wave 4 for P10), so this is **FE-only**:
> build the screens against the existing endpoints. **No admin-web screens exist for any of these yet.**
>
> **Do NOT touch** backend/BFF/Keycloak, provider-web, or CargoDry. Reuse the admin Stitch design system + the existing
> paged-table/detail-drawer/row-action patterns from the transactions screens. **Two phases: do Phase 1 fully + verify,
> then Phase 2.** Factor a small **reusable `QueueTable`** (paged + filters + row actions + detail drawer) in Phase 1 so
> Phase 2 (and future queues) reuse it.
>
> **Enum-int caveat:** several queue fields are **integer enums** on the wire (`RefundType`, `Reason`, `Status`,
> `Cause`, `ReleaseState`, `OnboardingStatus`). Map each to a human label from the module's enum definition — do NOT
> render raw ints. Keep a single labeled map per enum (i18n tr+en).

## Ground truth — endpoints (base `/api/v1/admin-panel`, `[Authorize(Policy="AdminPanelAccess")]`) + DTOs (confirmed)
**P10 (AdminPaymentController):**
- `GET payment/admin/refund-queue` (paged) → `RefundQueueItemDto` { Id, PaymentTransactionId, RefundCode, Amount,
  CurrencyCode, RefundType:int, Reason:int, Status:int, Cause:int?, ReleaseState:int?, RefundAllocationId?,
  GatewayRefundReference?, ProcessedAt?, CreatedAt? } + paged wrapper.
- `GET payment/admin/chargeback-queue` (paged) → `ChargebackQueueItemDto` { Id, PaymentTransactionId,
  GatewayChargebackReference, Amount, CurrencyCode, ChargebackExpenseAmount, ProviderRecoveredAmount,
  RemainingNegativeBalance, ReceivedAtUtc }.
- `GET payment/admin/provider-balances` (paged) → provider-balance ledger (`ProviderBalanceAdminDtos`: current balance,
  `NegativeBalanceLimit`, `IsOverLimit`, movements). (Check for a movements/detail endpoint + an audited **adjust**
  endpoint; wire them if present.)
- Refund actions on a transaction: `GET payment/admin/transactions/{id}/refund-history`,
  `POST payment/admin/transactions/{id}/refund`, `POST payment/admin/refund-records/{id}/reverse`.
- (Config, DEFERRED — optional follow-up) `GET/POST payment/admin/refund-allocation-policies` (+ resolve/{id}/deactivate)
  — a rule that would reuse the P2 `rule-crud` blocks (conflict `RefundAllocationPolicyConflict` = 5101, already in
  `RULE_CONFLICT_CODES`). **Not in scope here; note it.**

**I1 (AdminPaymentController):**
- `GET payment/providers/sub-merchant/onboarding-queue` (paged) → `ProviderSubMerchantOnboardingQueueItemDto`
  { ProviderProfileId, OnboardingStatus:enum, IsSplitEligible, HasIban, LegalName?, TaxNumberMasked?,
  SubMerchantKeyMasked?, VerifiedAt?, Status }.
- `POST payment/providers/{providerProfileId}/sub-merchant/verify` (SubMerchantCreated → Verified).
- `POST payment/providers/{providerProfileId}/sub-merchant/reject` (typed reason body → Rejected).

FE data layer: extend `endpoints.ts` + `paymentApi.ts` + add hook sets, mirroring the existing payment query/mutation
hooks; envelope-tolerant.

## Phase 1 — P10 money-ops queues
Build a reusable **`QueueTable`** (paged, server-driven filters, sortable columns, row → detail drawer, row actions with
confirm) under `src/features/payments/queues/` (or similar), then three screens:
1. **Refund queue** (`RefundQueuePage`): columns RefundCode, transaction, Amount, RefundType/Reason/Status/Cause/
   ReleaseState (labeled), ProcessedAt/CreatedAt; filters (status, cause, date, provider/transaction). Row drawer:
   refund detail + **refund-history** (`.../transactions/{id}/refund-history`); actions: **process a refund**
   (`POST .../transactions/{id}/refund`, typed body) and **reverse** a refund record (`POST .../refund-records/{id}/reverse`)
   — both with a confirm dialog + result toast; these are irreversible money ops, so make the confirm explicit.
2. **Chargeback queue** (`ChargebackQueuePage`): columns GatewayChargebackReference, transaction, Amount,
   ChargebackExpenseAmount, ProviderRecoveredAmount, RemainingNegativeBalance, ReceivedAtUtc; filters (date, provider).
   Read-only monitoring (13-month window) unless a chargeback action endpoint exists — if not, no actions.
3. **Provider-balance ledger** (`ProviderBalancesPage`): columns provider, current balance, `NegativeBalanceLimit`,
   `IsOverLimit` (danger chip); row drawer: balance movements; **audited adjust** action if the endpoint exists (typed
   reason + amount, confirm). Over-limit rows visually flagged.
Add routes + payments-dashboard nav for all three. i18n tr+en.

## Phase 2 — I1 sub-merchant KYC queue
`SubMerchantKycQueuePage` using the same `QueueTable`: columns provider (LegalName), OnboardingStatus (labeled),
IsSplitEligible, HasIban, TaxNumberMasked, SubMerchantKeyMasked, VerifiedAt; filter by status. Row actions:
**Verify** (`POST providers/{id}/sub-merchant/verify`, confirm) and **Reject** (`POST providers/{id}/sub-merchant/reject`
with a **typed reason** form — reason required). Show the masked KYC fields read-only; never render full tax/sub-merchant
key. Route + nav. i18n tr+en.

## Don't-break / QA
- Stitch design; reuse the transactions table/drawer patterns + the new `QueueTable`; envelope-tolerant (real DTO field
  names above); int-enums → labeled maps.
- Money/KYC actions (refund, reverse, verify, reject) are **irreversible** — always a confirm dialog + clear success/
  error feedback; on a fail-loud envelope show the message (these are not conflict rules, so no `RuleConflictBanner`).
- `npm run typecheck` clean; existing admin screens unregressed; no backend/BFF changes; provider-web/CargoDry git-clean.
- **Security:** these are admin-only; the routes sit under the existing admin auth/guard. Do not expose unmasked KYC.

## Verification (on-screen; keycloak-init ran, fresh admin login admin.user@inktavia.com, OTP from identity-api logs)
Phase 1: the refund queue lists (seeded refund/chargeback data exists from the Wave-B seed + P10 mocks) with labeled
enums; open a row → refund-history renders; a chargeback queue lists with the expense/recovery/remaining columns; the
provider-balance ledger lists with an over-limit provider flagged (Provider 2 is over-limit from the Wave-B seed).
Phase 2: the sub-merchant KYC queue lists providers by onboarding status; verify moves one to Verified; reject with a
reason moves one to Rejected. (Do irreversible actions against disposable/demo records.) typecheck clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_OPS_QUEUES.md`: the reusable `QueueTable`, each screen + its actions, the enum-label
maps, the on-screen transcript, and the deferred RefundAllocationPolicy CRUD (a rule-crud follow-up). After this, the
remaining wave items are P11 offer-boost (provider) and the P12 financial reporting dashboard (finale).
