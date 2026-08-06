# FE_S9 — line-level profit protection: admin policy fields + provider reason surfacing

> **Repos:** `inktavia-marine-admin-web` + the AdminPanel BFF (`addesso-project/Bff/src/AdminPanel`), and a light touch in
> `inktavia-marine-provider-web`. Surfaces **S9** (line-level profit protection): the 8 admin-tunable line-level policy
> fields, and readable messages for the new rejection codes. All **additive**; tr + en. **Do not commit** — leave the tree
> for the user (S9 backend is uncommitted on the part3 branch).

## PART A — Admin: line-level fields on the ProfitProtection policy (main deliverable)
**BFF (AdminPanel):** the ProfitProtectionPolicy CRUD passthrough already exists — extend its DTO + create/update request
(additively) with the 8 S9a fields so the FE can read/write them:
`defaultLineMinProviderReceivableRate`, `defaultLineMinProviderReceivableAmount`,
`defaultAllowedProviderFundedDiscountRate`, `defaultAllowedPlatformFundedDiscountRate`, `lineCommissionFloorRate`,
`minLinePlatformContributionRate`, `strategicLossExceptionEnabled`, and (if the backend carries it) the strategic-loss
limit. Match the module DTO/request names exactly; envelope-correct.
**FE (`inktavia-marine-admin-web`):**
- `ProfitProtectionPolicyForm.tsx` (create/edit): add a new **"Line-level protection"** section with inputs for the 8
  fields — rate fields as percentages (the form's existing `pct` convention), amount fields as currency,
  `strategicLossExceptionEnabled` as a toggle (default off). Client validation mirrors backend (rates 0..1, amounts ≥ 0);
  surface the server business-error envelope verbatim.
- `ProfitProtectionPolicyDetailPage.tsx`: add a **"Line-level protection"** `FieldRow` group displaying the 8 values
  (rates via `pct`, amounts with currency, the exception as an on/off badge), mirroring the existing "gates" / "expected
  expenses" groups.
- **Launch-default note (surface it):** the seeded launch defaults are a **no-op** (caps 100%, floors 0, exception off) so
  existing offers are unchanged — show these values plainly; an admin tightening them is what activates enforcement. A
  short helper/tooltip explaining "part lines take their floor from the part commercial term (S5); these are the defaults
  for the rest" is worthwhile.

## PART B — Provider: rejection-reason readability (light) + follow-up flag
- **Now:** map the new S9 rejection codes **5130–5134** (`LineProfitProtection*`) to clear tr/en messages wherever an offer
  acceptance / economics failure surfaces to the provider (the offer error handling in `features/service-requests/offer/*`)
  — e.g. "Bu teklifteki bir kalem asgari hak ediş / marj sınırının altında — fiyatı gözden geçirin." Do not invent a
  screen; just make the code readable if it appears.
- **Follow-up flag (not in this pass):** a **proactive** in-builder warning (before submit) would need a **line-protection
  preview endpoint** (S9 is enforced only at P8 acceptance today; there is no preview like S7's commission preview). Note
  this as a backend follow-up (`S9-preview`) in the report — do not fake a client-side margin check (the FE must never
  compute protection thresholds; the server owns them).
- Acceptance itself is owner-side (no owner app), so there is no provider-triggered reject flow to build here.

## Don't-break / QA
- Additive: 8 new fields on the admin policy form/detail + BFF DTO/request passthrough + provider code mapping. Existing
  ProfitProtection pages, offer builder, and economics panel unchanged. Server owns all thresholds — the FE only
  reads/writes policy values and displays server results.
- tr + en for every new string (payments / offer namespaces); typecheck + lint clean both repos; BFF builds clean, DTOs
  concrete + envelope-correct.

## Verify (on screen)
1. Admin edits a ProfitProtection policy → the 8 line-level fields save + reload correctly; the detail view shows them;
   launch defaults display as the no-op values.
2. Tightening `defaultLineMinProviderReceivableRate` (or enabling the strategic-loss exception) persists and round-trips
   through the BFF.
3. Provider: a 5130–5134 acceptance/economics error renders as a clear tr/en message (simulate the code) — not a raw code.
4. No FE computes a protection threshold; existing pages unchanged.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FE_S9.md`: the admin policy line-level fields (form + detail + BFF passthrough), the
provider code mapping, the flagged `S9-preview` backend follow-up, i18n keys, and on-screen verification. Then next SR
phase (S13 dispute / S12 recurring). **Do NOT commit.**
