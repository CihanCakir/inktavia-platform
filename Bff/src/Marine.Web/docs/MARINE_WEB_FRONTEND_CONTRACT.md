# Marine.Web BFF — Frontend Integration Contract

**For:** the `inktavia-marine-web` team (server-rendered Next.js 16 at inktavia.com).
**What this is:** every website-facing endpoint the BFF offers, marked **EXISTING** (live now), **PROPOSED** (designed,
not yet built), or **BLOCKED** (waiting on a module endpoint). For each: route, verb, the fields returned, and the
fields **deliberately withheld** (the field-stripping guarantees). BLOCKED items link to their unblock spec in
[`MARINE_WEB_BLOCKED.md`](../../../docs/MARINE_WEB_BLOCKED.md).

Status legend: ✅ EXISTING · 🟡 EXISTING (partial — the rest is BLOCKED) · 🔵 PROPOSED · ⛔ BLOCKED.

---

## How to call the BFF (all public reads)

- **Anonymous, server-to-server.** No user auth — the website never authenticates anyone.
- **Send `X-Aizen-Web-Caller: <secret>`** on every request from the Next.js server. A valid secret puts you on the
  high/unlimited trusted rate-limit tier; without it you share the strict per-IP browser limit and **will** be
  throttled at modest traffic. The secret is server-side only — never ship it to the browser.
- **Envelope:** every response is `AizenApiResponse<T>` — read `.body`. A clean business error (404/unavailable)
  comes back as the enveloped error, never a stack trace or a raw module status.
- **Caching:** every read sends `Cache-Control: public, max-age=…, stale-while-revalidate=…` and a weak `ETag`.
  Use them for ISR; send `If-None-Match` for a cheap `304`. Freshness per resource is in the README table.
- **Revalidation:** the BFF POSTs your `Revalidate:Url` with `{ entityType, id, slug, lang, changeKind }` when content
  changes, so you can revalidate a tag instead of polling.
- **Indexability:** trust `Seo.Indexable` as the backend verdict. When a projection has no `Seo` block, **fail safe to
  `noindex`**. For the sitemap, use the slug feed's per-row `indexable`.
- **hreflang:** emit `<link rel="alternate" hreflang>` for exactly the codes in `availableLangs` — nothing more.

---

## Content — `api/v1/web/content` ✅ EXISTING

### `GET /feed?lang&page&pageSize` · `GET /by-type/{type}?lang&page&pageSize`
Paged `{ items[], page, pageSize, total }`. Each item (content summary):
**Returned:** `id`, `type`, `slug`, `lang`, `availableLangs[]`, `title`, `summary`, `coverUrl`, `categorySlug`,
`tags[]`, `commentCount`, `favoriteCount`, `publishAt`, `publishedAt`.
**Note:** items on this surface are always Published (the BFF pins the `MarineOsWeb` surface; the module enforces
visibility/expiry). **Withheld:** body, the full multi-language translation set, targeting/audience, author ids,
moderation, placements.

### `GET /items/{slug}?lang` — content detail
**Returned:** `id`, `type`, `slug`, `lang`, `title`, `summary`, `body`, `seoTitle`, `seoDescription`, `coverUrl`,
`gallery[] { url, alt, kind, position }`, `availableLangs[]`,
`seo { indexable, reason, title, description }`, `tags[]`, `categorySlug`, `campaign?`, `releaseNote?`,
`commentCount`, `favoriteCount`, `publishedAt`.
**Withheld (field-stripped):** `authorUserId`, `placements`, `audience` (targeting), `status`, `expireAt`,
`updatedAt`, the **full `translations` set** (one language is resolved), and media `fileStorageId`.

### `GET /categories?lang`
**Returned:** `[{ id, slug, name{lang→text}, parentSlug, position, isActive }]` (clean taxonomy; `slug` is the key).

### `GET /items/{id}/comments?page&pageSize` — approved public comments
**Returned:** `{ items[{ id, authorDisplayName, body, parentCommentId, createdAt }], page, pageSize, total }`.
**Withheld:** author ids (`authorUserId`/`authorProfileId`), `status`, moderation trail
(`moderatedByUserId`/`moderatedAt`/`lastModerationReason`).

## Reference — `api/v1/web/reference` ✅ EXISTING
| Verb | Route | Returns | Notes |
|---|---|---|---|
| GET | `/countries` | `[{ countryCode, numericCode, name, defaultCurrencyCode, phoneCode, isActive }]` | public geography |
| GET | `/countries/{countryCode}/cities` | `[{ countryCode, cityCode, name, latitude?, longitude?, isCoastalCity, isActive }]` | 404 → "country not found" |
| GET | `/lookups/{groupCode}` | raw lookup items for a group (e.g. `VESSEL_TYPE`) | generic form/dropdown data; for the **marketing** service grid use `web/services` (stripped) below |

## SEO — `api/v1/web/seo` ✅ EXISTING

### `GET /slugs/{entityType}?lang` — sitemap / static-generation feed
`entityType = content` only today (others → clean error, never faked).
**Returned:** `[{ slug, availableLangs[], lastModified, indexable }]`.
**Caveat:** `lastModified` is the item's **PublishedAt**, not a true modified timestamp — a later edit that does not
republish will not move it. Fine for sitemap `<lastmod>`.

## Services — `api/v1/web/services` ✅ EXISTING (W4)

### `GET /` — service-category catalogue grid
**Returned:** `[{ code, slug, name, description, iconKey, colorCode, sortOrder }]` (`slug` derived deterministically
from `code`).
**Withheld:** `id`, `lookupGroupId`, `groupType`, `groupHierarchyPath`, `isDefault`, `isActive`.
**No `seo`/`availableLangs`** — this is a taxonomy grid (a lookup item has no body or translation set). Rich,
indexable per-service pages are **editorial content** under `web/content`, not here.

## Locations — `api/v1/web/locations` ✅ EXISTING (W4 + M3 slug resolver)

| Verb | Route | Returns |
|---|---|---|
| GET | `/{slug}` | **M3 collapsed lookup** — location by flat slug (identity + parent chain) |
| GET | `/countries/{countryCode}` | location detail (country) — coordinate-rich |
| GET | `/countries/{countryCode}/cities/{cityCode}` | location detail (city) — coordinate-rich |
| GET | `/countries/{countryCode}/cities/{cityCode}/districts/{districtCode}` | location detail (district) — coordinate-rich |

**Prefer `/{slug}`** for identity + breadcrumb (the SEO/URL path). The code-keyed routes are kept for
coordinate-rich detail (the slug route returns `latitude/longitude/isCoastal` as null — the module resolver is
minimal). Slugs are deterministic, globally unique, Turkish-aware, and parent-qualified for lower levels
(`istanbul-kadikoy`).
**Detail shape:** `{ locationType: "country"|"city"|"district"|"neighborhood", code, name,
parentChain[{ locationType, code, name }], latitude?, longitude?, isCoastal? }`.
**Withheld:** database ids. **Honesty note:** if a parent record can't be resolved, its `parentChain` entry uses the
real **code** as the display name — never a fabricated name.
**Still ⛔:** `region`/`marina` location types — a data-sourcing project (no dataset yet), see `MARINE_WEB_BLOCKED.md` §3.

## Pricing — `api/v1/web/pricing` ✅ EXISTING (W4 plans + M1 terms)

### `GET /` — published subscription tiers **and** published commercial terms
**Returned:** `{ currency: "TRY", providerPlans[], participantPlans[], terms? }`.

Each plan: `{ audience: "provider"|"participant", planCode, name, description, monthlyPriceTRY, annualPriceTRY?,
badgeLabel?, trialDays?, features[], sortOrder, effectiveFrom? }`.
**Plan withheld:** database `id` (`planCode` is the public key), per-viewer `isCurrent`, `isActive`, and discount-rate
mechanics (`serviceDiscountRate`/`cargoDryDiscountRate`/`inkCoinEarnMultiplier`) / `maxActiveOffers` (benefits are
conveyed via the authored `features[]`, not raw rates).

`terms` (M1, from Payment's Global published defaults — null if the terms read is unavailable):
```jsonc
{
  "commission": {                       // provider-facing standard headline
    "audience": "provider",
    "label": "Standard marketplace commission",
    "standardRatePercent": 15.0,        // Global + Standard-priority + effective-now rule; null if none — never faked
    "note": "Standard platform commission; individual rates may vary by plan, category or agreement."
  },
  "customerPlatformFee": {              // customer-facing Global fee headline; null if none resolves
    "model": "PercentageWithBounds",    // Percentage | Fixed | PercentageWithBounds | Waived
    "ratePercent": 2.5,                 // Percentage / PercentageWithBounds only
    "minAmount": 99, "maxAmount": 1500, // PercentageWithBounds only
    "currency": "TRY"
  },
  "effectiveFrom": "2026-01-01T00:00:00Z"  // max EffectiveFrom of the two source Global rules
}
```
**Terms withheld (never surfaced):** cost-share rates, tevkifat/withholding mechanics, profit-protection internals,
economics/commission-allocation snapshots, per-provider commission benefits/overrides, category/customer-type
override rows, rule `notes`/`ruleCode`/user ids/DB ids. **VAT is intentionally omitted entirely** (no `vatIncluded`,
no rate) — its treatment is not a published headline. The commission figure is the Global **Standard** default only —
never the Emergency surge or any negotiated per-provider rate.

## Service × location — `api/v1/web/service-pages` ✅ EXISTING (M2 + M3 slug form)

### `GET /{serviceSlug}/{locationSlug}` (M3) — or `GET /{serviceSlug}?cityCode={cityCode}` (M2 interim)
Prefer the **`/{serviceSlug}/{locationSlug}`** slug form: `locationSlug` is resolved to its city via the location
resolver (a district/neighborhood uses its parent city — availability is **city-keyed**). The interim `?cityCode=`
form still works.
**Returned:** `{ service: {...}, location: { cityCode }, availability }`.
- `service` — the catalogue tile (same shape as `web/services`): `{ code, slug, name, description, iconKey, colorCode, sortOrder }`.
- `location` — **interim: `{ cityCode }` only** (the availability read-model is city-keyed). Upgrades to the full
  location detail when M3 ships the location-slug resolver.
- `availability` — the **coarse** signal `"available" | "limited" | "none"`.

**Withheld (never surfaced):** provider count, provider ids/user ids, names, scores, ranking. The count is bucketed
inside Identity and never crosses the module boundary; the BFF only ever sees/returns the enum.
**Unknown `serviceSlug`** ⇒ clean not-found. **Availability fails safe to `none`** if the signal is momentarily
unavailable (never over-states).
**Interim → full:** the `{serviceSlug}/{locationSlug}` slug form and district/marina granularity are pending M3 (the
read-model is city-keyed) — see `MARINE_WEB_BLOCKED.md` §4.

## Contact submit — `api/v1/web/contact` ✅ EXISTING (M4) — the only WRITE surface

### `POST /` — anonymous contact form (stricter `contact-submit` rate limit)
This is a **WRITE**, on a **tighter** limit than the reads (a few submits/min per IP). Forward from your Server
Action; validate **shape only** — the BFF and Notification module never trust the payload.
**Request:** `{ name, email, subject, message, sourcePage?, captchaToken? }` — **plus a hidden honeypot field** a real
user leaves empty (a filled honeypot is silently treated as spam). Do **not** send the visitor's IP — the BFF forwards
it as `X-Forwarded-For` for the module to hash.
**Response:** `{ accepted: bool, ticketRef?: string }` — `accepted` is **always true** on a successful call (a spammy
message is silently accepted so bots learn nothing); `ticketRef` is an opaque handle (never a database id).
**Withheld:** everything else — spam score, IP hash, status, DB id. **Captcha** is accepted-and-ignored until a
provider (Turnstile/hCaptcha) is wired server-side.
**Ownership note:** folded into the Notification module as the interim owner; a future Support module could take it
over without changing this contract.

## Participant engagement — `api/v1/web/me/content/*` — FROZEN, NOT IN THIS CONTRACT
Built, tested, **unconsumed**. The website has no auth and never calls it. Excluded from this contract; kept for a
future authenticated customer app. Do not integrate.

---

## BLOCKED — waiting on a module endpoint (do not integrate; render "not available yet")

Each links to its unblock spec (owner module, exact route, web-safe fields) in
[`MARINE_WEB_BLOCKED.md`](../../../docs/MARINE_WEB_BLOCKED.md).

| Status | Projection | Intended route | Blocked because |
|---|---|---|---|
| ⛔ | Service detail (rich, slugged) | `GET api/v1/web/services/{slug}` | lookup is code-keyed, no body — rich pages live in `web/content` (spec §2) |
| ⛔ | Location `region` / `marina` types | (part of `api/v1/web/locations`) | slug resolver + single reads are **EXISTING** (M3); `region`/`marina` need a sourced dataset — a data-sourcing project (spec §3) |
| 🟡 | Service × location — **district/marina-level** availability | `GET api/v1/web/service-pages/{serviceSlug}/{locationSlug}` | slug form is **EXISTING** (M3); availability stays **city-keyed** — finer granularity needs a district/marina read-model (spec §4) |
| ⛔ | CargoDry catalogue + product | `GET api/v1/web/cargodry`, `.../{slug}` | `CargoDryPublicController` exposes only `POST validate` (spec §6) |

**PROPOSED:** none outstanding — every designed website route is either EXISTING above or BLOCKED here. When a module
ships one of the blocked endpoints, the BFF adds the projection over it (same rules: anonymous, cached, field-stripped,
no fabricated data) and it graduates to EXISTING in this document.
