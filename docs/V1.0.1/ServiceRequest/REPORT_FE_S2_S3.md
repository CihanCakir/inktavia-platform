# REPORT_FE_S2_S3 — pricing-attribute UI (admin CRUD + provider picker) & offer-FX display (provider + admin)

**Spec:** `docs/V1.0.1/ServiceRequest/FE_S2_S3_ATTRIBUTES_AND_FX.md`
**Repos touched:** `inktavia-marine-admin-web`, `inktavia-marine-provider-web`, and the two BFFs
`addesso-project/Bff/src/{AdminPanel,MarineProvider}` (+ one small additive change in the ServiceRequest **module** to surface
offer-level FX rows). **All additive.** The server owns every total/rate; the FE only sends inputs and displays server results.
**Status:** code complete; both BFFs + the SR module build clean; both FE repos **typecheck + lint clean**. Not committed. On-screen
verification pending a running stack (checklist below).

---

## Shared module change (serves both provider B1 and admin B2)

The MarineProvider/AdminPanel BFFs are thin passthroughs that return the SR module DTOs verbatim. The **per-line** FX fields
(`sourceUnitPrice`, `sourceCurrencyCode`, `settlementCurrencyCode`) already existed on `ServiceRequestOfferItemDto` (BE-S3d) and
flow through. The **offer-level** FX rate rows did not — so, additively, in the module:
- New `OfferFxSnapshotDto` (`SourceCurrencyCode`, `SettlementCurrencyCode`, `Rate`, `RateDate`).
- `ServiceRequestOfferDto.FxSnapshots: List<OfferFxSnapshotDto>` + mapping in `ServiceRequestOfferEntity.ToDto()`.
- `GetByIdWithDetailsAsync` now `.Include(x => x.Offers).ThenInclude(o => o.FxSnapshots)` so the admin operation-detail carries
  the rows; the provider preview path already populates them on the transient offer.

This one module addition feeds the provider preview, the provider offer read, and the admin operation-detail — no BFF DTO copies.

---

## PART A — S2 pricing attributes

### A1 — Admin CRUD (`inktavia-marine-admin-web` + AdminPanel BFF)

**BFF (AdminPanel):** four passthrough surfaces on `IServiceRequestAdminBffRemoteCall` + CQRS mirroring the `AdminReferenceData`
style, exposed on `ServiceRequestsController` (`api/v1/admin-panel/service-requests/pricing-attributes`): `GET` (list,
`?serviceCategoryCode`), `POST` (create), `PUT /{id}` (update), `DELETE /{id}` (deactivate). Reuses the module DTOs
(`PricingAttributeDefinitionDto` / `PricingAttributeDefinitionRequest`) — no BFF copies. The module DELETE soft-deactivates
(keeps the code); the BFF wraps it in an envelope-correct `DeletePricingAttributeResult`.

**FE:** new page `PricingAttributesPage.tsx` (route `/app/reference-data/pricing-attributes`, nav leaf `pricing-attributes` in
the `system` group next to Reference Data). Entity api `pricingAttributeApi` (list/create/update/deactivate via
`normalizeSuccess/Failure`) + feature hooks `usePricingAttributes*`. List table (code, name, type, categories, required, status)
with a slide-in create/edit drawer:
- Fields: **code (disabled/immutable on edit)**, name TR/EN, DataType select, **LookupGroupCode picker fed by the real R4 groups**
  (`useLookupGroupsQuery` → `GET /reference-data/lookup`) shown only for Lookup, min/max (Number only), **service-category
  multi-select** (the real seeded SR categories), required toggle.
- Client validation mirrors the backend (code/name required, Lookup needs a group, min ≤ max, ≥1 category); the server's
  `SR_PRICING_ATTR_*` / unique-code (409) message is surfaced **verbatim** inline (mutations return `ApiResult`, not thrown).
- DataType crosses the wire as its **numeric** enum value on create/update (enum-safe regardless of the BFF serializer);
  reads normalize number-or-string via `dataTypeName()`.

### A2 — Provider offer-line picker (`inktavia-marine-provider-web` + MarineProvider BFF)

**BFF (MarineProvider):** new `PricingAttributesController` (`api/v1/provider`) + CQRS over three module provider routes:
`GET service-requests/{id}/pricing-attributes` (applicable defs **with resolved Lookup options**), `GET/PUT
offers/{offerId}/items/{itemId}/attributes`. Identity via the established `IProviderProfileResolver` + assertion-header handler.

**FE (`OfferBuilder`):** `useApplicableAttributes(serviceRequestId)` (empty ⇒ nothing rendered). A collapsible **`LineAttributes`**
section renders under each priced line **once the line is persisted** (has a server id): Lookup → `<select>` of the R4 options
(tr/en label by `i18n.language`), Number → numeric (min/max), Text → text, Boolean → checkbox. Values load via GET and persist
(full-replace) via PUT on a short debounce; a server rejection shows inline. Required attributes are marked; a required-empty line
reports invalid up via `onValidityChange`, and the builder **blocks Submit** client-side (plus the server rejection is surfaced).
Attributes are descriptive — they never touch the totals/economics panel.

---

## PART B — S3 offer FX display (offer-level `terms.currencyCode`; no per-line currency control)

### B1 — Provider converted-TRY + freeze note (`OfferBuilder`)

- Added an **offer-level currency selector** (TRY / EUR / USD) bound to `terms.currencyCode` (the builder had none).
- Types extended additively: `OfferItemComputed` (`sourceUnitPrice`, `sourceCurrencyCode`, `settlementCurrencyCode`) and
  `OfferComputed` (`fxSnapshots[]`).
- Because a **draft save does not convert** (conversion is submit/preview-only, server-side), a new `useOfferFxPreview` hook calls
  the existing preview endpoint (debounced) **only when the currency isn't TRY** — the server converts and returns the TRY figures
  + the applied rate. The FE never computes a rate.
- Display when non-TRY: each line shows the source figure **and** `≈ ₺<converted>`; the totals block shows
  **"Customer pays (₺) …"** plus **"Kur: 1 EUR = ₺42,60 · kabulde sabitlenir / rate fixed at acceptance"** from the snapshot rate.
  A **TRY** offer renders **no FX chrome** (byte-identical to before) and makes no preview call.
- On submit, `SR_FX_RATE_UNAVAILABLE` is mapped to a clear inline message (tr: "Bu para birimi için güncel kur bulunamadı — …");
  the same message shows in the FX block if the live preview can't find a rate.

### B2 — Admin FX visibility on the offer/operation detail (`inktavia-marine-admin-web`)

- `ServiceRequestBffOffer` gained `fxSnapshots?: OfferFxSnapshotRow[]` (carried through `normalizeOffer`'s spread; per-line FX
  already present on the module DTO).
- `ServiceRequestDetailPage`'s `OffersModal` renders a **read-only FX block** under the financials when `fxSnapshots` is present:
  each row `1 EUR = ₺42.60` + `rateDate`, plus a "rate fixed at acceptance — the TRY total does not re-value" note, so an operator
  auditing an accepted foreign-currency offer sees the frozen rate that produced the TRY quote.

---

## i18n (tr + en for every new string)

- **provider-web** `offerBuilder.json` (tr+en): `attributes.{title,select,requiredBlock}`, `fx.{settleNote,grandInTry,rateLine,
  rateUnavailable}`; reused existing `terms.currency`.
- **admin-web** `navigation.json` (tr+en): `pricingAttributes`; `serviceRequests.json` (tr+en): full `pricingAttributes.*` block
  (title/subtitle/actions/columns/dataType/field/validation) + `offerFx.{title,frozenNote}`.

## Don't-break / QA

- Additive across both FEs + both BFFs + one module DTO. TRY-only / no-attribute offers are unchanged (no FX chrome, no attribute
  UI, no extra calls). Server owns totals/rates — the FE never computes an FX rate or a line total.
- **Builds/checks:** SR module + both BFFs `0 Error(s)`; admin-web `tsc --noEmit` + `eslint` clean; provider-web `tsc --noEmit`
  + `eslint` clean. One admin lint rule (`set-state-in-effect`) drove the drawer to a keyed-remount + `useState` initializer
  (no sync effect).

## Verify (on screen — pending a running stack)

1. **Admin S2:** create a `PAINT_TYPE` Lookup definition scoped to a hull category (group picked from real R4 groups; invalid/empty
   group rejected with the server message); it lists, edits (code disabled), deactivates.
2. **Provider S2:** open the builder for that category → the Paint Type dropdown shows the R4 options; a required attribute left
   empty blocks Submit; a valid selection saves and does **not** change the totals; a server rejection shows its message.
3. **Provider S3:** build an EUR offer → each line + grand total show `≈ ₺converted` and the rate note; no-rate → `SR_FX_RATE_UNAVAILABLE`
   inline; a TRY offer shows no FX chrome (identical to today).
4. **Admin S3:** the admin offer detail shows the frozen rate + rateDate on an accepted foreign-currency offer.

## Known follow-up

- The provider **accepted-offer read-only** view (`RequestDetailPage`) still shows only the TRY total; surfacing the frozen rate
  there needs `fxSnapshots` threaded through the provider detail aggregate (`detailApi`/`MyOffer`) — deferred (the rate is already
  visible live during build/preview and on the admin side). Per-line source figure display uses the FX preview positionally
  (mirrors the economics panel's positional line mapping).

## Next
FE follow-up above, then the next SR phase: **S4 travel** (I2 ready) / **S5 part terms**.
