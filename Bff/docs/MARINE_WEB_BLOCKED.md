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
| 4 | Service × location | `GET api/v1/web/service-pages/{serviceSlug}` (interim, cityCode query) | ✅ REACHABLE (M2 done) | Identity `GET /providers/for-area/availability` (`[AllowAnonymous]`, coarse enum) + W4 catalogue; full `/{serviceSlug}/{locationSlug}` slug form pending M3 |
| 5 | Public pricing | `GET api/v1/web/pricing` | ✅ REACHABLE (M1 done) | plans (`[AllowAnonymous]` plan GETs) **+** `GET /api/v1/payment/public/pricing-terms` (M1, `[AllowAnonymous]`) — commission/fee terms folded in; VAT deliberately omitted |
| 6 | CargoDry catalogue + product | `GET api/v1/web/cargodry`, `.../{slug}` | ⛔ BLOCKED | none — `CargoDryPublicController` exposes only `POST validate` |
| 7 | Contact submit | `POST api/v1/web/contact` | ✅ REACHABLE (M4 done) | `POST /api/v1/notification/public/contact` (`[AllowAnonymous]`) — folded into Notification (interim owner); persist + spam-score + admin notify |

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

## 3 — Location enrichment — slug resolver + single reads ✅ DONE (M3-1); region/marina ⛔ BLOCKED (data-sourcing)

**DONE (M3-1):**
1. **Single-slug resolver.** ReferenceData location docs now carry a deterministic, globally-unique, reserved-guarded
   `Slug` (Turkish-aware; backfilled idempotently at startup; partial-unique index per collection). Flat resolver
   `GET /api/v1/reference-data/locations/by-slug/{slug}` (`[AllowAnonymous]`) → `{ locationType, code, name,
   parentChain[], slug }`. The BFF collapses to **`GET api/v1/web/locations/{slug}`** (code-keyed routes KEPT for
   back-compat + coordinate-rich detail).
2. **Single reads.** `GET .../districts/{districtCode}` and `GET .../neighborhoods/{neighborhoodCode}` — replace the
   BFF's district list-filter workaround.
3. **Service × location slug form.** M2's `service-pages` upgraded to **`GET api/v1/web/service-pages/{serviceSlug}/{locationSlug}`**
   (locationSlug → city via the resolver; availability stays **city-keyed**). The interim `?cityCode=` form still works.

**Still BLOCKED — `region` and `marina` location types (a data-sourcing project, not code):**
The hierarchy is country → city → district → neighborhood only. Adding `region`/`marina` needs **new Mongo
collections + public reads AND a sourced dataset** — a regions list (per country, with city groupings) and a marina
POI dataset (names + coordinates + parent geography). **No source data exists today** (the only `marina` in the repo
is the `MARINA_SUPPORT` service category, not a place). Deferred until a dataset is provided; the BFF will not
fabricate a geography.

**Available services per location** is delivered by the service × location signal (§4).

## 4 — Service × location landing — ✅ DONE (M2, interim code form)

**Resolved.** The coverage read-model is Identity's I2 (`provider_service_categories` + `UserProfile.City`), already
`[AllowAnonymous]`. M2 added a **coarse** sibling endpoint and a BFF aggregation:

- **Identity** — `GET /api/v1/identity/providers/for-area/availability?cityCode={c}&categoryCode={cat}`
  (`[AllowAnonymous]`, mirrors `providers/for-area`, no new role) → CQRS `GetProviderAreaAvailabilityQuery` →
  `ProviderAreaAvailabilityDto { cityCode, categoryCode?, availability }`. Buckets internally from config
  `Availability:Thresholds` (`LimitedMin`=1, `AvailableMin`=3): `0`→none, `1..2`→limited, `≥3`→available. The query
  fetches **at most `AvailableMin` rows** (`CountForAreaAsync` uses a server-side `LIMIT` then `COUNT`), so no
  provider count/ids ever leave the module.
- **BFF** — `GET api/v1/web/service-pages/{serviceSlug}?cityCode={cityCode}` (`[AllowAnonymous]`, `[WebCache]`,
  trusted-caller aware). Resolves `serviceSlug` → `SERVICE_PROVIDER_CATEGORY.Code` (reverse of the W4 slug transform;
  unknown slug → clean not-found), aggregates service summary (#1) + city summary (#3) + the availability enum into
  one `WebServicePageDto`. Carries only the enum — no count/ids.
- The category filter matches because provider eligibility now stores canonical `lower(SERVICE_PROVIDER_CATEGORY.Code)`
  (see the CANON track) — the BFF passes the Code, the module compares `Code.ToLowerInvariant()`.

**Still blocked (city-keyed read-model):**
- **District / marina availability.** Coverage is keyed by `UserProfile.City` only; there is no district/marina
  granularity. Availability accepts a **city code** only.
  - Unblock: the eligibility read-model would need finer location keying (ties into #3's region/marina work).
- **Full `{serviceSlug}/{locationSlug}` slug form.** The interim route takes `cityCode` as a query code because
  locations have no slug resolver yet (#3). When M3 ships `GET /locations/by-slug/{slug}`, the BFF upgrades to
  `GET api/v1/web/service-pages/{serviceSlug}/{locationSlug}` (resolve locationSlug → cityCode) with no change to the
  availability signal.
- **`priceRange`** — omitted (no approved per-(service, location) published price exists; unchanged from the audit).

## 5 — Public pricing — commission model, customer platform fee, VAT flag — ✅ DONE (M1)

**Resolved.** Payment now exposes `GET /api/v1/payment/public/pricing-terms` (`[AllowAnonymous]`, in-cluster —
mirrors the sibling anonymous plan GETs, no new Keycloak role) via `PaymentPublicController` → CQRS
`GetPublicPricingTermsQuery`. The BFF folds it into `api/v1/web/pricing` as a `terms` block beside the plan tiers.

**Published (headline only):**
- **Commission** — the Global **Standard-priority, effective-now** rule's rate (`GetPublishedStandardGlobalRuleAsync`),
  as `standardRatePercent` (nullable, never fabricated) + a "may vary by plan/category/agreement" note, framed as
  provider-facing. Deliberately EXCLUDES the Emergency surge, scheduled/expired rules, and per-provider/plan/category
  negotiated rates. (This is NOT the old `GetStatsAsync` `GlobalBaseRate` KPI, which ignored priority/window.)
- **Customer platform fee** — the Global fee rule (no category, no customer type, TRY): `model`, `ratePercent`
  (Percentage/PercentageWithBounds), `minAmount`/`maxAmount` (PercentageWithBounds).
- **effectiveFrom** — max EffectiveFrom of the two source Global rules.

**VAT: intentionally omitted entirely** (no `vatIncluded`, no rate) — `PlatformFeeRuleEntity.VatRate` is optional/null
and resolved at transaction time, and its treatment awaits YMM sign-off, so it is not a publishable headline.

**Confirmed never surfaced:** cost-share rates, tevkifat/withholding mechanics, profit-protection internals,
economics/commission-allocation snapshots, per-provider commission benefits/overrides, category/customer-type override
rows, rule `notes`/`ruleCode`/user ids/DB ids — none are modelled in `PublicPricingTermsDto`, proven by a
field-stripping test. The commission/fee **admin** controllers and every economics-calculation path are untouched.

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

## 7 — Contact submit — ✅ DONE (M4, folded into Notification as interim owner)

**Resolved.** Owner = **Notification** (interim — a future dedicated Support/ContactIntake module could take it over;
contact intake is a distinct bounded context from notification delivery).

- **Notification** — `POST /api/v1/notification/public/contact` (`PublicContactController`, `[AllowAnonymous]`,
  in-cluster) → CQRS `SubmitContactMessageCommand`. FluentValidation shape check; **spam scoring** (honeypot ⇒ max
  score; link count / message length / all-caps heuristics + a per-IP submission-rate penalty, thresholds from config
  `ContactIntake`); **persists** a `ContactMessageEntity` (Postgres `contact_messages`: name/email/subject/message,
  `SourcePage?`, `Status`, `SpamScore`, opaque `TicketRef`, **salted `IpHash` — never the raw IP**, `CreatedAt`);
  **below threshold** → admin fan-out (`GetAdminUserIds` + one `SendNotificationCommand` per admin,
  `NotificationType.ContactReceived`=920, InApp template). Always returns `{ accepted:true, ticketRef }` — a spammer
  learns nothing. **Captcha:** `captchaToken` accepted-and-ignored behind a documented `// FUTURE:` verifier seam.
- **BFF** — `POST api/v1/web/contact` (`[AllowAnonymous]`, thin → CQRS → module) on the **stricter `contact-submit`**
  rate-limit policy (default 5/60s per IP, tighter than `public-read-ip`; not bypassed by the trusted-caller secret).
  Forwards the caller IP as `X-Forwarded-For` for the module to hash. Untrusted payload end to end.

**Request** `{ name, email, subject, message, sourcePage?, captchaToken? }` (+ a hidden honeypot field).
**Response** `{ accepted, ticketRef? }` — no DB id, no internal fields.
