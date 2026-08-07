# Admin QA — CargoDry (A-QA3)

~23 routes (kits, batches, products, analytics, qr-lookup, lifecycle-events, alerts, renewals, commercial settlements +
automation + attributions, rules resolve-preview, opportunity-routing-preview). Sidebar: CargoDry ▾ (16 children). All pages
are **wired to real BFF hooks** — no stub/MISSING pages, no dead onClick.

## Static + live findings
**Currency (X3) — latent USD.** Single origin = product create default:
- `CargoDryProductsPage.tsx:40` `currencyCode:'USD'` (DEFAULT_FORM); `:119` picker `['USD','EUR','GBP','AED','SGD','TRY']`
  (USD first, TRY last); price render `en-US` + `product.currencyCode` (`:385,:515`); batch modal mirrors
  (`CargoDryBatchGenerateModal.tsx:254`).
- Propagates to renewals which render raw inherited `currencyCode`: `CargoDryRenewalPreparationDetailPage.tsx:169`,
  `CargoDryRenewalPreparationsPage.tsx:154`, `CargoDryRenewalCandidatesPage.tsx:164`.
- **Live nuance:** seeded products currently display **TRY** (live: TRY 250/400/300/150) — so this is a **latent** bug, not
  a current on-screen USD. New products/renewals would bill USD until the default is fixed.
- ✅ Commercial/settlement cluster is correct TRY + `tr-TR` (dashboard/settlements/attributions/automation/rule-preview).

**i18n (X6) — largest surface.** Only **4 of 23 pages** call `t()` (CargoDryListPage, OpportunityRoutingPreview,
RenewalCandidates, Analytics). The other **19 are 100% hardcoded English** — entire commercial/settlement/automation cluster,
kit detail/list, batch pages, products, qr-lookup, lifecycle events, alerts, renewal prep/detail. No `tr` strings exist for
them. Live-confirmed on products ("Premium Hardware Fleet", VALIDITY/PRICE/DEVICE/VIEW SPECS/EDIT/Register Product).

**Enum (X4) — GOOD.** Commercial BFF ships `*Name` strings (statusName/salesChannelName/modeName/…) and every page renders
the name, not the number. Kit/renewal/lifecycle/alert statuses are string unions with badge maps. No leaks.

**UI-only placeholders** (commented, not API-backed, shown as if real): "Sensor Arrays" (`CargoDryListPage.tsx:75,407`),
"Quality Compliance" (`CargoDryBatchListPage.tsx:39`), "SYSTEM STATUS" strips (`CargoDryProductsPage.tsx:13,582` — live:
QR Signing Service / Key Vault / IoT Telemetry Bridge / Serial Ledger).

**Robustness:** `renewalPrice.toFixed(2)` on `CargoDryRenewalPreparationsPage.tsx:154` is unguarded → 500-in-render if BFF
returns null (candidates page guards it, preparations page does not).

## Live walkthrough checklist
- [ ] Register a new product → default currency is **₺**, picker TRY-first; kit/renewal pricing shows ₺.
- [ ] Every CargoDry page renders TR when locale=tr (currently 19 pages stay English).
- [ ] Commercial settlements/automation/attributions: statuses show names, ₺ figures correct.
- [ ] A renewal candidate with null price doesn't crash the preparations list.
- [ ] UI-only strips (sensors/quality/system-status) labelled as placeholder or removed.

## Fix candidates
`FIX_A_QA3_CARGODRY_CURRENCY` (product default →TRY + renewal/product formatters →₺/tr-TR), `FIX_A_QA3_CARGODRY_I18N` (19
pages), `FIX_CARGODRY_RENEWALPRICE_GUARD`.
