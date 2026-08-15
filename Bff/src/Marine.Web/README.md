# Aizen.Bff.Marine.Web

The Backend-for-Frontend for the **public Inktavia website** — the **server-rendered Next.js site
`inktavia-marine-web` at inktavia.com**, NOT a browser SPA. A thin aggregation/adaptation layer over the platform
modules — **no database, no business logic**. It fetches from module APIs over Refit, reshapes module DTOs into
web-safe DTOs (stripping internal fields), pins the content surface, and enforces the anonymous-vs-authenticated
boundary.

Mirrors the sibling `Bff/src/Marine.Participant.Mobile` conventions (CQRS, Refit remote clients, identity-assertion
handler).

> **Consumer reality.** The public read surface is consumed **server-to-server**: the Next.js server makes every
> request for every visitor from a handful of IPs, renders HTML that Google indexes, and caches via ISR. It is not a
> per-visitor browser client. Everything below (rate-limit tiering, caching contract, revalidation webhook) follows
> from that.

---

## Auth model

**The website has NO authentication and never authenticates anyone** (a locked frontend decision). The public read
surface is anonymous and consumed server-to-server; there is no OIDC/PKCE login flow for it.

- **Public reads (the website's actual surface):** anonymous. Trust is by network placement (the cluster
  NetworkPolicy admits only the BFFs to the modules) plus the **trusted server-caller** rate-limit tier below.
- **Trusted server-caller (W2.1):** the Next.js server presents header **`X-Aizen-Web-Caller`** carrying
  `MarineWebPublic:TrustedCallerSecret` (server-side secret only — **never sent by a browser**). A valid secret moves
  the request onto a separate, much-higher shared rate-limit partition; everyone else stays on the strict per-IP
  `public-read-ip` limit. Mirrors the ecosystem's `X-Aizen-Bff-Assertion` shared-secret style (constant-time compare).
- **`/me` engagement surface — BUILT, UNCONSUMED, FROZEN.** `api/v1/web/me/content/*` is authenticated
  (Keycloak JWT resource server; identity resolved by subject and asserted to Content via `X-Aizen-Bff-Assertion` +
  `X-Aizen-User-Id`). The website never calls it. It is kept — tested, working — for a future authenticated customer
  app, and is **excluded from the website's integration contract**. See "Frozen surface" below.
- **CORS:** the config (`Cors:AllowedOrigins`, `AllowCredentials`, no wildcard) is retained for **local browser
  development only**. Server-to-server calls from the Next.js server do not exercise CORS (it is a browser
  same-origin/pre-flight mechanism), so it is not load-bearing in production.

---

## Freshness contract (resource → staleness tolerance)

Every public read emits `Cache-Control: public, max-age=<m>, stale-while-revalidate=<s>` plus a weak `ETag`
(conditional `If-None-Match` → `304`). These are the TTLs the frontend must not invent; the numbers are defined once
in `WebCacheAttribute` and mirrored here.

| Resource | Route | max-age | stale-while-revalidate | Rationale |
|---|---|--:|--:|---|
| Reference data | `web/reference/*` | 3600s | 86400s | countries/cities/lookups change rarely |
| Content categories | `web/content/categories` | 1800s | 86400s | taxonomy changes rarely |
| Content detail | `web/content/items/{slug}` | 300s | 3600s | editorial detail, moderate churn |
| Content feed / by-type | `web/content/feed`, `.../by-type/{type}` | 60s | 300s | list surface changes more often |
| Approved comments | `web/content/items/{id}/comments` | 30s | 120s | most volatile |
| SEO slug feed | `web/seo/slugs/{entityType}` | 300s | 3600s | sitemap tolerates lag |
| Service catalogue | `web/services` | 3600s | 86400s | taxonomy changes rarely |
| Location detail | `web/locations/**` | 3600s | 86400s | reference geography changes rarely |
| Public pricing | `web/pricing` | 1800s | 86400s | published plan terms change rarely |

**Change notifications (push, not poll).** This BFF is also a **bus consumer** (`AppType.Worker` in the host
`TypeInclude`): it subscribes to the Content module's `ContentPublishedMessage` / `ContentUnpublishedMessage` and
POSTs a best-effort revalidation webhook to `MarineWebPublic:Revalidate:Url` with payload
`{ entityType, id, slug, lang, changeKind }` (`lang` is null — the events are language-agnostic; the site
revalidates the item's tag across every locale). A webhook failure never breaks the consume. The Content module is
**not modified** — only its published event contracts are consumed via `Content.Abstraction`.

---

## Endpoint map

### Public content — `api/v1/web/content` · `[AllowAnonymous]` · rate-limited `public-read-ip`
| Method | Route | Purpose |
|---|---|---|
| GET | `feed` | MarineOsWeb feed (lang, page, pageSize) |
| GET | `by-type/{type}` | Feed narrowed to a `ContentType` |
| GET | `items/{slug}` | Localized public detail (404 → clean "not found") |
| GET | `categories` | Category list |
| GET | `items/{id}/comments` | Approved public comments (paged) |

### Participant engagement — `api/v1/web/me/content` · `[Authorize(WebAuthenticated)]` · **FROZEN, not in the website contract**
> Built, unconsumed, frozen pending a customer app — the website has no auth and never calls these. Kept, not deleted.

| Method | Route | Purpose |
|---|---|---|
| POST | `items/{id}/comments` | Add a comment (author from token, never body) |
| POST | `items/{id}/favorite` | Favorite an item |
| DELETE | `items/{id}/favorite` | Un-favorite |
| GET | `favorites` | My favorites (paged) |
| GET | `items/{id}/my-comments` | My own comments (keeps own moderation status) |

### Public reference — `api/v1/web/reference` · `[AllowAnonymous]` · rate-limited `public-read-ip`
| Method | Route | Purpose |
|---|---|---|
| GET | `countries` | Country reference |
| GET | `countries/{countryCode}/cities` | Cities of a country (404 → "country not found") |
| GET | `lookups/{groupCode}` | Lookup options for a group (e.g. `VESSEL_TYPE`) |

### Public SEO — `api/v1/web/seo` · `[AllowAnonymous]` · rate-limited `public-read-ip` · trusted-caller aware
| Method | Route | Purpose |
|---|---|---|
| GET | `slugs/{entityType}` | Slug feed backing the sitemap / static generation (`entityType=content` only today). Returns `[{ slug, availableLangs[], lastModified, indexable }]` |

### Public marketing projections (W4) — `[AllowAnonymous]` · cached · rate-limited `public-read-ip`
Only projections whose owning module exposes a genuinely public read are implemented. Everything else is reported in
[`docs/MARINE_WEB_BLOCKED.md`](../../docs/MARINE_WEB_BLOCKED.md) with a concrete unblock spec — never faked.

| Method | Route | Status | Source | Notes |
|---|---|---|---|---|
| GET | `web/services` | ✅ implemented | ReferenceData `SERVICE_PROVIDER_CATEGORY` lookup group | Taxonomy grid: `code`, `slug` (derived), `name`, `description`, `iconKey`, `colorCode`, `sortOrder`. No `Seo`/`availableLangs` — a lookup item has no body or translation set (rich per-service pages are editorial content under `web/content`). |
| GET | `web/locations/countries/{countryCode}` | 🟡 partial | ReferenceData `LocationController` | `locationType` + `parentChain[]`, code-keyed. |
| GET | `web/locations/countries/{countryCode}/cities/{cityCode}` | 🟡 partial | ReferenceData `LocationController` | City detail + parent chain. |
| GET | `web/locations/countries/{countryCode}/cities/{cityCode}/districts/{districtCode}` | 🟡 partial | ReferenceData `LocationController` | District resolved from the districts-by-city list (no single-district module read). |
| GET | `web/pricing` | 🟡 partial | Payment `provider-plans` + `participant-plans` (`[AllowAnonymous]`) | Published subscription tiers only. Commission model / customer platform fee / VAT flag are admin-only → **BLOCKED**. |

**Partial/blocked**: location `{slug}` resolver + `region`/`marina` types, service×location availability, CargoDry
catalogue/product, and contact submit are all BLOCKED on a missing module endpoint — see the blocked doc. The website
renders honest "not available yet" states for these by design.

Every controller action is thin → `IAizenCQRSProcessor.ProcessAsync` → `SetResponse` → `AizenApiResponse<T>`.

---

## Remote clients (Refit `IAizenRemoteCall`) — which module endpoints they hit

| Client | Module | Endpoints | Envelope |
|---|---|---|---|
| `IContentRemoteCall` (public) | Content | `GET /api/v1/content/public/{feed,by-type,items/{slug},categories,items/{id}/comments}` | **raw DTO** |
| `IContentRemoteCall` (`/me`) | Content | `POST/DELETE/GET /api/v1/content/me/*` | `AizenApiResponse<T>` |
| `IIdentityRemoteCall` | Identity | `GET /api/v1/identity/participant/profiles/by-subject/{sub}` | `AizenApiResponse<T>` |
| `IReferenceDataRemoteCall` | ReferenceData | `GET /api/v1/reference-data/locations/{countries,countries/{c},{c}/cities,{c}/cities/{city},.../districts}`, `.../lookup-groups/lookup-items/{group}` | `AizenApiResponse<T>` |
| `IPaymentPlanRemoteCall` (W4) | Payment | `GET /api/v1/payment/{provider-plans,participant-plans}` (`[AllowAnonymous]` on the module) | **raw DTO** (BFF-local wire mirrors) |

**Envelope discipline:** Content *public* endpoints return the raw DTO (`Ok(dto)`) → bind `Task<T>`; Content `/me`
and all ReferenceData/Identity endpoints wrap in `AizenApiResponse<T>` → handlers unwrap `.Body`. A Refit
`ApiException` is always translated to a clean `AizenBusinessException` (404 → specific "not found", other →
generic "unavailable"); no Refit type or raw status leaks to the caller.

**Future public-directory clients (not built — no public module endpoints exist yet):** a provider/venue directory
would need `Identity`/`Vessel` public read endpoints. Grepped at W3: Identity exposes only auth endpoints as
`[AllowAnonymous]` (OTP/login/refresh) and Vessel exposes none — so no directory client was invented. Add one only
when a genuinely public module endpoint ships.

---

## Surface pinning

The BFF serves exactly one content surface: `WebContentSurface.Pinned = ContentSurface.MarineOsWeb`. The feed and
by-type handlers send it on every Content call; the web query types have **no `Surface` property**, so a caller can
never request another surface (e.g. Provider or MobileParticipant content) onto the website. The module still
enforces visibility/expiry — the BFF only passes the surface.

## Field-stripping guarantees (security-critical)

`WebContentMapper` reshapes module DTOs so the website never receives internal fields:

- **by-slug detail** (`WebContentDetailDto`) omits `AuthorUserId`, `Placements`, `Audience`, `Status`, `ExpireAt`,
  `UpdatedAt`, and the full multi-language `Translations` set (one language is resolved); media omits `FileStorageId`.
- **public comments** (`WebContentCommentDto`) omit author ids, `Status`, and the moderation trail
  (`ModeratedByUserId`/`ModeratedAt`/`LastModerationReason`).
- **my-comments** (`WebMyCommentDto`) keep the caller's **own** `Status` (so they see if it's live) but strip author
  ids and the moderator trail.
- **my-favorites** (`WebMyFavoriteDto`) strip the participant's internal `UserId`/`ProfileId`.

These are covered by `WebContentMapperTests` (behavioural + structural property-absence assertions).

---

## SEO contract (W3)

What a server-rendered, indexed site needs from the BFF and the module cannot infer client-side.

### `availableLangs` — hreflang alternates (W3.1)

Editorial content keeps **localized route segments** (`/tr/rehberler/x` ↔ `/en/guides/x`) but a **single entity
slug** (O-12), so there is no per-locale slug. Instead every projection advertises the exact locales an item exists
in, and the site emits `<link rel="alternate" hreflang>` for those only:

- **Detail** — `WebContentDetailDto.AvailableLangs`, mapped in `WebContentMapper` from the item's full translation
  set (**BFF-only**; the by-slug detail already carries every translation).
- **Feed / slug items** — `ContentItemSummaryDto.AvailableLangs`, an **additive** field populated in the Content
  module's `ContentMapper.ToSummary`. It rides the existing feed passthrough and the SEO slug feed.

### `Seo` block + indexability verdict (W3.2)

Every indexable projection carries `Seo: { Indexable, Reason, Title?, Description? }`. `Indexable` is the **backend's
verdict** (the frontend fails safe to `noindex` when it is absent) and `Reason` explains it. It is computed behind
the **`ISeoIndexabilityPolicy`** seam — never with `if`-statements hardcoded at call sites. Detail folds `SeoTitle`/
`SeoDescription` into the block (both are also kept flat on the detail DTO for back-compat); coarser projections
(slug feed) carry the verdict computed from the fields they have (no body → a `summary-level` reason).

The current implementation `OptionsSeoIndexabilityPolicy` reads thresholds from **`MarineWebPublic:Seo`**
(`RequirePublished`, `RequireTitle`, `RequireDescription`, `MinBodyLength`), so SEO owners tune indexability by
config, not a deployment.

> **FUTURE — managed-data policy.** The policy source migrates from BFF Options to **managed data (ReferenceData
> `SystemParameter`)** by swapping in a new `ISeoIndexabilityPolicy` implementation. Because the handlers depend on
> the interface (not on Options), **no call site changes**. Marked `// FUTURE:` in the seam and its Options.

### Slug feed (W3.3)

`GET api/v1/web/seo/slugs/{entityType}?lang=tr → [{ slug, availableLangs[], lastModified, indexable }]`. Only
`entityType=content` is reachable today (the one module with a public slugged read); any other type is rejected with
a clean error, never faked. The handler enumerates the whole published `MarineOsWeb` feed (paged, bounded) so the
sitemap is complete.

> **`lastModified` precision caveat.** The feed summary carries **`PublishedAt`, not `UpdatedAt`**, so `lastModified`
> reflects publish time — a later edit that does not republish will not move it. Accept this for sitemap
> `<lastmod>` until the summary carries a true modified timestamp.

### Reserved slugs (W3.4)

A site resolves static route segments before dynamic entity routes, so an entity slugged `search`, `request`, `new`,
`all`, `compare`, `index`, `api`, `admin`, `sitemap`, `robots`, `assets`, `static`, `_next` — or a Turkish
equivalent (`ara`, `talep`, `yeni`, `tumu`, `karsilastir`) — would be permanently, silently unreachable. Enforcement
is a **Content-module authoring rule** (the read-only BFF cannot police creation): the single source of truth is
`Aizen.Modules.Content.Application.Services.ReservedSlugs`, referenced by the create/update `FluentValidation`
validators and by `SlugService` auto-generation (a reserved auto-slug is suffixed rather than emitted bare).

---

## Configuration keys

| Section | Keys | Purpose |
|---|---|---|
| `MarineWebKeycloak` | `BaseUrl`, `Realm`, `Authority`/`MetadataAddress`, `Audience`/`BffClientId` (resource-server audience), `ClientId`, `AdminClientId` + `AdminClientSecret` (confidential service account), `WebUserRole`, `ParticipantProfileIdAttributeName`, `ModuleAssertionSecret` | **`/me` (frozen) surface only** — JWT validation + outbound service token + identity assertion. Not exercised by the website. |
| `MarineWebPublic:TrustedCallerSecret` | secret string | Trusted server-caller secret checked against the `X-Aizen-Web-Caller` header (server-side only). Empty ⇒ every caller stays on `public-read-ip` (fail-closed). |
| `MarineWebPublic:TrustedRateLimit` | `PermitLimit` (def 6000), `WindowSeconds` (def 60) | Shared limiter for valid trusted callers. `PermitLimit <= 0` ⇒ effectively unlimited. |
| `MarineWebPublic:Revalidate` | `Url`, `Secret` | Best-effort revalidation webhook target on the Next.js server. Empty `Url` ⇒ webhook disabled. |
| `MarineWebPublic:Seo` | `RequirePublished` (def true), `RequireTitle` (def true), `RequireDescription` (def false), `MinBodyLength` (def 200) | Thresholds behind the `ISeoIndexabilityPolicy` seam (W3.2). Config-managed so SEO owners retune indexability without a deploy. `// FUTURE:` migrates to ReferenceData `SystemParameter`. |
| `RateLimiting:PublicRead` | `PermitLimit` (def 120), `WindowSeconds` (def 60) | Per-client-IP sliding window for untrusted callers (`public-read-ip`), **unchanged**. |
| `Cors:AllowedOrigins` | `string[]` | Local browser dev only (see auth model); not load-bearing server-to-server. `AllowCredentials`, no wildcard, under `AizenBffCors.PolicyName` before authentication. |
| `RemoteCalls:I{X}RemoteCall:BaseUrl` | `IContentRemoteCall`, `IIdentityRemoteCall`, `IReferenceDataRemoteCall`, `IPaymentPlanRemoteCall` (W4 pricing → payment-api) | Downstream module host base URLs. |

Forwarded headers (`X-Forwarded-For`/`-Proto`) are honoured so the real client IP drives the per-IP limiter for
untrusted callers (trusted callers partition on a fixed shared key, so their few IPs are irrelevant).

## Frozen surface

`api/v1/web/me/content/*` is **built, unconsumed, frozen pending a customer app** and is excluded from the website's
integration contract. The website is authentication-free by design; nothing calls `/me`. The code is tested and kept
for a future authenticated app — do not delete it and do not wire it into the public read surface.

---

## Ops dependencies

See [`docs/MARINE_WEB_OPS.md`](docs/MARINE_WEB_OPS.md). In short:

- `MarineWebKeycloak:ModuleAssertionSecret` **must equal** each consumed module's `BffAssertion:SharedSecret`
  (Content especially, for `/me`).
- The `marine-web-bff` Keycloak service account needs the `reference_data_read` role (for `lookups/{groupCode}`)
  and `identity_read` (for by-subject resolution).
- A `web_user` realm role + a `participant_profile_id` claim mapper on the web SPA client.
- Gateway routing to this BFF host + `RemoteCalls:*` base URLs per module.

---

## Frontend contract

The Next.js team consumes [`docs/MARINE_WEB_FRONTEND_CONTRACT.md`](docs/MARINE_WEB_FRONTEND_CONTRACT.md): every
website-facing endpoint marked `EXISTING` / `PROPOSED` / `BLOCKED`, with the fields returned and the fields
deliberately withheld. Blocked items cross-reference [`docs/MARINE_WEB_BLOCKED.md`](../../docs/MARINE_WEB_BLOCKED.md),
which holds the per-item unblock specs (owner module, route, web-safe fields) for the module teams.

## Tests

`Aizen.Bff.Marine.Web.UnitTests` (xUnit + FluentAssertions). Run:
`dotnet test Bff/src/Marine.Web/Aizen.Bff.Marine.Web.UnitTests`.

- **Field-stripping (security-critical)** — `WebContentMapperTests` (detail/comments/`/me`), `WebServiceMapperTests`
  (service tiles + slug derivation), `WebLocationMapperTests` (parent chain + code fallback), `WebPricingMapperTests`
  (plan tiers, no db-id / per-viewer / economics leak).
- **W2.1 trusted caller** — `TrustedWebCallerTests`: valid secret → trusted partition; missing/invalid → per-IP;
  empty configured secret → fail-closed; constant-time compare.
- **W2.2 freshness** — `WebCacheTests`: `Cache-Control` + weak `ETag`, `If-None-Match` → `304`, non-GET not cached.
- **W3 SEO** — `SeoIndexabilityPolicyTests` (verdict driven by Options thresholds, not hardcoded) and
  `WebSeoContractTests` (`AvailableLangs` on detail, `Seo` block from the policy, slug feed
  slug/langs/lastModified/indexable, unknown entity type rejected not faked).
- **Surface pinning, envelope binding, engagement identity, Refit translation** — unchanged from W0–W2.

Reserved-slug rejection (W3.4) is a Content-module rule, tested in
`Aizen.Modules.Content.Application.UnitTests` → `ReservedSlugsTests` (each reserved segment incl. Turkish,
case-insensitive; create/update validators; `SlugService` auto-suffix).
