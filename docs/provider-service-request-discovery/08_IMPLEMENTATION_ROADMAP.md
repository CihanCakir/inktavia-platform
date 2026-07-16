# 08 — Implementation Roadmap

Ordered by dependency, not by visibility. The screen cannot be honest before its data exists.

---

## Phase 0 — Contract & architecture validation

**Goal.** Freeze decisions before a handler is written.
**Confirm.** Canonical auth (end-user token stops at the BFF; service-account + assertion inward; identity never
a parameter) · geo as a **marked temporary exception** in ServiceRequest · browser coordinates are **untrusted
discovery input** with a **city fallback** · snapped coordinates · vessel **snapshot at publication** · offer
**projection** instead of exclusion · `LocationMode` codes instead of "service area" · hybrid endpoints · cursor
paging · MapLibre.
**Open product decisions to close here:** budget in/out of MVP (doc 05 §C) · the meaning of `ExpiresAt`.
**Exit.** No implementation begins while "who sets the budget" is still unanswered.

## Phase 1 — Backend discovery foundation (no geo yet)

**Projects.** `Aizen.Modules.ServiceRequest.{Abstraction,Application,Domain,Repository}`
**Tasks.**
- `PublishedAt` (nullable, UTC): migration; set in the publication command; **idempotent** (a republish does not
  overwrite); backfill published rows from `CreateDate` and **record that it is approximate**.
- Vessel snapshot columns, populated at publication, **immutable** thereafter; one-off backfill from Vessel;
  `null` where unknown.
- Budget columns **only if Phase 0 said yes**.
- `ProviderServiceRequestDiscoveryFilter` + query + handler + validator (**no `ProviderProfileId` field**).
- Repository: `Select` projection (no `Include`); `.Count()` / `.Any()` subqueries; provider offer-state
  projection; **delete the hard exclusion**; `OfferState` as an optional predicate.
- Cursor pagination with a filter fingerprint; stable tiebreak on `Id`.
- Indexes: `(Status, LocationCityCode)`, `(Status, PublishedAt)`, title search.
**Acceptance.** A provider who has bid on a request **sees it**, badged. No child collection is materialised.
Paging is stable while new requests are published. Identity comes from the assertion; a client-supplied
`providerProfileId` changes nothing.
**Rollback.** New endpoint only; the old `open` endpoint stays until the SPA switches.

## Phase 2 — Geospatial + privacy

**Tasks.** Indexed lat/lng; server-side bbox prefilter; **haversine in SQL**; distance sorting with `Id`
tiebreak; coordinate **snapping** in the projection; validation (ranges, radius ≤ 200, `DistanceAsc` ⇒ center);
`LocationMode` resolution incl. the **city fallback**; the **GEO ON LOAN** header on every geo file.
**Acceptance.** The generated SQL is pasted in the report and shows the index in use. No distance arithmetic in
BFF or SPA. Discovery never returns exact coordinates. No origin ⇒ city results, `DistanceKm = null`,
`LocationMode = ProviderCity`.
**Risk.** Npgsql translation of haversine — fall back to `FromSqlInterpolated`, **never** to in-memory sorting.

## Phase 3 — Provider BFF aggregation

**Tasks.** `discovery`, `discovery/markers`, `discovery/summary`; `AizenRemoteCall` methods through the
**existing** `MarineProviderBffAuthDelegatingHandler`; summary cache (30–60 s, filter-fingerprint key).
**Acceptance.** No domain logic in the BFF. Identity from `IProviderIdentityHolder` → assertion. Missing
identity ⇒ **reject** (not an empty list). Auth tests: assertion propagation · missing/invalid secret ·
unauthorized client id · **a client-supplied `providerProfileId` cannot change the authorization context**.

## Phase 4 — Frontend data layer & page

**Tasks.** `features/service-requests/discovery/` — adapter + zod, query keys, filter model, **URL state**,
debounce + `AbortController`, cursor infinite scroll (reset on filter change), KPI cards driven by
`LocationMode`, card component, skeleton/empty/error states, i18n for every enum, locale formatters.
**Acceptance.** Zero sample data. No raw enum in the DOM. Budget/distance/vessel render **only** when present.
No `providerProfileId` is ever sent.

## Phase 5 — Map & interactions

**Tasks.** MapLibre + clustering; markers bound to the viewport (throttled); two-way marker↔card selection;
"search this area"; geolocation **on demand** with a designed **denial** path; view switcher; responsive collapse.
**Acceptance.** Denying location leaves a working, city-filtered screen. `Truncated` ⇒ "zoom in", never a
partial map presented as complete.

## Phase 6 — Realtime

**Tasks.** Publish updated / cancelled / urgency-changed from the module; consume in the BFF; SPA
insert/update/remove **by request id** (idempotent, coalesced); disconnected banner.
**Acceptance.** A duplicate frame does not duplicate a card. **Re-run the two-replica backplane test** — the
event must arrive when the socket-less instance consumes the message. (That test has already caught three
silent failures. Do not skip it because "nothing changed in realtime".)

## Phase 7 — Tests, performance, hardening

Query plans (index used, no seq scan on the hot path) · N+1 audit with query logs, not by reading code · payload
sizes · page-size caps · cache TTLs · Playwright E2E · visual pass vs `screen.png` · a11y.

**Security hardening, mandatory before production (doc 06 §1):**
- Internal modules **not publicly reachable**; `NetworkPolicy` admitting only authorized BFFs/service clients.
- **`AllowedClientIds` explicitly configured** — an empty allowlist means any valid service token plus the
  secret is accepted. Not acceptable in production.
- No secrets, tokens, whole auth headers or exact coordinates in logs; `/hubs` query-string logging disabled at
  the ingress.

## Phase 8 — Release & observability

Feature-flag the route; keep the old list until parity. Metrics: discovery p95 · markers/viewport · realtime
drops · **geolocation denial rate** (it tells us how often the fallback *is* the product). Rollback = flag off;
migrations are additive.

## Post-MVP — Market price range (replaces the budget field we deliberately did not build)

**Decision (2026-07-14): no owner-stated budget.** See `09a` for the reasoning: a published budget anchors every
offer to the ceiling, owners cannot realistically state one for marine work, and it is not what a provider needs
in order to decide.

The capability the screen actually wants is **price context**, and we can produce it ourselves once offer data
accumulates:

> "Recent offers for this category, on vessels of this size, ranged **₺14.000 – ₺19.000**."

Derived from historical `ServiceRequestOffer` totals, grouped by `ServiceCategoryCode` + vessel length band +
city (the vessel snapshot from Phase 1 makes that grouping possible). Shown to **both sides**.

Why this is the better shape:

- It informs the owner **without whispering a ceiling to the supply side** — the anchoring effect only exists
  because the buyer's maximum is private information being made public. A market range is public information
  being made visible.
- Both parties see the same number, so competition happens on real cost and reputation rather than on who guessed
  the buyer's limit.
- It gets **better with data**, whereas an owner-stated budget gets no better ever.

Preconditions: enough completed offers per (category × size band × city) to be meaningful — publish a range only
above a minimum sample (e.g. ≥ 5 offers), and suppress it otherwise rather than showing a range built from two
data points. A confidently wrong price range is worse than none.

Scope: read-model or scheduled aggregate; **not** a discovery-time computation. Revisit after the offer composer
(roadmap P1) has produced real line-item offers.

## Security follow-up (recorded, not in this phase)

Replace the **static** shared-secret assertion with a **short-lived signed BFF assertion JWT** (`iss`, `aud`,
`exp`, `iat`, `jti`, `user_id`, `provider_profile_id`, calling client id; replay protection via `jti`/nonce).
Written down so "interim" does not quietly become permanent.

---

## MVP cut

**In:** discovery list with offer state · KPIs with `LocationMode` · filters · city fallback · radius/bounds +
distance · snapped coordinates · map + clustering · realtime published/cancelled · i18n · designed states.
**Out (record, do not sneak in):** budget (no producer exists) · vessel fields where the backfill found nothing ·
marina-code filtering (marina is free text) · provider service-area matching (Identity has none) · true
subcategories · signed assertion JWT.
