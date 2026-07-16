# 09b.1 — Vessel data moves out of ServiceRequest: BFF bulk enrichment

Run before 09c. This reverses the vessel snapshot built in 09a and replaces it with the cheaper, architecturally
correct shape: **the BFF composes; modules do not call each other to enrich each other's reads.**

---

## The rule we are applying (and its one exception)

**Enrichment is the BFF's job.** "What are the vessels behind these 20 requests?" is composition across module
boundaries — the Provider BFF resolves it. ServiceRequest must not reach into Vessel to decorate its own rows.

**Invariant validation stays in the module.** `ServiceRequest → ReferenceData` (validating that
`LocationCityCode` is a real city) **stays exactly as it is.** That call is not enrichment; it enforces a domain
rule. If we moved it to the BFF, the module would be trusting whatever code the caller sent — and any caller
reaching the module directly could store a city code that exists nowhere, producing a request that lands in no
provider's city group and is silently seen by nobody. We closed that exact hole this week; do not reopen it.

The distinction, once, so it is not re-argued: **if the answer changes what we STORE, the module asks. If the
answer only changes what we SHOW, the BFF asks.**

---

## Work

### 1. ServiceRequest — remove the vessel snapshot

- Drop the five columns added in 09a: `VesselTypeCode`, `VesselManufacturer`, `VesselModel`,
  `VesselLengthValue`, `VesselLengthUnitCode`. New EF migration (additive-then-subtractive is fine; the columns
  are empty apart from whatever a test published).
- Delete `IServiceRequestVesselRemoteCall` and its registration/config (`docker-compose` base URL).
- Revert `ServiceRequestEntity.Publish(...)` to its no-argument form and remove the vessel fetch from
  `PublishServiceRequestCommandHandler`.
- **Keep `VesselName`** — it predates all of this and is already denormalised on the row. Do not touch it.
- **Keep the ReferenceData city validation** in the same handler. It is an invariant, not enrichment.
- The discovery DTO now carries **`VesselId`** (and the existing `VesselName`). Nothing else about the vessel.

### 2. Vessel — one bulk summary endpoint

```
GET /api/v1/vessels/summary?ids=1,2,3
```

Returns `VesselSummaryDto[]`: `VesselId`, `Name`, `VesselTypeCode`, `Brand`, `Model`, `LengthValue`,
`LengthUnitCode`. Nothing else — this is a card decoration, not a vessel profile, and every extra field here is
paid for on every page of every provider.

- **Cap the id list** (e.g. 100). Above the cap: reject. An uncapped `ids` parameter is an invitation to pull the
  whole table one request at a time.
- Ids that do not resolve are simply **absent** from the response — not an error. A request whose vessel was
  deleted must still render.
- Auth: the standard service-token + BFF-assertion path. No new mechanism.

### 3. Provider BFF — enrich the page, once

In the discovery handler, **after** the module returns a page:

1. collect the **distinct** `VesselId`s from the page (≤ page size, so ≤ 50 by contract);
2. make **one** call to `GET /vessels/summary?ids=…`;
3. merge into the DTOs by id.

**One call per page. Not one per card.** A test must assert this: N items ⇒ exactly 1 Vessel call. This is the
whole point — the earlier objection to a Vessel call was about N+1 on the read path, and a single bulk call is
not N+1.

**Cache the summaries** (Redis, TTL ~10 minutes, keyed per vessel id). Vessel specs change rarely; a provider
scrolling a list will hit the same boats repeatedly, and most pages will need no Vessel call at all after the
first.

**Failure policy — degrade, never fail:** if Vessel is unreachable or slow, return the page with the vessel
fields `null` and log a warning. A boat's length is decoration; it must never stop a provider from seeing their
work. (Same policy the vessel snapshot used — keep it.)

**Never filter or sort by vessel data in the BFF.** Filtering after the page is drawn would mean filtering a
page, not the result set — the user would see "20 results" with three of them removed and no way to page
correctly. If vessel-based filtering is ever needed, it belongs in the module's query, and that is a different
conversation.

### 4. Markers

Markers carry **no vessel data at all** and make **no** Vessel call. A viewport can hold hundreds of pins; a
bulk call per pan is exactly the cost we are avoiding.

---

## Why this is the cheaper structure (state it in the report)

| | Snapshot in ServiceRequest (09a) | BFF bulk enrichment (this) |
|---|---|---|
| Cross-module coupling | ServiceRequest → Vessel, on the **write** path | BFF → Vessel, on the **read** path |
| Read cost | zero (columns on the row) | 1 call per page, mostly served from cache |
| Freshness | frozen at publication | always current |
| Schema | 5 columns + migration + backfill | none |
| Backfill for old rows | required (and was never done) | not applicable — nothing to backfill |
| Failure mode | vessel unreachable at publish ⇒ permanent nulls on that row | vessel unreachable ⇒ this page renders without the decoration; next page is fine |

The snapshot's only real advantage was avoiding a read-side call. A single cached bulk call per page is cheap
enough that it does not justify five columns, a migration, a backfill nobody ran, and a permanent staleness bug.

---

## Acceptance — observed, not asserted

- [ ] The five vessel columns are **gone** from the database (paste the schema).
- [ ] `IServiceRequestVesselRemoteCall` no longer exists; ServiceRequest calls **no** module except ReferenceData
      (for the city invariant), which is unchanged and still rejects unknown codes.
- [ ] `GET /vessels/summary?ids=…` exists, is capped, and omits unresolved ids rather than failing.
- [ ] A discovery page of N items triggers **exactly one** Vessel call (test asserts the count, not the code).
- [ ] A second page hitting the same vessels triggers **zero** Vessel calls (cache).
- [ ] With Vessel stopped, the discovery page still renders; vessel fields are null; a warning is logged.
- [ ] Markers make no Vessel call.

## Report

Append to `REPORT_BACKEND_09b.md`: the removed migration, the new Vessel endpoint, the call-count test result,
and the cache TTL. If anything could not be done, the phase is **not done** — say it in the summary line, not in
a paragraph in the middle.
