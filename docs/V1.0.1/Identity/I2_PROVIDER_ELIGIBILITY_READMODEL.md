# I2 (core) — provider location + service-category eligibility read-model (queryable "providers for area")

> **Repo:** `addesso-project` — **Identity module** (provider profile + onboarding) + a remote call for consumers.
> Foundation that unblocks THREE things: **N-C region notifications** ("bölgede açılan iş talebi"), the **provider
> realtime city-group** (today providers never get city events because their profile City is empty — the known
> city-group bug), and **travel pricing (SR S4)**. This is the permanent, correct source Option 3 called for — not a
> throwaway proxy. MVP = **city-level** (geo radius / service-area polygons come later with GeoDiscovery).

## Problem (investigated)
- Provider profile = Identity `UserProfileEntity`; it **has a `City` field but it's unpopulated**, no service-area, no
  categories. Provider **service categories live only in onboarding JSON** (not queryable). There is **no provider
  directory / by-city / eligibility query anywhere**. So nothing can answer "which providers serve city X + category Y".

## Scope — build the queryable eligibility read-model
### 1. Provider operating location (City) — capture + populate
- Ensure the provider's **operating city** (+ country) is captured at onboarding (`SubmitProviderOnboarding`) and written
  to `UserProfileEntity.City` (the field exists; wire it if onboarding doesn't set it). Validate the city code against
  **ReferenceData** (reject unknown codes, as the discovery query already does).
- This alone fixes the **city-group realtime bug** (`ProviderRealtimeHub` reads `resolution.Profile?.City` → now
  populated → providers finally join their real city group).

### 2. Provider service categories — normalize into a queryable table
- Add a normalized **`ProviderServiceCategory`** (provider × `ServiceCategoryCode`) sourced from onboarding (today JSON).
  Populate it in `SubmitProviderOnboarding` (validate category codes against ReferenceData/the marine lookups). Update on
  onboarding revision.

### 3. Eligibility read-model + query
- `GetProvidersForArea(cityCode, categoryCode?)` → **active + approved** provider user ids (and profile ids) whose City
  matches and (if categoryCode given) who serve that category. Repository query with indexes on City + category; only
  approved/active providers; support paging/cap for large cities.

### 4. Backfill existing providers (idempotent)
- Populate `City` for existing approved providers (from the onboarding JSON / wherever captured) and extract their
  categories into `ProviderServiceCategory`. Idempotent + re-runnable + reversible; report counts. (Providers with no
  resolvable city are skipped and flagged — they won't receive area notifications until they set a city.)

### 5. Expose a remote call
- Internal remote call (typed, `[Authorize]`, envelope-correct) so the **Notification module** (N-C `GetProvidersForServiceRequestArea`) can resolve targets, and note it's reusable by travel (S4) and any GeoDiscovery successor.

## Don't-break / QA
- Additive: new table + onboarding capture + backfill + query + remote call. Existing onboarding/approval flow otherwise
  unchanged; `UserProfileEntity.City` already existed. No change to Payment/Messaging/bus. Migration applies cleanly;
  backfill idempotent. Builds clean.
- MVP city-level; leave a seam for radius/service-area (GeoDiscovery) — don't build geo now.

## Verify
1. A provider onboarded (or backfilled) with city = İzmir (35) + category = "hull cleaning" is returned by
   `GetProvidersForArea("35","hull-cleaning")`; a provider in another city/category is not; unapproved/inactive excluded.
2. `ProviderRealtimeHub` now logs the provider joining their **real** city group (city-group bug fixed) — a provider gets
   city realtime events.
3. Backfill counts reported; re-run converges (idempotent); providers with no city flagged.
4. The remote call is reachable by the Notification module (sets up N-C C1).

## Report
`docs/V1.0.1/Identity/REPORT_I2_PROVIDER_ELIGIBILITY.md`: the City capture + `ProviderServiceCategory` model, the
onboarding wiring, the backfill (counts + skipped-no-city), the `GetProvidersForArea` query + remote call, and proof the
city-group realtime now works. Then N-C C1 region fan-out can be re-run against this real source.
