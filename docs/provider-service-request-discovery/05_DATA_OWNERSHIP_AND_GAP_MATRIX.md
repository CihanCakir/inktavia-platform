# 05 — Data Ownership & Gap Matrix

Size XS/S/M/L/XL · MVP yes/no · Blocker yes/no. Evidence paths are real files.

---

## A. List-card fields

| Field | Owner | Exists? | Gap class | Solution | Size | MVP | Blocker |
|---|---|---|---|---|---|---|---|
| Id, RequestCode | ServiceRequest | ✅ | reusable | — | — | y | n |
| Title, Description | ServiceRequest | ✅ | reusable | excerpt client-side | XS | y | n |
| Status | ServiceRequest | ✅ | reusable | code → i18n | XS | y | n |
| Urgency (`Priority`) | ServiceRequest | ✅ Low=1…Urgent=4, Emergency=5 | reusable | code → i18n; "ACİL" = Urgent+Emergency | XS | y | n |
| Category / type | ServiceRequest + ReferenceData | ✅ codes | existing-but-incomplete | design shows two chips; we have `ServiceCategoryCode` + `ServiceTypeCode`. **No subcategory model — do not invent one** | S | y | n |
| CreatedAt / UpdatedAt | ServiceRequest (audit) | ✅ | reusable | — | — | y | n |
| **PublishedAt** | ServiceRequest | ❌ | **missing field + migration** | see §B | S | **y** | **y** |
| Planned start | ServiceRequest (`RequestedStartDate`) | ✅ | reusable | — | — | y | n |
| Offer deadline | ServiceRequest (`ExpiresAt`) | ⚠️ ambiguous | existing-but-incomplete | **product decision required**: is `ExpiresAt` the request's expiry or the offer deadline? One field, one meaning. Do not add a second half-used column | XS | y | n |
| ~~Budget range + currency~~ | — | ❌ | **DECIDED: not built** (§C) | no columns, no DTO, no card section | — | **no** | n |
| Marina, city | ServiceRequest | ✅ | reusable | city = canonical plate code | — | y | n |
| **Approximate coordinates** | ServiceRequest | ✅ stored, ❌ never returned | **missing API field + privacy rule** | snapped (~500 m grid, deterministic per id); exact coords only for the assigned provider | M | **y** | **y** |
| **DistanceKm** | ServiceRequest (computed) | ❌ | **missing repository capability** | SQL haversine; `null` when no origin | L | y | n |
| Vessel snapshot | ServiceRequest ← Vessel | ❌ (`VesselName` only) | **missing field + migration** | see §D | M | n (display), y (columns) | n |
| AttachmentCount / PhotoCount | ServiceRequest | ⚠️ via `Include` | technical debt | DB-side `.Count()` projection | S | y | n |
| OfferCount | ServiceRequest | ⚠️ via `Include` | technical debt + **privacy** | DB-side `.Count()`; stop loading other providers' offer rows | S | y | n |
| **HasProviderOffer / ProviderOfferId / ProviderOfferStatus** | ServiceRequest | ❌ | **missing capability** | see §E | M | **y** | **y** |
| New / updated indicator | client | ❌ | missing FE | `PublishedAt > lastSeenAt` (per-provider, `localStorage`) | S | y | n |

**Never projected into any discovery DTO:** `OwnerUserId`, `RequestedByEmail`, owner personal data, other
providers' identities or offer amounts, exact coordinates.

---

## B. `PublishedAt` — explicit, not `CreateDate`

`CreateDate` is **draft creation**. Using it for "added today" and "2 hours ago" does not produce a missing
value; it produces a **wrong** one, which is worse because it looks right.

- Add nullable `PublishedAt` (UTC).
- Assigned in the **publication command** (`PublishServiceRequestCommandHandler`) at the Draft → Open/Published
  transition. **Idempotent**: re-publishing must not overwrite an existing `PublishedAt` (a republish is not a
  new publication).
- **Backfill policy**: for rows already in a published status, `PublishedAt = CreateDate`, and the report must
  state that these values are **approximate**. Rows that were never published stay `null`.
- Sorting: `PublishedAtDesc` is the default discovery sort. KPI "added today" counts `PublishedAt >= todayUtc`.
- A request with `PublishedAt = null` is not "new" — it is not published, and it is not in discovery at all.

---

## C. Budget — DECIDED: NO (2026-07-14)

**No columns, no DTO fields, no card section, no placeholder.** The mock shows `₺18.000 – ₺25.000`; the entity
has no budget field and **no owner-side flow writes one**, because we have decided not to ask.

Why (recorded so it is not quietly reversed inside a migration):

- A published budget **anchors offers to the ceiling**; with supply still thin, that transfers value from the
  buyer — our primary customer — to the seller.
- Owners cannot realistically state a budget for marine work (antifouling on a 45 ft hull varies threefold).
- It is not what a provider needs: vessel type/length, category, urgency, marina and distance decide whether a
  job is worth the drive.

**Instead, post-MVP (doc 08): a market price range** derived from historical offers, shown to both sides. It
informs the owner without whispering a ceiling to the supply side, and it improves with data.

**If a future decision reverses this**, all of the following ship together — columns without a producer are dead
columns:

- `BudgetMin`, `BudgetMax`, `BudgetCurrencyCode` — all **nullable**.
- Validation: both absent, or both present, or a **documented** partial-range rule. `BudgetMin <= BudgetMax`.
- `BudgetCurrencyCode` follows the existing ReferenceData currency convention (validated server-side, rejected
  when unknown — the same rule we enforced for city codes).
- An **owner-side flow that actually sets them** (request creation/edit). Without a producer, the columns are
  decoration.

This is recorded as an **open product decision** (doc 12). It is not a blocker for the rest of the screen.

---

## D. Vessel — snapshot at publication (decision: snapshot, not batch endpoint)

Two options were compared:

| | Snapshot at publish | Vessel batch endpoint |
|---|---|---|
| N+1 | eliminated by construction | eliminated only if every caller remembers to batch |
| Discovery query cost | zero extra I/O (columns on the row) | one cross-module call per page, plus latency |
| New dependency | none | a Vessel remote call + a batch API that **do not exist today** |
| Consistency | frozen at publication | always current |

**Decision: snapshot.** Discovery is a read-heavy, latency-sensitive list; a cross-module call per page adds a
network hop to every keystroke and every map pan, and there is no Vessel remote call or batch endpoint to build
on.

- Columns on `ServiceRequestEntity`, populated at **publication**: `VesselName` (exists), `VesselTypeCode`,
  `VesselManufacturer`, `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode`.
- **Mutability: immutable after publication.** The snapshot describes the boat *as the job was advertised*. If
  the owner renames the boat mid-job, the advertised request does not silently change under the providers who
  bid on it. Vessel remains the source of truth for the live vessel; the request keeps its snapshot.
- **Existing requests**: backfill once from Vessel where the vessel still exists; where it does not, leave
  `null` and the UI hides those fields. Do not guess.
- **Not rendered in MVP** unless the backfill actually produced data — a field that is `null` for most rows is
  not a feature.

---

## E. Provider offer state — projection, not exclusion

`BuildProviderOpenQuery` today: `!x.Offers.Any(o => o.ProviderProfileId == providerProfileId)` — it **removes**
the request from the result. The new screen needs the opposite: keep it, badge it, and let the user filter.

Discovery must project, **database-side**:

- `HasProviderOffer` (`Offers.Any(o => o.ProviderProfileId == me && !o.IsDeleted)`)
- `ProviderOfferId`, `ProviderOfferStatus`
- `OfferCount` (`.Count()`), `AttachmentCount`, `PhotoCount`

`OfferState { Any | NotOffered | Offered }` becomes an **optional predicate**, replacing the hard exclusion.

**Do not `Include` the `Offers` / `Attachments` collections to count them.** Today's query loads every offer row
— including other providers' amounts — to produce two integers. That is both a performance defect and a privacy
smell. Counts come from `Any` / `Count` subqueries; foreign offer rows never leave the database.

---

## F. KPIs — labelled by location mode, never by "service area"

There is **no persistent provider service-area data**. The API returns a mode code; the SPA translates it.

| KPI | Definition | Notes |
|---|---|---|
| Open requests (mode-labelled) | `BrowserLocation` → radius · `MapViewport` → bounds · `ProviderCity` → `LocationCityCode == profile.City` | label via i18n; **never** "in your service area" |
| Added today | `PublishedAt >= todayUtc` | blocked on §B |
| Emergency | `Priority >= Urgent` | |
| My active offers | my offers with status `Submitted` | impossible under today's exclusion query (§E) |

---

## G. Filters

| Filter | Backend today | Gap |
|---|---|---|
| Search | ✅ `SearchTerm` | reusable (+ index) |
| Category / type | ✅ | reusable |
| City | ✅ | reusable (canonical plate code; unknown code ⇒ reject) |
| Marina | ⚠️ free-text `LocationMarinaName` | **not a code** — MVP: city only. Marina codes belong to ReferenceData, later |
| Bounding box | ❌ | missing repository capability |
| Center + radius | ❌ | missing repository capability |
| Urgency | ✅ `MinPriority` | reusable |
| Planned-start range | ❌ | S |
| Offer-deadline range | ❌ | depends on the `ExpiresAt` decision (§A) |
| Budget range | ❌ | depends on §C; **not in MVP** |
| Offered / not offered | ⚠️ inverted | becomes an optional predicate (§E) |
| New only | ❌ | needs `PublishedAt` |
| Has attachment | ❌ | S |
| Sort | ⚠️ `CreateDate` only | `PublishedAtDesc`, `DistanceAsc`, `PriorityDesc` — always with `Id` as the stable tiebreak |
| Pagination | ⚠️ offset | **cursor** — offset + realtime insertion duplicates and skips rows |

---

## H. Privacy rules (enforced in the module's projection, not in the BFF)

- Draft / Cancelled / Closed / Expired / assigned requests never appear in discovery.
- Discovery returns **snapped** coordinates. Exact berth coordinates are released only to the **assigned**
  provider, through the existing job endpoints. A marker means "there is work near this marina", not "the boat
  is at berth D-14".
- Owner personal data is never in a discovery DTO.
- Other providers' identities and amounts are never loaded or returned; only the aggregate `OfferCount`.
- **`ProviderProfileId` is never accepted from the client** — it is resolved from the authenticated subject.
- Never log tokens, secrets, whole auth headers, or exact coordinates.

---

## I. Geolocation — untrusted discovery input

Browser coordinates filter **the caller's own view**. They must never be used for authorization, entitlement,
persistent service-area validation, or any access-control decision. Server-side: validate ranges, cap
`RadiusKm`, cap `PageSize`.

**Denial is a designed state, not an error**: fall back to `ProviderCity`, keep loading, hide distances, disable
radius controls, and say so. A screen that requires a permission the user is free to refuse must still work when
they refuse it.
