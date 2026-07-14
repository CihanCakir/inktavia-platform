# Claude Code Prompt — One city vocabulary: ReferenceData codes, end to end (+ backfill)

**Follow-up to `docs/realtime-backplane-two-replica-report.md`.** The backplane is proven. Do not touch it. This
prompt fixes the data problem the backplane test exposed, which is worth more than the backplane itself.

## The bug, stated precisely

The realtime hub joins `city:{UserProfile.City}`. `ServiceRequestPublished` is pushed to
`city:{ServiceRequest.LocationCityCode}`. **These two fields are filled from different vocabularies, and nobody
ever checked that they match.**

- The provider SPA offers a **hardcoded** city list (`src/features/onboarding/model/onboardingFieldConfigs.ts`,
  `CITY_OPTIONS_BY_COUNTRY`, still carrying the comment `@planned MarineProvider BFF GET /location/cities`). Its
  TR entries include `istanbul`, `izmir`, `antalya`, **`bodrum`**, `mersin` — a mix of **provinces and
  districts/ports**.
- Service requests carry province codes: the seeded request at *Bodrum Marina* has `LocationCityCode = MUGLA`.

So a provider who selects **Bodrum** joins `city:bodrum`, while every request in Bodrum is published to
`city:mugla`. They are never told about a single job in their own harbour. No error, no log, no complaint — just
an empty screen that looks like a quiet market.

`UserProfile.City` is also a free-text column today, so nothing prevents `"Bodrum"`, `"bodrum"`, `"Çeşme Marina"`
and `"IZMIR"` from coexisting.

Two related defects, already found, are in scope:

1. **Onboarding never populated `UserProfile.City` at all** (`MirrorDraftToProfile` read `BusinessIdentity.city`,
   which does not exist; the value lives in `OperatingRegion.cityOrPort`). A partial fix was applied during the
   backplane test — **review it, do not assume it is right.** Every provider approved before that fix still has
   `City = null`.
2. Profile 100011's `City` was set to `izmir` **by hand** in the database for the test. That is test data, not a
   fix.

## What to build

### 1. One canonical city vocabulary — from ReferenceData

ReferenceData already owns country/city/district. Make it the single source of truth for the code both sides
store. Decide and state the canonical form (e.g. the ReferenceData city code for the **province**), and apply it
identically in Identity and ServiceRequest. Normalise casing in exactly **one** place — not in each caller.

**City-level matching is the MVP** (see the GeoDiscovery decision: the supply side has no finer geo data). Bodrum
is therefore *not* a city; it is a district of Muğla. Port/district refinement belongs to GeoDiscovery later. Say
so in the code where someone will be tempted to re-add it.

### 2. Serve the list from the server; delete the hardcoded one

- Add the BFF endpoint the SPA's own comment already promises (`GET /location/cities?country={code}`), backed by
  ReferenceData. Cache it — it changes rarely.
- Delete `CITY_OPTIONS_BY_COUNTRY` from the SPA and bind the `OperatingRegion` select to the endpoint. A
  hardcoded list in the browser is exactly how the two vocabularies drifted apart.
- Rename the field. `cityOrPort` is the bug in linguistic form: a field that may hold *either* a city or a port
  cannot be matched against a city code. Make it `cityCode`, and validate it against ReferenceData **server-side**
  on save — reject an unknown code rather than storing it.

### 3. Backfill

Existing providers have `City = null` (or, after the partial fix, whatever free text they typed). Write a backfill
that maps each existing profile to a canonical code and reports what it could not map, rather than guessing.
Anything unmappable must be listed for a human — do not silently pick the nearest match.

### 4. Make the silence loud

Every defect in this chain was silent, and that is why it survived. Fix that:

- The hub currently logs `city (none)` at **Information** when a provider joins no city group. That is not
  information; it is a provider who will receive nothing. Log it at **Warning**, with the profile id.
- Add a startup/readiness check: if a service maps a SignalR hub and the Redis backplane is configured but **not
  connected**, the pod must fail readiness. Today it starts happily and drops events into the void.

### 5. Audit the BFF's new assembly scan

To make the BFF's realtime consumers register, `TypeInclude = { AppType.Worker }` was added (the framework scanned
only `Aizen.Modules.*`, so both BFF consumers were **dead code** — registered nowhere, consuming nothing).

That fix widens what the BFF loads. **List exactly what is now registered in the BFF process** — consumers,
recurring jobs, anything else. The BFF runs with **many replicas** in Kubernetes. If a recurring job or a
state-mutating consumer was silently picked up, it now runs N times, or writes N times. Report what you find. If
something must not run there, exclude it explicitly.

## Verify — against the real data, not against your own assumptions

1. **Prove the two vocabularies now agree.** Query the ServiceRequest DB for the distinct `LocationCityCode`
   values and the Identity DB for the distinct `UserProfile.City` values. Every value on both sides must be a
   valid ReferenceData city code. Paste the two lists. If they still disagree, the fix is not done.
2. Re-run onboarding for a **new** provider through the real screens and confirm `UserProfile.City` holds the
   canonical code — not free text, not null.
3. Confirm the hub logs `Realtime connected: provider {id}, city {code}` with that same code.
4. A request published in that city reaches the provider. (The backplane already works; this is about the group
   matching, not the transport.)
5. State plainly what the backfill could not map.

## Constraints

- Provider identity from the BFF assertion; never from body or query.
- No geo/radius logic here — that is GeoDiscovery. City-code equality only.
- Do not weaken the hub's server-side group decision. No client-callable subscribe.
- Do not "fix" a mismatch by making the matcher fuzzy (`contains`, `startsWith`, case-insensitive guessing).
  Fuzzy matching would paper over exactly the class of bug this prompt exists to remove.
- If something cannot be finished, leave it and **say so in the summary**.
