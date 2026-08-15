# Marine.Web BFF — Blocked public projections (W4 backlog)

The website's marketing/SEO surface (W4 of the realignment) can only project data a module actually exposes on a
public read. This file is the **actionable backlog**: each blocked projection names the module, the exact route it
must expose, the web-safe fields it must return, and the auth posture — so a module team can build it later without
re-deriving the requirement. Until then the website renders an honest "not available yet" state.

**Rule (unchanged from the brief):** the BFF never invents a client for a non-existent module endpoint and never
fabricates data. A projection is either implemented over a real endpoint or listed here.

Fresh reachability re-verified on the current branch (Step 0). Verdicts: **REACHABLE** (implemented) / **PARTIAL**
(the real part implemented, the rest below) / **BLOCKED** (nothing reachable yet).

| # | Projection | Route (proposed web) | Verdict | Module endpoint found |
|---|---|---|---|---|
| 1 | Service catalogue | `GET api/v1/web/services` | ✅ REACHABLE | `GET /api/v1/reference-data/lookup-groups/lookup-items/SERVICE_PROVIDER_CATEGORY` (service-token role `reference_data_read`) |
| 2 | Service detail | `GET api/v1/web/services/{slug}` | ⛔ BLOCKED | none — lookup is Code-keyed with no body; rich per-service pages are editorial content, already served by `api/v1/web/content` |
| 3 | Location detail | `GET api/v1/web/locations/...` | 🟡 PARTIAL | `LocationController [AllowAnonymous]` country/city/district **by code** — implemented; region/marina/slug below |
| 4 | Service × location | `GET api/v1/web/service-pages/{serviceSlug}/{locationSlug}` | ⛔ BLOCKED | none — `ProviderCatalogController` is `[Authorize]`; no public availability read |
| 5 | Public pricing | `GET api/v1/web/pricing` | 🟡 PARTIAL | `ParticipantPlanController` + `ProviderPlanController` GETs are `[AllowAnonymous]` — implemented; commission/fee/VAT below |
| 6 | CargoDry catalogue + product | `GET api/v1/web/cargodry`, `.../{slug}` | ⛔ BLOCKED | none — `CargoDryPublicController` exposes only `POST validate` |
| 7 | Contact submit | `POST api/v1/web/contact` | ⛔ BLOCKED | none — no module owns anonymous inbound persistence + spam scoring + notification |

---

## 2 — Service detail `{slug}` — BLOCKED

**Why blocked.** The service taxonomy is the ReferenceData `SERVICE_PROVIDER_CATEGORY` lookup group. A lookup item is
`Code`-keyed and carries a single `Name`/`Description` — no slug, no body, no per-locale translation set. There is no
slugged "service entity" with editorial content anywhere in the modules.

**How the website copes today.** Rich per-service pages are the **editorial content layer** and are already served by
`api/v1/web/content` (a guide/announcement authored per service). The catalogue (#1) links to those.

**Unblock spec (only if a distinct structured service-detail entity is ever wanted, separate from editorial content):**
- Owner: a content-owning module (Content, or a new Catalog module).
- Route: `GET /api/v1/{module}/public/services/{slug}` — `[AllowAnonymous]` (in-cluster) **or** a service-token read role.
- Web-safe fields: `slug`, `code`, `name`, `shortDescription`, `body`, `availableLangs[]`, `relatedCategoryCodes[]`,
  `seoTitle`, `seoDescription`, `heroMediaKey?`. No provider data, no economics.
- Slug rules must reuse the reserved-slug guard (W3.4).

## 3 — Location detail — region / marina types, slug resolver, per-location services — BLOCKED (rest is PARTIAL)

**Implemented (real):** `country`, `city`, `district` detail **by hierarchical code** with `locationType` + resolved
`parentChain[]`, from `LocationController`'s `[AllowAnonymous]` reads.

**Still blocked:**
1. **`region` and `marina` location types.** The ReferenceData geography hierarchy is
   country → city → district → neighborhood only. The website's generic location model also wants `region` and
   `marina`.
   - Unblock: ReferenceData adds `region` (between country and city, or as a city grouping) and `marina` (a point of
     interest under city/district) as first-class geography, each with a public read
     `GET /api/v1/reference-data/locations/.../regions|marinas` returning `{ code, name, parentCodes, lat?, lon? }`.
2. **Single-slug resolver.** Locations are code-keyed and hierarchical; there is no `GET /locations/{slug}` that maps a
   flat slug to a location + its ancestors.
   - Unblock: ReferenceData adds a slug column per geography level (reusing the reserved-slug guard) and a flat
     resolver `GET /api/v1/reference-data/locations/by-slug/{slug}` → `{ locationType, code, name, parentChain[], slug }`.
     Then the BFF collapses its three code routes into `GET api/v1/web/locations/{slug}`.
3. **Single-district / single-neighborhood read.** Only a **districts-by-city list** exists (the BFF resolves a
   district by filtering it); neighborhoods are list-only.
   - Unblock: add `GET .../districts/{districtCode}` and `GET .../neighborhoods/{neighborhoodCode}` single reads.
4. **Available services per location.** The website wants "services available in this location"; no module exposes a
   public location→service availability read (see #4).

## 4 — Service × location landing — BLOCKED

**Why blocked.** The aggregated landing needs a **coarse availability signal** (`available` / `limited` / `none` —
never a raw provider count) for a (service, location) pair. The provider catalogue lives on
`ProviderCatalogController` (`api/v1/service-requests/provider/catalog`), which is `[Authorize]` — provider-only,
never public — and it exposes per-provider rows, not an aggregate signal.

**Unblock spec:**
- Owner: ServiceRequest (reads the provider eligibility/coverage read-model, e.g. the I2 `GetProvidersForArea`
  projection) with input from Profile.
- Route: `GET /api/v1/service-requests/public/availability?serviceCode={c}&locationCode={l}` — `[AllowAnonymous]`
  (in-cluster) or a service-token read role.
- **Web-safe response — coarse only:** `{ serviceCode, locationCode, availability: "available"|"limited"|"none" }`.
  The endpoint must bucket internally and **never** return a provider count, provider ids, names, scores, or ranking.
- Optional (commercially approved only): `priceRange: { min, max, currency }` derived from published terms — omit
  entirely unless approved.
- The BFF then aggregates service summary (#1) + location summary (#3) + this signal into one
  `GET api/v1/web/service-pages/{serviceSlug}/{locationSlug}` projection (one call from the page, never four).

## 5 — Public pricing — commission model, customer platform fee, VAT flag — BLOCKED (plans are PARTIAL)

**Implemented (real):** published **subscription tiers** (provider + participant plans) via the two
`[AllowAnonymous]` plan GETs — `planCode`, `name`, `description`, `monthlyPriceTRY`, `annualPriceTRY?`, `badgeLabel`,
`trialDays?`, `features[]`, `sortOrder`, `effectiveFrom`, `currency: TRY`.

**Still blocked** — everything else the pricing projection wants is on **admin-only** controllers:
- **Commission model** — `CommissionRuleController` is `[Authorize(Roles = "Admin")]`.
- **Customer platform fee** — `PlatformFeeRuleController` is `[Authorize(Roles = "Admin")]`.
- **VAT flag / effective-from for commercial terms** — no anonymous read.

**Unblock spec:**
- Owner: Payment.
- Route: `GET /api/v1/payment/public/pricing-terms` — `[AllowAnonymous]` (in-cluster) or a service-token read role.
- **Web-safe, published commercial terms ONLY:**
  `{ commissionModel: { label, ratePercent }, customerPlatformFee: { amount|percent, currency }, vatIncluded: bool,
  currency, effectiveFrom }`.
- **Must NOT expose:** cost-share rates, tevkifat/withholding mechanics, profit-protection internals, per-provider
  commission benefits, or any economics snapshot. Publish only the headline figures a customer is quoted.
- The BFF folds these into the existing `api/v1/web/pricing` response alongside the plan tiers.

## 6 — CargoDry catalogue + product — BLOCKED

**Why blocked.** `CargoDryPublicController` (`api/v1/cargodry/public`) exposes only `POST validate`. There is no
public catalogue or product read. The website emits `Product`/`Offer` structured data, so anything here must be
commercially approved for public display.

**Unblock spec:**
- Owner: CargoDry.
- Routes: `GET /api/v1/cargodry/public/catalogue`, `GET /api/v1/cargodry/public/products/{slug}` — `[AllowAnonymous]`.
- Web-safe fields: `slug`, `name`, `shortDescription`, `specs` (approved only), `mediaKeys[]`, `availableLangs[]`,
  `seoTitle`, `seoDescription`. `price` / `availability` **only** when a product is flagged commercially-approved for
  public display — otherwise omit (the website renders without an `Offer`).
- Reserved-slug guard (W3.4) applies to product slugs.

## 7 — Contact submit — BLOCKED

**Why blocked.** No module owns anonymous inbound contact: persistence + spam scoring + notification/routing to a
support inbox. The BFF must **not** stand up its own store (it has no database — a core constraint).

**Unblock spec:**
- Owner: Notification (or a Support module).
- Route: `POST /api/v1/{module}/public/contact` — `[AllowAnonymous]`, **stricter rate limit than reads**.
- Request (validated shape only — the Next.js Server Action forwards it and must never be trusted):
  `{ name, email, subject, message, sourcePage?, captchaToken? }`.
- Server-side responsibilities: shape + server validation, spam scoring, persistence, and notification to the support
  inbox. Returns `{ accepted: bool, ticketRef? }`.
- The BFF then adds `POST api/v1/web/contact` on a dedicated `contact-submit` rate-limit policy (stricter than
  `public-read-ip`), thin → CQRS → the module command.
