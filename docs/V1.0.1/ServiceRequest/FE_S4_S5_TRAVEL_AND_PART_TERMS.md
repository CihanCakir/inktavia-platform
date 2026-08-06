# FE_S4_S5 — travel-line pricing UI (provider + admin) & part commercial terms (admin CRUD + provider allowance)

> **Repos:** `inktavia-marine-provider-web`, `inktavia-marine-admin-web`, and the two BFFs in
> `addesso-project/Bff/src/{AdminPanel,MarineProvider}`. Surfaces the two backend phases just shipped: **S4** (structured
> travel/mobilization pricing on the Travel line + acceptance snapshot) and **S5** (part commercial terms — dealer margin /
> funded splits / min-receivable, **cost-confidential**). All **additive**; server owns every total/allowance. tr + en for
> every new string.

## Headline constraint — cost confidentiality carries to the FE (S5, §20.9)
The provider's real cost (`supplierListPrice`, `providerDealerMargin`) is **Payment-internal** and never crosses to SR.
On the FE this means: **no provider-facing or customer-facing screen ever renders cost/margin.** The provider offer
builder shows **only** the cost-free allowance (`maxAllowedCustomerDiscount`, funded split, `minimumProviderReceivable`).
**Only the admin PartCommercialTerm CRUD page** (admin-scoped, admin BFF) shows the cost fields — because the admin
manages them. If a cost field appears on any provider/customer payload, that's a bug.

## Ground rules (respect existing conventions — live in the repos)
- Admin CRUD mirrors the existing Payment-rule pages (`pages/app/payments/CustomerDiscountRulesPage.tsx` +
  `CustomerDiscountRuleDetailPage.tsx`, the `commissions`/`payments` features) — same list/detail/drawer, TanStack Query,
  shared table/`Button`/`AlertBanner`, `queryKeys`, lucide, i18n. The `pricing-attributes` admin feature (from S2 FE) is
  the closest recent precedent.
- Provider follows `features/service-requests/offer/*` (`OfferBuilder`, `useOfferDraft`, `offerApi`, `offerTypes.ts`); the
  S2 attribute picker + S3 FX display just added there are the pattern to extend.
- **BFF passthrough first, then FE.** Add typed, envelope-correct passthrough (concrete DTOs; wrapped modules →
  `AizenApiResponse<T>`) before wiring screens. Additive DTO fields only. tr+en; typecheck + lint clean.

---

## PART A — S4 travel-line pricing UI

### A1. Provider: travel picker on the Travel line (`OfferBuilder` + MarineProvider BFF)
**BFF:** passthrough for the module's provider `PUT/GET .../travel-pricing` (set/get the `TravelPricingDetail` for a Travel
line). Typed DTO: `method (FlatMobilization | PerKm)`, `originCityCode?`, `destinationCityCode?`, `distanceKm?`,
`perKmRate?`, `unitCode`.
**FE:** when a line's `itemType === Travel`, show a **travel detail** sub-form:
- **method** toggle FlatMobilization / PerKm.
- **PerKm** → `distanceKm` + `perKmRate` inputs; keep them **in sync with the line** (`Quantity = distanceKm`,
  `UnitPrice = perKmRate`, `PricingMethod = PerKm`) so the server-side consistency check (S4a) passes — ideally derive the
  line's quantity/unitPrice from these fields so they can't diverge; unit is **KILOMETER** (from R3, shown read-only).
- **FlatMobilization** → a single fee (`Quantity = 1`, `UnitPrice = fee`, `PricingMethod = Fixed`); no km/rate fields.
- **origin / destination city:** prefill origin (provider) + destination (vessel) from the existing I2 + vessel read-models
  (display + city-code); the provider may adjust origin among their cities. City labels tr/en.
- Surface the server's `SR_TRAVEL_*` rejection inline (e.g. inconsistent km/quantity). Travel is an Exempt pass-through —
  it does **not** change commission; the economics panel is unchanged.

### A2. Admin: travel snapshot visibility (`inktavia-marine-admin-web` + AdminPanel BFF)
- Extend the admin offer/operation-detail passthrough (additive) with the travel snapshot per Travel line: method,
  origin/destination city labels, distanceKm, perKmRate, resolved amount.
- Render a small read-only **Travel** block on the admin SR offer view so an operator sees how the mobilization cost was
  derived on an accepted offer.

---

## PART B — S5 part commercial terms

### B1. Admin: `PartCommercialTerm` CRUD (`inktavia-marine-admin-web` + AdminPanel BFF) — admin-only, cost visible here
**BFF:** passthrough over the Payment admin CRUD (list/get/create/new-version/deactivate) for `PartCommercialTerm`. This is
the **only** surface that carries the cost fields (admin-scoped).
**FE:** a new admin page under `pages/app/payments/` (route + nav leaf next to CustomerDiscountRules), mirroring
`CustomerDiscountRulesPage` + detail:
- List by scope (brand / productCode / provider / category) + effective window + active version.
- Create/edit drawer: `supplierListPrice`, `providerDealerMargin`, `maxCustomerDiscount`, the three funded amounts
  (supplier/provider/platform), `minimumProviderReceivable`, `maximumDiscountableAmount`, effective `from`/`to`, scope.
  Client validation mirrors backend (Σ funded ≤ maxDiscountable; money ≥ 0; overlap rejected → surface the server's
  `PART_TERM_*` / conflict envelope verbatim). Versioning = append a new version (don't edit an active one), matching the
  ProviderPlanPrice/CustomerDiscountRule UX.

### B2. Provider: cost-free part allowance on part lines (`OfferBuilder` + MarineProvider BFF) — NO cost
**BFF:** passthrough for the SR part-line allowance preview (compute-on-demand) returning the cost-free
`PartLineAllowanceDto { productCode, maxAllowedCustomerDiscount, funding { supplierFunded, providerFunded, platformFunded },
minimumProviderReceivable }` per part line. **Assert no cost field is present.**
**FE:** when a line is `Product` / `Consumable`, show a small **allowance** hint (secondary, non-blocking): "Bu parçada
uygulanabilir maks. indirim: ₺X · fon: platform ₺a / provider ₺b · min. hak ediş: ₺Y" (tr/en). It's **informational** for
S5 (the actual application/enforcement is S9) — do **not** feed it into the provisional totals; do **not** render any cost
or margin.

### B3. Admin: part-terms visibility on the offer detail (optional, additive)
- If a part line's resolved term is relevant on the admin offer view, show the same cost-free allowance (admin already has
  the CRUD for full detail) — keep it light; no new economics.

---

## Don't-break / QA
- Additive across both FEs + both BFFs: provider travel picker + part allowance hint + admin PartCommercialTerm CRUD +
  admin travel/part visibility + the passthrough DTOs. Existing offer builder, economics panel, admin pages unchanged for
  the no-Travel / no-part / TRY case. Server owns totals/allowances — the FE computes no money.
- **Confidentiality:** a test/assertion that no provider- or customer-facing payload carries `supplierListPrice` /
  `providerDealerMargin` / cost. Only the admin CRUD shows cost.
- tr + en for every new string; typecheck + lint clean both repos; BFF builds clean; DTOs concrete + envelope-correct.

## Verify (on screen)
1. **Provider S4:** on a Travel line, PerKm mode drives km × rate consistent with the line (KILOMETER shown), origin/dest
   cities prefilled; a Flat fee mode works; inconsistent input shows `SR_TRAVEL_*`; economics unchanged.
2. **Admin S4:** the admin offer detail shows the travel derivation (method, cities, km, rate, amount) on an accepted offer.
3. **Admin S5:** create a versioned `PartCommercialTerm` for a brand/product (cost + funded split + minReceivable);
   overlap + Σfunded>maxDiscountable rejected; cost fields visible **here only**.
4. **Provider S5:** a part line shows the cost-free allowance hint (max discount + funded split + min-receivable) — **grep
   the payload: no cost/margin anywhere**; totals unchanged.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FE_S4_S5.md`: the provider travel picker + admin travel visibility (S4), the admin
PartCommercialTerm CRUD + provider cost-free allowance (S5, with the confidentiality assertion), the BFF passthrough added
each side, i18n keys, and on-screen verification. Then next SR phase (S9 line profit protection — applies the S5 caps).
