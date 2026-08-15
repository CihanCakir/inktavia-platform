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

## Locations — `api/v1/web/locations` 🟡 EXISTING (partial, W4)

Code-keyed hierarchy only (country → city → district). No single-slug resolver and no `region`/`marina` types yet
(⛔ below).
| Verb | Route | Returns |
|---|---|---|
| GET | `/countries/{countryCode}` | location detail (country) |
| GET | `/countries/{countryCode}/cities/{cityCode}` | location detail (city) |
| GET | `/countries/{countryCode}/cities/{cityCode}/districts/{districtCode}` | location detail (district) |

**Detail shape:** `{ locationType: "country"|"city"|"district", code, name, parentChain[{ locationType, code, name }],
latitude?, longitude?, isCoastal? }`.
**Withheld:** database ids. **Honesty note:** if a parent record can't be resolved, its `parentChain` entry uses the
real **code** as the display name — never a fabricated name.

## Pricing — `api/v1/web/pricing` 🟡 EXISTING (partial, W4)

### `GET /` — published subscription tiers
**Returned:** `{ currency: "TRY", providerPlans[], participantPlans[] }`, each plan:
`{ audience: "provider"|"participant", planCode, name, description, monthlyPriceTRY, annualPriceTRY?, badgeLabel?,
trialDays?, features[], sortOrder, effectiveFrom? }`.
**Withheld:** database `id` (`planCode` is the public key), per-viewer `isCurrent`, `isActive`, and discount-rate
mechanics (`serviceDiscountRate`/`cargoDryDiscountRate`/`inkCoinEarnMultiplier`) / `maxActiveOffers` (benefits are
conveyed via the authored `features[]`, not raw rates).
**Partial:** commission model, customer platform fee and the VAT flag are admin-only in Payment → ⛔ below.

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
| ⛔ | Location single-slug resolver + `region`/`marina` types | `GET api/v1/web/locations/{slug}` | ReferenceData has no per-level slug, no flat resolver, no region/marina (spec §3) |
| ⛔ | Service × location landing | `GET api/v1/web/service-pages/{serviceSlug}/{locationSlug}` | no public **coarse** availability read (`ProviderCatalog` is `[Authorize]`) (spec §4) |
| ⛔ | Full pricing terms (commission / platform fee / VAT) | folds into `GET api/v1/web/pricing` | those reads are admin-only in Payment (spec §5) |
| ⛔ | CargoDry catalogue + product | `GET api/v1/web/cargodry`, `.../{slug}` | `CargoDryPublicController` exposes only `POST validate` (spec §6) |
| ⛔ | Contact submit | `POST api/v1/web/contact` | no module owns anonymous persistence + spam scoring + notification (spec §7) |

**PROPOSED:** none outstanding — every designed website route is either EXISTING above or BLOCKED here. When a module
ships one of the blocked endpoints, the BFF adds the projection over it (same rules: anonymous, cached, field-stripped,
no fabricated data) and it graduates to EXISTING in this document.
