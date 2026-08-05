# FE_S2_S3 — pricing-attribute UI (admin CRUD + provider picker) & offer-FX display (provider + admin)

> **Repos:** `inktavia-marine-admin-web`, `inktavia-marine-provider-web`, and the two BFFs in `addesso-project/Bff/src/{AdminPanel,MarineProvider}`.
> Surfaces the two backend phases just shipped: **S2** (pricing attribute definitions + per-line values, R4 lookup-backed)
> and **S3** (offer-time FX convert-to-TRY + immutable rate snapshot). All **additive**; every existing screen keeps
> working. **The server owns every total/rate** — the FE only sends inputs and displays server results (the offer-builder's
> governing rule). Turkish + English for every new string.

## Ground rules (respect existing conventions — they're live in the repos)
- Follow the established patterns: admin CRUD like the existing reference-data / `pages/app/*Page.tsx` pages + `features/*`
  hooks (TanStack Query, the shared `Button`/`AlertBanner`/table components, `queryKeys`, lucide icons, i18n namespaces).
  Provider follows `features/service-requests/offer/*` (`OfferBuilder`, `useOfferDraft`, `offerApi`, `offerTypes.ts`).
- **BFF passthrough first, then FE.** Neither BFF exposes S2/S3 yet — add typed passthrough (concrete DTOs, envelope-correct
  Refit — never `object`/`JsonElement`; wrapped modules → `AizenApiResponse<T>`) before wiring the screens.
- Additive DTO fields only; don't rename existing offer/economics fields. tr+en; typecheck + lint clean.

---

## PART A — S2 pricing attributes

### A1. Admin: `PricingAttributeDefinition` CRUD (`inktavia-marine-admin-web` + AdminPanel BFF)
**BFF (AdminPanel):** add passthrough Command/Query wrapping the module admin endpoints under
`api/v1/admin/service-requests/pricing-attributes` (list, get, create, update, delete/deactivate). Mirror the existing
`AdminServiceRequests` command/query style. The list/detail DTO carries: `code`, `nameTr`/`nameEn`, `dataType`
(Lookup/Number/Text/Boolean), `lookupGroupCode` (when Lookup), `isRequired`, `sortOrder`, `min`/`max`, `isActive`, and the
applicable **service category codes**.
**FE:** a new admin page (route + nav entry under the ServiceRequest/ReferenceData area — match where the SR admin config
lives) listing definitions with a create/edit drawer or dialog:
- Fields: code (immutable after create), name tr/en, DataType select, **when Lookup** a `LookupGroupCode` picker (fed by
  the existing R4 marine-lookup groups — reuse the reference-data lookup hook so the admin picks a real group, not a free
  string), `isRequired`, min/max (Number only), category multi-select (the SR service categories), active toggle.
- Client validation mirrors the backend (unique code enforced server-side — surface `409`/business-error envelope
  cleanly; min≤max; Lookup requires a group). Fail-loud on the server's `SR_PRICING_ATTR_*` / unknown-group error — show
  the message, don't swallow.

### A2. Provider: offer-line attribute picker (`inktavia-marine-provider-web` + MarineProvider BFF)
**BFF (MarineProvider):** add two passthrough surfaces on `ServiceRequests`:
1. **Get applicable attributes** for an SR (by its `ServiceCategoryCode`) — returns each applicable definition **with its
   resolved Lookup options** (label tr/en + item code) so the FE renders the dropdown without a second call.
2. **Set / get line attribute values** for an offer line (the module's provider set/get endpoints).
Typed DTOs; provider-identity via the established BFF assertion path.
**FE (`OfferBuilder`):** per offer line, render an **Attributes** section driven by the applicable definitions for the
SR's category:
- Lookup → a `<select>` of the resolved R4 options (tr/en label); Number → numeric input (respect min/max); Text → text;
  Boolean → toggle. Mark **required** definitions; block submit (client-side) if a required attribute is empty, and also
  surface the server's `SR_PRICING_ATTR_*` rejection verbatim.
- Persist values through the set endpoint on the same save cadence as the line inputs (attributes are **descriptive** — do
  **not** feed them into the provisional totals; the economics panel is unchanged).
- Keep it collapsible/secondary so a category with no attributes shows nothing new.

---

## PART B — S3 offer FX display

> **FE currency model note:** the provider offer builder carries **one offer-level** `terms.currencyCode` (not per-line).
> So the FE surfaces **offer-level** FX: build in EUR/USD/TRY, and when it isn't TRY the customer-facing figures are the
> **converted TRY**. Per-line mixed currency is a backend capability (S3 handles it) but is **not** surfaced now — deferred
> until the builder supports per-line currency. Don't add a per-line currency control in this pass.

### B1. Provider: converted-TRY display + freeze note (`OfferBuilder` / economics preview)
- The submit-time convert already happens server-side; the **preview/detail DTOs return both the source figure and the
  converted-TRY figure** (S3d). Thread those through the MarineProvider BFF offer detail/preview passthrough (additive
  fields: per-line `sourceUnitPrice`, `currencyCode`, `resolvedTryUnitPrice`; offer-level the FX snapshot rows `{sourceCcy,
  rate, rateDate}`).
- When `terms.currencyCode !== 'TRY'`: show, next to each line and the grand total, **both** "₺ converted" and the source
  figure, plus a small line "Kur: 1 EUR = ₺42,60 · kabulde sabitlenir / rate fixed at acceptance" (use the snapshot rate).
  When TRY, the panel is **byte-identical to today** (no FX chrome).
- On submit, if the server returns **`SR_FX_RATE_UNAVAILABLE`**, show a clear inline error ("Bu para birimi için güncel kur
  bulunamadı — lütfen sonra tekrar deneyin veya TL ile fiyatlayın"), not a generic failure.
- On an **accepted/submitted** offer, show the **frozen** rate (read-only) so the provider sees the locked TRY total.

### B2. Admin: FX visibility on the offer/operation detail (`inktavia-marine-admin-web` + AdminPanel BFF)
- Extend `GetAdminServiceRequestOffers` / `GetAdminServiceRequestOperationDetail` passthrough DTOs (additive) with the
  offer FX snapshot rows + per-line source/resolved figures.
- On the admin SR offer view, render a small **FX** block: source currency, applied rate, rateDate, and the converted TRY —
  so an operator auditing an accepted offer sees the frozen rate that produced the TRY total. Read-only.

---

## Don't-break / QA
- Additive across both FEs + both BFFs: new admin CRUD page + provider attribute picker + FX display + the passthrough DTOs.
  Existing offer builder, economics panel, admin SR pages, reference-data pages **unchanged** for the TRY-only / no-attribute
  case. Server owns totals/rates — the FE never computes an FX rate or a line total.
- tr + en for every new string (new i18n keys in the right namespaces). Typecheck + lint clean in both repos. BFF builds
  clean; DTOs concrete + envelope-correct.

## Verify (on screen)
1. **Admin S2:** create a `PAINT_TYPE` Lookup definition scoped to a hull category (group picked from real R4 groups; an
   invalid/empty group is rejected); it lists, edits, deactivates; code is immutable after create.
2. **Provider S2:** opening the offer builder for that category shows the Paint Type dropdown (real R4 options); a required
   attribute left empty blocks submit; a valid selection saves and does **not** change the totals; a server rejection shows
   its message.
3. **Provider S3:** build an offer in EUR → each line + grand total show ₺converted and the EUR source with the acceptance-
   frozen-rate note; a currency with no rate → `SR_FX_RATE_UNAVAILABLE` inline; a TRY offer shows no FX chrome (identical to
   today).
4. **Admin S3:** the admin offer detail shows the frozen FX rate + converted TRY on an accepted foreign-currency offer.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FE_S2_S3.md`: the admin CRUD page + provider picker (S2), the provider + admin FX
display (S3), the BFF passthrough added on each side, the i18n keys, and the on-screen verification. Then next SR phase
(S4 travel — I2 ready / S5 part terms).
