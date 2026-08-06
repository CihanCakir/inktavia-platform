# REPORT — FE_S4_S5: travel-line pricing UI (S4) & part commercial terms (S5)

Surfaces the two backend phases (S4 travel/mobilization pricing, S5 cost-confidential part commercial terms) across **four**
codebases: `inktavia-marine-provider-web`, `inktavia-marine-admin-web`, and the two BFFs
(`addesso-project/Bff/src/{MarineProvider,AdminPanel}`). Everything additive; the server owns every total/allowance; the FE
computes no money. tr + en for every new string.

**HEADLINE (S5, §20.9) — cost confidentiality held on the FE:** no provider- or customer-facing screen or payload renders
`supplierListPrice` / `providerDealerMargin` / cost. The admin `PartCommercialTerm` CRUD is the **only** surface that shows cost.
Proven by grep across both provider surfaces (0 hits) + a codified FE test (below).

---

## PART A — S4 travel-line pricing UI

### A1. Provider travel picker (`inktavia-marine-provider-web` + MarineProvider BFF)

**MarineProvider BFF (passthrough first):**
- `IServiceRequestRemoteCall` (`Common/RemoteClients/IServiceRequestRemoteCall.cs`) — 3 new Refit methods:
  `GetOfferLineTravelPricing`, `SetOfferLineTravelPricing` (→ module `GET/PUT
  /api/v1/service-requests/provider/offers/{offerId}/items/{itemId}/travel-pricing`, wrapped `AizenApiResponse<TravelPricingDetailDto>`),
  and `GetOfferPartTermsPreview` (S5, below).
- New flat handlers `Application/TravelPricing/{GetOfferLineTravelPricingBff,SetOfferLineTravelPricingBff}.cs` — the S2
  resolve-provider-identity + guard + forward + `Refit.ApiException → AizenBusinessException(ExtractMessage)` pattern, so
  `SR_TRAVEL_*` surfaces verbatim.
- New controller `Controllers/V1/TravelPricingController.cs` (route `api/v1/provider`, `ProviderActive` policy) — `GET/PUT
  offers/{offerId}/items/{itemId}/travel-pricing`, both `SetResponse(...)` → outward envelope.

**Provider FE (`features/service-requests/offer/`):**
- `model/offerTypes.ts` — added `OFFER_ITEM_TYPE.Travel = 10` + `Consumable = 9`; new `PRICING_METHOD`,
  `TRAVEL_PRICING_METHOD`, `TravelPricingDetail`, `PartLineAllowance`, `PartTermsPreview` types; added `pricingMethod` to
  `OfferItemInput`.
- `api/offerApi.ts` — `toLineInputs` now sends `pricingMethod` (so the server-side S4 consistency check can pass); shared by the
  draft/preview bodies and the economics adapter.
- `api/travelPricingApi.ts` (new) — get/set the structured detail; `shared/api/endpoints.ts` → `lineTravelPricing`.
- `components/TravelLineForm.tsx` (new) — mounted in the `ItemRow` colSpan sub-row when `itemType === Travel`:
  - **method toggle** FlatMobilization / PerKm;
  - **PerKm** → `distanceKm` + `perKmRate` inputs that **drive** the line (`quantity = distanceKm`, `unitPrice = perKmRate`,
    `pricingMethod = PerKm`, `unitCode = KILOMETER` shown read-only) — the line's Qty/UnitPrice inputs are **disabled** on a Travel
    line so they can't diverge;
  - **FlatMobilization** → single fee (`quantity = 1`, `unitPrice = fee`, `pricingMethod = Fixed`, no km/rate/unit);
  - **origin** prefilled from the provider's profile city (editable), **destination** prefilled from the SR's `locationCityCode`
    (threaded from `RequestDetailPage`); tr/en labels;
  - **Save** persists the draft line first (`saveNow()`) then upserts the travel detail, so the server check sees up-to-date money;
    `SR_TRAVEL_*` shows inline. Travel stays Exempt — the economics panel is untouched.

### A2. Admin travel snapshot (`inktavia-marine-admin-web` + AdminPanel BFF + SR module read-path)

The admin operation-detail proxy passes the module `ServiceRequestDetailDto` through verbatim, so the snapshot rides along —
**no AdminPanel BFF change needed**. The data path (additive, no migration):
- `ServiceRequestOfferItemDto` gained a nullable `TravelPricingDetailDto? Travel`.
- `IServiceRequestRepository.GetTravelPricingByOfferItemIdsAsync` (+ impl) reads the `travel_pricing_details` rows by item id.
- `GetServiceRequestDetailQueryHandler` enriches Travel lines with their detail after mapping (resolved amount = the line's own
  `LineTotal`, since Travel is an Exempt pass-through).
- **admin-web:** `serviceRequest.types.ts` added `OfferItemTravelSnapshot` + `OfferItemRow`; `ServiceRequestDetailPage.tsx`
  `OffersModal` renders a read-only **Travel** block per Travel line (method, origin→dest city, `distanceKm × perKmRate`, resolved
  amount) next to the existing FX-snapshot block. tr/en (`offerTravel.*`).

---

## PART B — S5 part commercial terms

### B1. Admin `PartCommercialTerm` CRUD (`inktavia-marine-admin-web` + AdminPanel BFF) — cost visible HERE ONLY

**AdminPanel BFF:** mirrors the CustomerDiscount passthrough exactly.
- `IAdminPaymentBffRemoteCall` — 6 bare-result Refit methods over the Payment module
  `/api/v1/payment/part-commercial-term/rules{,/{id}{,/deactivate,/reactivate}}` (list/get/create/update/deactivate/reactivate).
- `AdminPayment/Dto/PartCommercialTermBffDtos.cs` (records, string enums) — carries the confidential cost (admin-only).
- `AdminPayment/PartCommercialTerm/{PartCommercialTermBffQueries,PartCommercialTermBffCommands}.cs` (CQRS handlers).
- `AdminPaymentController.cs` — 6 endpoints under `part-commercial-term/rules` (bare bool deactivate/reactivate wrapped in
  `PartCommercialTermDeactivateResult` so the outward envelope can carry it). Fail-envelope + auth are automatic.

**admin-web:** new page pair under `pages/app/payments/`, mirroring `CustomerDiscountRules{,Detail}Page`:
- `PartCommercialTermsPage.tsx` — list (scope via `SpecificityHint`, effective window, active version) + KPIs + **create** drawer;
  **cost fields (`supplierListPrice`, `providerDealerMargin`) are shown in an amber "confidential — admin-only" section here**.
- `PartCommercialTermDetailPage.tsx` — detail (a dedicated **Cost (Confidential)** card) + **edit** drawer (scope + version
  read-only/immutable) + deactivate/reactivate confirms; versioning = append a new version (create), not editing an active one.
- Client validation mirrors the backend: money ≥ 0; **Σ funded ≤ maximumDiscountableAmount**; maxCustomerDiscount ≤ maximumDiscountableAmount;
  effective ordering. Overlap → the server's `PartCommercialTermConflict` (5120) surfaces verbatim via the shared `RuleConflictBanner`
  (added 5120 to `RULE_CONFLICT_CODES`).
- Wiring: `paymentApi` (6 methods) + `endpoints.ts` + `queryKeys.payments.partCommercialTerm` + `usePartCommercialTermsQuery.ts`
  hooks + `payment.types.ts` DTOs; route consts (`routes.tsx`) + route objects (`routeObjects.tsx`) + a sidebar leaf next to
  CustomerDiscounts (`DashboardLayout.tsx`); `navigation.json` + `payments.json` (tr + en).

### B2. Provider cost-free allowance (`inktavia-marine-provider-web` + MarineProvider BFF) — NO cost

**MarineProvider BFF:** `GetOfferPartTermsPreview` Refit method (→ module `GET
/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/part-terms-preview`, wrapped
`ResolvePartLineAllowancesRemoteCallResponse`) + `Application/PartTerms/GetOfferPartTermsPreviewBff.cs` handler + a
`GetOfferPartTermsPreview` action on `OffersController`. The DTO is the cost-free `PartLineAllowanceDto`
(`{ lineRef, found, productCode, maxAllowedCustomerDiscount, funding{supplier/provider/platformFunded}, minimumProviderReceivable }`)
— **no cost field present** (module reflection test enforces it).

**Provider FE:** `api/partTermsApi.ts` + `hooks/useOfferPartAllowances.ts` (debounced, `lineRef → allowance` map, keyed by
persisted line id) + `components/PartAllowanceHint.tsx`. On a Product/Consumable line the `ItemRow` renders a small,
**non-blocking** hint: "max discount + funded split (platform / provider) + min. receivable" (tr/en, `allowance.*`).
**Informational only** — not fed into provisional totals; renders no cost/margin.

### B3. Admin part-line visibility (optional)

The admin already sees full cost detail via the B1 CRUD (the authoritative surface), so a redundant cost-free allowance strip on
the offer detail was intentionally **not** added — the admin offer detail instead shows the S4 Travel derivation (A2). This keeps
the offer detail light and avoids a second BFF preview call; noted as the deliberate optional-scope decision.

---

## Confidentiality assertion (the headline)

- **Grep (both provider surfaces → 0 hits):** `supplierListPrice|providerDealerMargin|dealerMargin|listPrice` across
  `inktavia-marine-provider-web/src` and `addesso-project/Bff/src/MarineProvider` → **nothing**.
- **Positive control:** cost fields appear **only** in admin-web (`payment.types.ts`, the two PartCommercialTerm pages, i18n) and
  the AdminPanel BFF (`PartCommercialTermBffDtos.cs`).
- **Codified FE test:** `inktavia-marine-provider-web/.../offer/costConfidentiality.test.ts` walks the whole offer feature and
  fails if any source references supplier cost / dealer margin — **passing**. Mirrors the module's reflection test
  `PartLineAllowanceConfidentialityTests`.

---

## Verification

**Automated — all green:**
- `dotnet build` clean (0 errors): SR module + repository, MarineProvider BFF, AdminPanel BFF.
- SR module unit tests: **92 passed** (the detail-handler enrichment is guarded by `travelItemIds.Count > 0`; no regressions).
- provider-web: `tsc --noEmit` clean, `eslint` clean on all changed files, i18n parity test passing, confidentiality test passing.
- admin-web: `tsc --noEmit` clean, `eslint` clean on all changed files, i18n parity test passing (payments / serviceRequests /
  navigation namespaces all at tr↔en parity).
- Repo-wide eslint shows only **pre-existing** errors in files this task did not touch (e.g. `shared/realtime/useProviderRealtime.tsx`,
  and the reference `CustomerDiscountRules{,Detail}Page` react-refresh violations); every file added/edited here is clean.

**Runtime (deployed + endpoint smoke tests):** the changed backend images were rebuilt and redeployed onto the running local
Docker stack — `service-request-api`, `bff-adminpanel`, and **both** `bff-marineprovider` replicas (the split-brain gotcha: each
replica builds its own image, so both were rebuilt + force-recreated). Every new route now resolves (auth-gated **401**, vs
**404** before deploy — proving the passthrough chains are wired and live end to end):
- SR module (`:7107`): `GET .../provider/offers/{o}/items/{i}/travel-pricing` → 401; `GET .../{sr}/offers/{o}/part-terms-preview` → 401.
- MarineProvider BFF (`:17002`): `GET /provider/offers/{o}/items/{i}/travel-pricing` → 401; `GET /provider/service-requests/{sr}/offers/{o}/part-terms-preview` → 401.
- AdminPanel BFF (`:17001`): `GET /admin-panel/payment/part-commercial-term/rules` → 401; `POST .../rules` → 401.
- Payment module (`:7102`): `GET /payment/part-commercial-term/rules` → 401.

**On-screen (UI flows) — remaining manual gate.** The four Verify flows below drive the browser through Keycloak login against the
now-deployed backend + the two web dev servers (`npm run dev` in each repo). All wiring, deployment, and confidentiality are already
proven above; this is the visual pass:
1. Provider S4 — Travel line: PerKm km×rate consistent with the line (KILOMETER shown), origin/dest prefilled; Flat fee mode;
   inconsistent input → `SR_TRAVEL_*`; economics unchanged.
2. Admin S4 — offer detail shows the travel derivation (method, cities, km, rate, amount) on an accepted offer.
3. Admin S5 — create a versioned `PartCommercialTerm` (cost + funded split + min-receivable); overlap + Σfunded>maxDiscountable
   rejected; cost visible here only.
4. Provider S5 — a part line shows the cost-free allowance hint; payload grep confirms no cost/margin; totals unchanged.

---

## Next SR phase

**S9 — line profit protection:** applies the S5 caps (max customer discount + `minimumProviderReceivable`) as enforcement at
offer submit/accept (S5 defines/resolves only; this FE surfaces them descriptively). The provider allowance hint becomes a hard
guardrail, and the admin PartCommercialTerm CRUD is the rule source it reads.
