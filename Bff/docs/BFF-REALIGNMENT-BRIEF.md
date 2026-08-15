# Marine.Web BFF — Realignment Brief for the Backend Team

**Target repository:** `addesso-project`, project
`Bff/src/Marine.Web` (`Aizen.Bff.Marine.Web`).
**Consumer:** `inktavia-marine-web` — a **server-rendered Next.js 16 site** at
`inktavia.com`. **Not** an SPA.

This brief exists because the BFF was built for a different client than the one that will
actually consume it. Everything below follows from that one fact. The verified current
state is in [`MARINEWEB-BFF-FACTS.md`](./MARINEWEB-BFF-FACTS.md).

---

## The one thing to internalise

`README.md` line 3 today says:

> "The Backend-for-Frontend for the public MarineOS website (marineos.* SPA)."

The real consumer is a **server-rendered Next.js application**:

| | The SPA it was designed for | The site that will consume it |
|---|---|---|
| Where requests originate | the visitor's browser | **the Next.js server** |
| How many client IPs | one per visitor | **a handful, for all visitors** |
| Authentication | Keycloak OIDC + PKCE, SPA holds tokens | **none — the site never authenticates anyone** |
| CORS | load-bearing | **irrelevant** (server-to-server) |
| Caching | browser + SWR | **ISR + tag revalidation on the Next server** |
| What it renders | client-side after hydration | **HTML that Google indexes** |

The BFF is otherwise well-built — thin controllers, CQRS, Refit remote calls, pinned
content surface, field-stripping with tests. **Do not rewrite it.** Realign it.

---

## W1 — Audit first (no code)

Confirm and write down, with file paths:

1. Which `ContentType` values exist in `Aizen.Modules.Content.Abstraction.Enum` — and which
   of them represent **guides / articles / editorial posts** as opposed to `Campaign` and
   `ReleaseNote`. The website's Guides section depends on this answer.
2. Whether a **service-category lookup group** exists in ReferenceData (alongside
   `VESSEL_TYPE`). If it does, the service taxonomy may already be reachable through
   `reference/lookups/{groupCode}` and no new endpoint is needed.
3. Which modules expose **genuinely public (`[AllowAnonymous]`) read endpoints** today.
   The README already records that Identity exposes only auth endpoints and Vessel exposes
   none. Re-verify, and extend the list to ServiceRequest, Profile, Payment, CargoDry and
   Commerce.
4. Whether the Content module can filter or return **which languages an item is translated
   into**.
5. How the gateway routes to this BFF, and whether it can identify a trusted server-side
   caller (header, mTLS, source range).

**Report what is possible versus what is blocked on a module.** Do not invent a client for a
module endpoint that does not exist — that discipline is already in the README and it is
correct.

---

## W2 — Realign to a server-side consumer

### W2.1 — Rate limiting ⛔ this is the production blocker

`public-read-ip` permits **120 requests / 60 seconds per client IP**, honouring
`X-Forwarded-For`. That is right for browsers and **wrong for a server-rendered consumer**:
the Next.js server makes every request for every visitor from a few IPs, so it will trip the
limit at modest traffic and the website will start rendering "data unavailable" states. It
will look like a frontend bug. It is not.

**Implement a trusted-caller path:**

- Add a **trusted server-caller credential** — a shared secret header, following the
  ecosystem's existing `X-Aizen-Bff-Assertion` / `BffAssertion:SharedSecret` pattern rather
  than inventing a new mechanism. Suggested: `X-Aizen-Web-Caller` validated against
  `MarineWebPublic:TrustedCallerSecret` (`__FROM_SECRET__`).
- Requests presenting a valid secret use a **separate, much higher (or unlimited) rate-limit
  policy**. Everything else keeps `public-read-ip` exactly as it is today.
- The secret is server-side only. It must never be documented as something a browser sends.
- Keep `X-Forwarded-For` handling for untrusted callers.

Do **not** simply raise `PermitLimit` globally — that weakens the protection the limiter
exists to provide.

### W2.2 — Caching and freshness (this is what makes W2.1 survivable)

The frontend uses ISR with tag-based revalidation, but it can only do that if the BFF tells
it what it is allowed to cache and when things change. Today nothing does.

- Emit **`Cache-Control`** on every public read with a per-resource `max-age` /
  `stale-while-revalidate` that reflects real freshness needs. Content categories and
  reference data change rarely; a content feed changes more often.
- Emit **`ETag`** and/or **`Last-Modified`** so conditional requests are cheap.
- **Publish the freshness contract** in the README as a table: resource → expected staleness
  tolerance. The frontend roadmap has deliberately refused to invent TTLs; this table is the
  missing input.
- **Add a change webhook** (or bus subscription) that can notify the website when content
  changes, so it can revalidate a cache tag instead of polling. Suggested payload:
  `{ entityType, id, slug, lang, changeKind }`.

### W2.3 — Documentation and the `/me` surface

- Update `README.md`: the consumer is a **server-rendered Next.js site**, not an SPA. Correct
  the auth-model section accordingly, and state plainly that the public read surface is
  consumed server-to-server.
- **`api/v1/web/me/content/*` currently has no consumer.** The website has no
  authentication and will never have any — that is a locked decision on the frontend side.
  **Do not delete it.** It is tested, working code that a future customer application will
  want. Mark it in the README as *built, unconsumed, frozen pending a customer app*, and
  exclude it from the website's integration contract.
- CORS: harmless but no longer load-bearing. Keep the config for local development, and note
  in the README that server-to-server calls do not exercise it.

---

## W3 — The SEO contract (what the website cannot do without)

This is where the BFF becomes a *public-website* BFF rather than a generic content proxy.

### W3.1 — `availableLangs` on content detail and feed items ⛔ blocks hreflang

`WebContentDetailDto` returns one `Slug` and the resolved `Lang`. The website must emit
`<link rel="alternate" hreflang>` for **exactly the locales an item actually exists in** —
advertising a Turkish guide as available in English is a real SEO defect.

**Add `AvailableLangs: List<string>`** to `WebContentDetailDto` and to feed items. This is a
small change with a large payoff, and it is the piece the frontend genuinely cannot infer.

> Context: the website keeps **localized route segments** (`/tr/rehberler/x` ↔
> `/en/guides/x`) while accepting a **single entity slug** for editorial content. That is a
> deliberate, scoped exception — recorded as **O-12** — so **no per-locale slug work is
> required in the Content module.** `AvailableLangs` is what replaces it.

### W3.2 — `Seo` block with an indexability verdict

Every projection the website may index must carry:

```
Seo: { Indexable: bool, Reason: string, Title?: string, Description?: string }
```

`Indexable` is the **backend's verdict**, not a frontend heuristic — the frontend enforces it
mechanically and fails safe to `noindex` when it is absent. Critically: **the thresholds
behind that verdict must be configuration or content-managed data, not `if` statements
compiled into the BFF**, so the people who own SEO can change them without a deployment.

`WebContentDetailDto` already has `SeoTitle` / `SeoDescription` — fold them into this block
or keep both, but the `Indexable` + `Reason` pair is new and required.

### W3.3 — Slug feeds for the sitemap ⛔ blocks the sitemap and static generation

The website's `sitemap.xml` and its build-time path generation need, per entity type:

```
GET api/v1/web/seo/slugs/{entityType}?lang=tr
→ [ { slug, availableLangs[], lastModified, indexable } ]
```

Without this the sitemap can only ever contain static routes. Start with `content` (the one
entity type that exists today).

### W3.4 — Reserved slugs

The website resolves static route segments before dynamic ones, so an entity slugged
`search`, `request`, `new`, `all`, `compare`, `index`, `api`, `admin`, `sitemap`, `robots`,
`assets`, `static` or `_next` — or a localized equivalent — would become permanently
unreachable, silently. **Reject these at slug-creation time** in the module or the BFF.

---

## W4 — New public projections (only where a module endpoint exists)

The website's marketing and SEO core has no backend today. In priority order:

| Priority | Projection | Route | Blocked on |
|---|---|---|---|
| 1 | **Service catalogue** — category code, slug, name, short description, `availableLangs`, `Seo` | `api/v1/web/services` | W1.2 — may already be reachable via a lookup group |
| 2 | **Service detail** — description, related categories, available locations, FAQ, `Seo`, breadcrumb | `api/v1/web/services/{slug}` | ReferenceData / ServiceRequest public reads |
| 3 | **Location detail** — `locationType` (`country`/`region`/`city`/`district`/`marina`), `parentChain[]`, description, available services, `Seo` | `api/v1/web/locations/{slug}` | ReferenceData; note the website deliberately uses a **generic location**, not city-only |
| 4 | **Service × location landing** — **one aggregated projection, never four calls from the page**: service summary, location summary, a **coarse** availability signal (`available` / `limited` / `none` — never a raw provider count), optional price range only if commercially approved, FAQ, `Seo` | `api/v1/web/service-pages/{serviceSlug}/{locationSlug}` | ReferenceData + ServiceRequest + Profile |
| 5 | **Public pricing** — commission model, subscription tiers, customer platform fee, currency, VAT flag, `effectiveFrom`. **Published commercial terms only** — no cost-share, no tevkifat mechanics, no internal economics | `api/v1/web/pricing` | Payment/Commerce configuration |
| 6 | **CargoDry catalogue + product** — approved specs and media keys; `price`/`availability` **only** when commercially approved, because the website emits `Product`/`Offer` structured data from them | `api/v1/web/cargodry`, `.../{slug}` | CargoDry / Commerce |
| 7 | **Contact submit** — `POST api/v1/web/contact`, anonymous, **stricter rate limit than reads**, server-side validation, spam scoring, persistence and notification. The website will forward from a Next.js Server Action; it validates shape only and must never be trusted | Notification / a support inbox |

**Non-negotiable for all of them — the same rules the existing code already follows:**

- Web-facing DTOs only. Never surface provider internal scores, commission internals,
  cost-share, admin notes, private profiles, payment internals, performance ranking, or
  database IDs where a public slug exists.
- Anonymous, rate-limited, thin controller → CQRS query → `AizenApiResponse<T>`.
- Field-stripping proven by a mapper test, matching `WebContentMapperTests`.
- **If a module has no public endpoint, do not invent one and do not fabricate data.** Report
  it as blocked and stop. The website renders an honest "not available yet" state and is
  designed to ship in exactly that condition.

---

## W5 — Tests and documentation

- Extend `Aizen.Bff.Marine.Web.UnitTests`: field-stripping for every new mapper, the trusted
  caller path (valid secret → high limit, missing/invalid → `public-read-ip`), `Seo.Indexable`
  passthrough, and `AvailableLangs` correctness.
- Update `README.md`'s endpoint map, auth model and configuration table.
- Update `docs/MARINE_WEB_OPS.md` with the new secret and rate-limit policy.
- Produce a **contract document the frontend can consume**, listing every endpoint as
  `EXISTING` / `PROPOSED` / `BLOCKED`, with the fields returned and the fields deliberately
  withheld.

---

## Definition of done

- [ ] The Next.js server is not rate-limited as if it were one abusive browser.
- [ ] A freshness/caching contract is published, and change notifications exist.
- [ ] `AvailableLangs` ships on content detail and feed items.
- [ ] A `Seo` block with `Indexable` + `Reason` exists on every indexable projection, driven
      by managed policy rather than compiled-in thresholds.
- [ ] A slug feed exists for at least `content`.
- [ ] Reserved slugs are rejected at creation.
- [ ] `README.md` describes the real consumer; `/me` is marked frozen, not deleted.
- [ ] Every new projection has a field-stripping test.
- [ ] Anything blocked on a missing module endpoint is reported as blocked — not faked.
