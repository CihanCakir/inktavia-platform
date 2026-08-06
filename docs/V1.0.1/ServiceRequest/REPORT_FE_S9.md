# REPORT — FE_S9: line-level profit protection (admin policy fields + provider reason surfacing)

> **Repos:** `inktavia-marine-admin-web` + the **AdminPanel BFF** (`addesso-project/Bff/src/AdminPanel`), and a light touch
> in `inktavia-marine-provider-web`. All **additive**, tr + en, server owns every threshold (the FE never computes a
> protection value). **NOT committed — trees left for review.** (The S9 backend it consumes is itself uncommitted.)

## Result

- **AdminPanel BFF:** builds **0 errors**. The ProfitProtectionPolicy passthrough DTOs + create/update requests carry the 8
  S9a fields (concrete records, envelope-correct, matching the module DTO/request names exactly).
- **admin-web:** `tsc --noEmit` **clean (0)**, `eslint` on the changed files **clean** (the 6 `react-refresh/only-export-components`
  warnings on `ProfitProtectionPolicyForm.tsx` are **pre-existing** — identical count before my change; I introduced none).
- **provider-web:** `tsc --noEmit` **clean (0)**, `eslint` on the changed files **clean (0)**.
- **i18n:** every new string is tr + en; key parity verified exact (payments: en 1988 / tr 1988, no orphans either way;
  offerBuilder: en 139 / tr 139).

## PART A — Admin: line-level policy fields

### BFF passthrough (`Bff/src/AdminPanel/.../Payment/Dto/ProfitProtectionPolicyBffDtos.cs`)

Additively extended, matching the module names 1:1 (the remote call serializes the request by property name and
deserializes the module response into the BFF DTO, so the fields flow straight through):

- `CreateProfitProtectionPolicyBffRequest` + `UpdateProfitProtectionPolicyBffRequest`: 8 fields appended as optional record
  params with the module's no-op defaults (caps `1m`, floors/rates `0m`, `StrategicLossExceptionEnabled = false`,
  `StrategicLossExceptionMaxLineDeficit = 0m`) — so an omitted field is defaulted server-side.
- `ProfitProtectionPolicyListItemBffDto` + `ProfitProtectionPolicyDetailBffDto`: the same 8 read fields appended.

The 8 fields: `defaultLineMinProviderReceivableRate`, `defaultLineMinProviderReceivableAmount`,
`defaultAllowedProviderFundedDiscountRate`, `defaultAllowedPlatformFundedDiscountRate`, `lineCommissionFloorRate`,
`minLinePlatformContributionRate`, `strategicLossExceptionEnabled`, **and** the backend-carried limit
`strategicLossExceptionMaxLineDeficit`.

### admin-web types (`src/shared/api/types/payment.types.ts`)

`ProfitProtectionPolicyDto` gains the 8 fields (required — the module always returns them); the create/update request
interfaces gain them optional (the BFF defaults them).

### `ProfitProtectionPolicyForm.tsx`

- 8 fields added to `PolicyFormState` + `EMPTY_POLICY_FORM` (launch **no-op** defaults: caps `100`%, floors `0`,
  `strategicLossEnabled = false`, limit `0`), to `policyFormFromDto` (fractions → percent for the 5 rate fields; boolean
  for the toggle; amounts as-is), and to `commonBody` (percent → fraction on submit; the toggle boolean; amounts absolute).
- New **"Line-level protection (S9)"** section: 6 `NumField`s (rates with a `%` suffix per the existing `pct` convention,
  amounts as currency) + a `strategicLossExceptionEnabled` **toggle** (default off; the per-line limit input reveals only
  when the toggle is on) + a helper note.
- Client validation mirrors the backend: the 5 line rates bounded **0..100%** (new `errors.lineRateRange`), amounts ≥ 0;
  the server business-error envelope is still surfaced verbatim (the existing `RuleConflictBanner` / `editError` path).
- **Launch-default / no-op note** surfaced in the section: "Launch defaults are a no-op (caps 100%, floors 0, exception
  off) so existing offers are unchanged. Part lines (Product/Consumable) take their floor from the part commercial term
  (S5); these are the defaults for the rest."

### `ProfitProtectionPolicyDetailPage.tsx`

New **"Line-level protection (S9)"** `DashboardCard` with a `FieldRow` group mirroring the existing gates / expenses
groups — rates via `pct`, amounts with the currency, the exception as an **on/off badge** (amber = on), the per-line
limit shown only when the exception is on, plus a one-line "part lines take their floor from S5" hint.

## PART B — Provider: rejection-reason readability + follow-up flag

- **New:** `src/features/service-requests/offer/model/lineProtectionErrors.ts` — a pure
  `lineProtectionMessageKey(code)` that maps the 5 S9 codes **5130–5134** to `offerBuilder` i18n keys (else `null`). It
  **never computes a threshold** — it only makes a server code readable.
- **Wired** into the one place an offer failure surfaces to the provider today: `OfferBuilder.tsx` `onSubmit` error
  handler — if a `LineProfitProtection*` code ever appears it renders the mapped tr/en message; otherwise the existing
  behaviour is unchanged (FX-rate special-case first, then the raw server message). Example (tr): *"Bu teklif reddedildi:
  bir kalem asgari hak ediş / marj sınırının altında — fiyatı gözden geçirin."*
- No screen invented; the offer builder / economics panel are otherwise untouched.

### Flagged backend follow-up — **`S9-preview`**

A **proactive** in-builder warning (before submit) is **not** possible today: S9 is enforced **only at owner-side P8
acceptance**, and there is **no line-protection preview endpoint** (unlike S7's commission preview). Building one requires
a new backend read-model that runs `LineProfitProtectionEngine` over the draft and returns per-line pass/breach **without**
persisting — mirroring the S7 preview. Until then the FE must not fake a client-side margin check (the server owns every
threshold). Acceptance is owner-side (no owner app), so there is **no provider-triggered reject flow** to build.

## i18n keys added

- **admin `payments`** — `profitProtectionPolicies.form`: `lineSection`, `lineHint`, `lineMinRecvRate`, `lineMinRecvAmt`,
  `allowedProvFundedRate`, `allowedPlatFundedRate`, `lineCommFloorRate`, `minLineContribRate`, `strategicLossEnabled`,
  `strategicLossEnabledHint`, `strategicLossMaxDeficit`, `lineNoopNote`; `…form.errors.lineRateRange`;
  `profitProtectionPolicyDetail.line`: `title`, `hint`, `on`, `off`. (tr + en)
- **provider `offerBuilder`** — `lineProtection`: `rejected`, `providerReceivable`, `fundedCap`, `negativeContribution`,
  `configError`. (tr + en)

## Verification

**Static (done):** BFF builds 0 errors; admin-web + provider-web `tsc` + `eslint` clean on the changed files; i18n tr/en
key parity exact; no new lint errors introduced; no FE path computes a protection threshold (grep: the only S9 numbers in
the FE are the form's percent↔fraction conversion of *policy inputs* and the pure code→message map — never a margin/floor
computation).

**On-screen (done — client-rendered surfaces; user logged in):**

1. ✅ **Admin create form, Turkish** — Payments → Kâr Koruma → "Politika Sürümü Ekle": the new **"KALEM BAZLI KORUMA (S9)"**
   section renders all 8 fields with the correct **no-op launch defaults** — funded-discount caps `%100`, all floors/rates
   `0`, "Stratejik Zarar İstisnası" toggle **off**; the hint + no-op note render (not raw keys).
2. ✅ **Strategic-loss toggle** — enabling it reveals the "Stratejik Zarar Limiti (kalem başına)" input (conditional field).
3. ✅ **Client validation** — a line rate of `150` on submit is rejected client-side with the localized message
   *"Kalem bazlı oranlar 0 ile %100 arasında olmalıdır."* (new `errors.lineRateRange`) — no BFF call made.
4. ✅ **Admin create form, English** — same section renders as **"LINE-LEVEL PROTECTION (S9)"** with the English labels,
   the `100% / 0 / off` no-op defaults, and the full no-op note ("…Part lines (Product/Consumable) take their floor from
   the part commercial term (S5)…"). Both **tr + en** confirmed on screen.
5. ✅ **Provider messages** — the running provider app serves both `offerBuilder.lineProtection` bundles; all 5 keys
   (5130–5134) resolve, e.g. tr *"Bu teklif reddedildi: bir kalem asgari hak ediş / marj sınırının altında — fiyatı
   gözden geçirin."* / en *"This offer was declined: a line falls below its minimum receivable / margin floor…"* — so a
   surfaced code renders a clear message, not a raw number.

**Still deploy-gated (documented for the reviewer):** the admin **save → reload round-trip** and the detail-card *values*
require the **uncommitted** S9 backend deployed — the running `payment-api`/`bff-adminpanel` predate it and the migration
(8 policy columns) is not applied to the dev DB, so today a save would not persist the 8 fields and the detail card would
read them as empty. A live provider `5130`–`5134` is likewise only producible at **owner-side P8 acceptance** (no owner
app). To finish: apply the migration + redeploy `payment-api` and `bff-adminpanel`, then edit a policy → tighten
`defaultLineMinProviderReceivableRate` / enable the strategic-loss exception → save → reopen and confirm the detail
"Line-level protection" card shows the new values + the exception **on** badge (round-trips through the BFF).

## Next SR phase

**S13 (dispute)** or **S12 (recurring)** — plus the flagged **`S9-preview`** backend follow-up (a non-persisting
line-protection preview endpoint) to enable a proactive in-builder margin warning for providers.
