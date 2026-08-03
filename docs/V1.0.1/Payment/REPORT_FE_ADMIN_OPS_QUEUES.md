# REPORT — FE_ADMIN operational queues (P10 refund/chargeback + provider-balance ledger, I1 sub-merchant KYC)

**Repo:** `inktavia-marine-admin-web` (FE-only; no backend/BFF/Keycloak/provider-web/CargoDry changes).
**Spec:** `docs/V1.0.1/Payment/FE_ADMIN_OPS_QUEUES_P10_I1.md`
**Scope delivered:** Phase 1 (P10 money-ops queues) + Phase 2 (I1 sub-merchant KYC), both on-screen verified.
**Typecheck:** `npm run typecheck` → clean. Existing admin screens unregressed (only additive changes + new files).

These are **operational queues** an admin *works* — process a refund, review a chargeback, adjust a
balance, verify/reject a sub-merchant — **not** conflict rules, so there is **no `RuleConflictBanner`**; money/KYC
actions are irreversible and always gated behind an explicit confirm dialog with clear success/error toast feedback.

---

## 1. Reusable `QueueTable` (+ queue toolkit)

New feature module `src/features/payments/queues/`:

| File | Purpose |
|---|---|
| `QueueTable.tsx` | The reusable shell: Stitch `DashboardCard` + optional server-driven filter bar + the shared paged `DataTable` (row → detail drawer via `onRowClick`) + a count footer. Re-exports `Column<T>`. Used by all four screens. |
| `QueueActionDialog.tsx` | Confirm/action dialog aligned to the `surface-*` tokens the payment screens use (the shared `ConfirmModal` still uses the legacy `marine-navy` palette). Supports a plain confirm (verify) **or** an inline typed-body form (process refund, reverse, adjust, reject). `confirmDisabled` + spinner + danger variant. |
| `QueueFields.tsx` | Small labeled form controls (`Field`, `TextInput`, `NumberInput`, `TextArea`, `SelectInput`), a read-only `DrawerRow`, and a `FilterField` block — keeps the four pages consistent. |
| `enumMaps.ts` | Int-enum → i18n-leaf-key maps + `StatusBadge` variant maps (see §2). |
| `format.ts` | `formatMoney` (currency-symbol aware), `formatDate`, `formatDateTime`. |
| `index.ts` | Barrel export. |

The detail drawer uses the shared `@shared/ui/drawer/Drawer`; row actions open a `QueueActionDialog`. Feedback is via
`useToast()` (`success`/`error(title, message)`); all list/detail data is **envelope-tolerant** through the existing
`normalizeSuccess`/`ApiResult` layer (`select: r => r.ok ? r.data : null`), so a fail-loud envelope surfaces its message.

---

## 2. Enum-label maps (int → labeled, i18n tr+en)

Wire fields arrive as **integer enums** (the BFF lacks `StringEnumConverter`). We never render raw ints — each maps to a
stable leaf key resolved under the shared `queues.enums.*` i18n section (both `tr` and `en`), plus a badge variant where
relevant. Source of truth: `Aizen.Modules.Payment.Abstraction.Enum.*`.

| Field | Enum | Values |
|---|---|---|
| `refundType` | `RefundType` | 1 Full, 2 Partial |
| `reason` | `RefundReason` | 1–14 (UserCancel … CompensationCredit) |
| `status` | `TransactionRefundStatus` | 1 Pending, 2 Processed, 3 Reversed, 4 Failed (+ badge variant) |
| `cause` | `RefundCause` | 1–8 (ProviderCancelled … AdministrativeCorrection) |
| `releaseState` | `ReleaseState` | 1 BeforeProviderRelease, 2 AfterProviderRelease |
| `movementType` | `ProviderBalanceMovementType` | 1 RefundClawback, 2 ChargebackClawback, 3 PayoutOffset, 4 ManualAdjustment |
| `onboardingStatus` | `ProviderSubMerchantOnboardingStatus` | 0–6 (NotStarted … Blocked) (+ badge variant) |

i18n keys added to `src/shared/i18n/locales/{en,tr}/payments.json`: `queues.*` (common + `enums.*`), `refundQueue.*`,
`chargebackQueue.*`, `providerBalances.*`, `subMerchantKyc.*`, and four `dashboard.*` nav-tile labels.

---

## 3. Data layer

- **`endpoints.ts`** — added `PAYMENT_REFUND_QUEUE`, `PAYMENT_CHARGEBACK_QUEUE`, `PAYMENT_PROVIDER_BALANCES`,
  `PAYMENT_PROVIDER_BALANCE_BY_ID`, `PAYMENT_PROVIDER_BALANCE_ADJUST`, `PAYMENT_SUBMERCHANT_ONBOARDING_QUEUE`,
  `PAYMENT_SUBMERCHANT_VERIFY`, `PAYMENT_SUBMERCHANT_REJECT`. (Refund-history + refund + reverse already existed.)
- **`payment.types.ts`** — DTO mirrors matching the exact BFF field names: `RefundQueueItemDto`/`RefundQueueResult`/
  `RefundQueueFilters`, `ProcessRefundRequest`, `ReverseRefundRequest`, `ChargebackQueueItemDto`/`…Result`/`…Filters`,
  `ProviderBalanceMovementDto`, `ProviderBalanceAdminDto`/`…Result`/`…Filters`, `AdjustProviderBalanceRequest`,
  `ProviderBalanceAdjustResult`, `SubMerchantOnboardingQueueItemDto`/`…Result`/`…Filters`, `RejectSubMerchantRequest`.
- **`paymentApi.ts`** — `getRefundQueue`, `processRefund`, `reverseRefund`, `getChargebackQueue`, `getProviderBalances`,
  `getProviderBalance`, `adjustProviderBalance`, `getSubMerchantOnboardingQueue`, `verifySubMerchant`, `rejectSubMerchant`.
- **`queryKeys.ts`** — `payments.refundQueue`, `payments.chargebackQueue`, `payments.providerBalances`,
  `payments.subMerchantOnboarding`.
- **Hooks** (`src/features/payments/hooks/`): `useRefundQueue.ts` (list + `useRefundHistoryQuery` +
  `useProcessRefundMutation` + `useReverseRefundMutation`), `useChargebackQueue.ts`, `useProviderBalances.ts`
  (list + detail + `useAdjustProviderBalanceMutation`), `useSubMerchantOnboardingQueue.ts` (list + verify + reject).
  Mutations invalidate the relevant list/detail keys on success.

---

## 4. Screens + actions

All under `src/pages/app/payments/`, each with `PageHeader` + `PaymentBackNav` + `QueueTable`. Routes registered in
`routes.tsx` + `routeObjects.tsx`; nav tiles added to `PaymentDashboardPage` header.

### RefundQueuePage — `/app/payments/refund-queue`
Columns: RefundCode, transaction, Amount, RefundType, Reason, Status (badge), Cause, ReleaseState, CreatedAt — all labeled.
Filters: Status, Cause, ReleaseState (the three the BFF query supports — `?cause&releaseState&status`). Row drawer shows the
full refund detail + the **refund-history** list (`GET admin/transactions/{id}/refund-history`). Actions:
- **Process refund** (`POST admin/transactions/{id}/refund`, body `{refundAmount, reason, refundType, adminNote}`) — form dialog, irreversibility warning, confirm + toast.
- **Reverse** (`POST admin/refund-records/{id}/reverse`, body `{reversalReason(required), adminNote}`) — danger dialog; enabled only for a **Processed** refund.

### ChargebackQueuePage — `/app/payments/chargeback-queue`
Read-only (13-month window) — a "Read-only" badge, no actions (no chargeback action endpoint exists). Columns:
GatewayChargebackReference, transaction, Amount, ChargebackExpenseAmount, ProviderRecoveredAmount,
RemainingNegativeBalance (red when > 0), ReceivedAtUtc. Row drawer shows detail + notes.

### ProviderBalancesPage — `/app/payments/provider-balances`
Columns: provider, currency, Balance (red when negative), NegativeBalanceLimit, **IsOverLimit** danger chip. Filter:
"only negative balances". Row drawer: balance summary + **movements** (`GET admin/provider-balances/{id}`, labeled
`movementType`) + **audited adjust** action (`POST admin/provider-balances/{id}/adjust`, body
`{currencyCode, signedAmount, adminUserId, note}` — `adminUserId` from the auth store, `note` required) — danger dialog.

### SubMerchantKycQueuePage — `/app/payments/sub-merchant-kyc`
Columns: provider (LegalName + id), OnboardingStatus (badge), IsSplitEligible, HasIban, **TaxNumberMasked**,
**SubMerchantKeyMasked**, VerifiedAt. Filter by onboarding status. Masked KYC is read-only and **never** unmasked (a lock
note states this). Actions:
- **Verify** (`POST providers/{id}/sub-merchant/verify`) — confirm dialog; enabled only for `SubMerchantCreated`.
- **Reject** (`POST providers/{id}/sub-merchant/reject`, body `{reason}`) — **required typed reason**; confirm disabled until non-empty.

---

## 5. On-screen verification transcript

Env: full docker stack up (bff-adminpanel :17001, payment-api, keycloak, identity-api); admin-web dev server :3000;
fresh OTP login `admin.user@inktavia.com` (OTP `116448` from `docker compose logs identity-api`).

| Check | Result |
|---|---|
| **Refund queue** lists with labeled enums | ✅ 4 rows; İşlendi/Geri alındı status badges, Tam/Kısmi, Turkish reason/cause/release-state labels |
| Row drawer + **refund-history** | ✅ Drawer shows detail incl. `IYZICO-REFUND-P2-0205`; İade Geçmişi renders `REF-20260610-P205 PROCESSED ₺12.720,00` |
| Process-refund dialog | ✅ Amount + labeled RefundType/Reason selects + admin note + irreversibility warning (cancelled — no live money op) |
| **Chargeback queue** | ✅ Renders with expense/recovery/remaining columns + "Read-only" badge; no chargeback rows seeded → envelope-tolerant empty state |
| **Provider-balance ledger** flags over-limit | ✅ Provider `#100011` "Provider 2 AS": Balance **₺-10.560,00** (red), Neg. Limit ₺5.000,00, **"Limit aşıldı"** danger chip |
| Provider-balance drawer movements + adjust | ✅ Summary + movement `İade geri alımı` (RefundClawback, note "…Wave B demo") + Adjust action |
| **KYC queue** lists by status, masked KYC | ✅ Providers listed with labeled onboarding status; tax/key shown **masked** (`*******321`, `****9F21`); split/IBAN flags |
| **Verify → Verified** | ✅ `#990001` verify confirmed → DB `OnboardingStatus 2→3 (Verified)`, `Status=Active`, `VerifiedAt` set; row drops from the queue |
| **Reject (required reason) → Rejected** | ✅ `#990002` reject-with-reason confirmed → DB `OnboardingStatus 1→4 (Rejected)`, `Status=Blocked`; row now shows "Reddedildi"; empty reason keeps confirm disabled |

### Note on the KYC verification data (runtime DB split)
The running payment-api reads **`inktavia_store`** (it holds the seeded over-limit balance `#100011` the ledger showed),
but the Wave-B sub-merchant onboarding profile seed landed in the **`aizen`** DB — so `inktavia_store.
provider_payment_profiles` was empty and the KYC queue returned 0. This is a **seed/data placement gap, not an FE defect**
(confirmed the `/onboarding-queue` endpoint returns `200 {items:[],total:0}` for every status filter). To exercise the
irreversible KYC actions I inserted three **disposable, clearly-labeled demo rows** (`#990001–#990003`, "Demo … A.Ş.")
directly into the runtime DB — no backend/seed code was changed. They can be removed with:

```sql
DELETE FROM payment.provider_payment_profiles WHERE "ProviderProfileId" IN (990001,990002,990003);
```

(Follow-up for the backend team: align the sub-merchant onboarding seed to the same DB the runtime uses, as the balance
seed already is.)

---

## 6. Deferred — RefundAllocationPolicy CRUD

Per the spec, **`RefundAllocationPolicy` CRUD is deferred** and intentionally **not built** here. It is a config *rule*
(not an operational queue) that would reuse the P2 `rule-crud` blocks and the existing conflict code
`RefundAllocationPolicyConflict = 5101` (already in `RULE_CONFLICT_CODES`). Endpoints exist
(`GET/POST payment/admin/refund-allocation-policies` + `resolve` / `{id}/deactivate`) for a future rule-crud follow-up.

Remaining wave items after this: **P11 offer-boost (provider)** and the **P12 financial-reporting dashboard (finale)**.
