# Admin QA — Reference Data / Lookup / Currencies / Pricing-Attributes / Settings (A-QA7)

Routes: `/app/reference-data` (redirect), `/currencies`, `/lookup-groups` (+ `/:groupId/items`), `/pricing-attributes`,
`/app/settings` (+ `/profile`, `/account`, `/security`, `/preferences`).

## Static findings
- **`/app/reference-data`** `ReferenceDataPage` — pure `<Navigate>` redirect to lookup-groups (V1 superseded).
- **Currencies** `CurrencyExchangeRatePage` — REAL (`useCurrenciesQuery` → `/reference-data/currency`) but 🔴 **USD-pegged
  model**: `currency.types.ts` field `exchangeRateToUsd`; column header "Rate (USD)". Marketplace settles ₺ → the reference
  model should be TRY-anchored. Also: **orphaned** (no nav/link), **dead Edit button**, **fake "Sync" action**, mock
  sparkline, mostly hardcoded English.
- **Lookup groups** `LookupGroupTreePage` — REAL tree (`useLookupGroupTree`) but **dead "Add Root" / "Add child" buttons**
  (no handlers), `handleEditNode` no-op, hardcoded default-expanded ids `['root-1','node-2']` (L26).
- **Lookup items** `LookupItemManagementPage` — **REAL full CRUD** (create/update/delete) + `LookupItemStatusBadge`. Bug:
  breadcrumb link L87 `to="/reference-data/lookup-groups"` is **missing the `/app` prefix** → 404.
- **Pricing attributes** `PricingAttributesPage` — **REAL** full CRUD; numeric `dataType` mapped via `dataTypeName()`; uses
  `serviceRequests` i18n keys. Clean (backend: SR S2).
- **Settings** — `/app/settings` = language card only; `/profile`, `/account`, `/security` = **"coming soon" stubs**
  (hardcoded EN, no `t()`); `/preferences` = LanguageSwitcher + real `NotificationPreferencesCard`.

## Live walkthrough checklist
- [ ] Currencies: reachable from nav; ₺-anchored model (not "Rate (USD)"); Edit works; no fake Sync/mock sparkline.
- [ ] Lookup groups: Add Root / Add child / edit node actually create/update.
- [ ] Lookup items: breadcrumb navigates (no 404); CRUD works.
- [ ] Pricing attributes: create/edit/deactivate an attribute (SR S2).
- [ ] Settings: sub-pages built or hidden; all localized.

## Fix candidates
`FIX_A_QA7_LOOKUP_CRUD_HANDLERS` (+ breadcrumb `/app` prefix), `FIX_A_QA7_CURRENCY_MODEL_NAV` (₺ anchor + nav + real
edit/sync), `FIX_A_QA7_SETTINGS_STUBS` (build or hide).
