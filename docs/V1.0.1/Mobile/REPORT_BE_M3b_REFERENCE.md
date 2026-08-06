# BE_M3b — Mobile reference lookups (BFF) + FE dropdown wiring

**Date:** 2026-08-06 · **Repos:** `addesso-project` (BFF + ReferenceData seed) + `inktavia-marine-mobile` (RN).
Second M3 slice. Builds 0 errors, redeployed, HTTP-verified. Read-only (no identity assertion).

## Endpoints (mobile BFF, `[Authorize]` mobile_user)
- `GET /api/v1/mobile/reference/{groupCode}` → `body: [{ code, name, description?, iconKey?, sortOrder, isDefault }]`
  for one lookup group (VESSEL_TYPE / FUEL_TYPE / ENGINE_TYPE / HULL_MATERIAL / SERVICE_PROVIDER_CATEGORY).
- `GET /api/v1/mobile/reference/countries` → `body: [{ code, name, dialCode, currencyCode }]`.
- Literal `countries` route precedes the `{groupCode}` template; group codes are uppercase, `countries` lowercase — no collision.

## How
- New `ReferenceController` → CQRS `GetReferenceItemsQuery(groupCode)` / `GetCountriesQuery` → handlers delegate to
  `IReferenceDataRemoteCall` (added `GetLookupItems` → module `GET /lookup-groups/lookup-items/{groupCode}`, and
  `GetCountries` → module `GET /locations/countries`), mapping the module DTOs to trimmed mobile contracts
  (`ReferenceItemDto` / `CountryItemDto`; `dialCode` = CountryDto.PhoneCode). Handlers order items by `sortOrder`
  and fail soft (empty list + warning log) so a lookup hiccup never 500s a form.
- **Service-account role:** the endpoints returned data (no 403), confirming the `marine-mobile-bff` service account
  already holds `reference_data_read` (granted in M1 foundation). No grant was needed. (kcadm reminder for future:
  grant with `--uid <service-account-user-id>`, not `--uusername …`, which is a silent no-op.)
- **Countries module endpoint is `[AllowAnonymous]`**; the BFF still calls it with the service token.

## Reference seed-data gap fixed (ReferenceData)
The groups VESSEL_TYPE (8) and SERVICE_PROVIDER_CATEGORY (10) had items, but **FUEL_TYPE / ENGINE_TYPE /
HULL_MATERIAL had ZERO items** — the groups exist in `lookup-groups.json` / `lookup-tree-marine.json` but
`lookup-items.json` never authored items for them. Added 5 items each (idempotent per (group, code) via the
existing additive seeder — it inserts new items on an already-seeded DB, so no migration/reset). Turkish `name` +
English `description`, matching the file's convention.
- FUEL_TYPE: DIESEL, GASOLINE, ELECTRIC, HYBRID, LNG
- ENGINE_TYPE: INBOARD, OUTBOARD, STERNDRIVE, JET_DRIVE, SAIL
- HULL_MATERIAL: FIBERGLASS, STEEL, ALUMINUM, WOOD, CARBON_FIBER

**GOTCHA — stale Redis lookup cache:** `GetLookupItemsByGroupQueryHandler` caches results for **12h** in the
distributed (Redis DB 12) cache. Querying the 3 groups *before* reseeding cached empty lists, and the cache
survives a container restart — so even after the reseed the API kept returning `[]`. Fixed by deleting the
`ReferenceData:GetLookupItemsByGroupQueryHandler:*` (+ `GetCountriesQueryHandler:*`) keys from Redis DB 12; the
next call repopulated from the DB. (In normal operation the seeder path invalidates on item change; the manual
flush was only needed because the empty results were cached by my pre-reseed probes.)

## Verification (redeployed `bff-marine-mobile` + `reference-data-api`; `localhost:17003`; mobile_user token)
```
GET /api/v1/mobile/reference/VESSEL_TYPE               → 200  count=8   [MOTOR_YACHT, SAILING_BOAT, CATAMARAN, FISHING_BOAT, …]
GET /api/v1/mobile/reference/FUEL_TYPE                 → 200  count=5   [DIESEL, GASOLINE, ELECTRIC, HYBRID, LNG]
GET /api/v1/mobile/reference/ENGINE_TYPE              → 200  count=5   [INBOARD, OUTBOARD, STERNDRIVE, JET_DRIVE, SAIL]
GET /api/v1/mobile/reference/HULL_MATERIAL           → 200  count=5   [FIBERGLASS, STEEL, ALUMINUM, WOOD, CARBON_FIBER]
GET /api/v1/mobile/reference/SERVICE_PROVIDER_CATEGORY→ 200  count=10  [MOTOR_MAINTENANCE, BOAT_CLEANING, FOOD_AND_BEVERAGE, FUEL_SUPPORT, …]
GET /api/v1/mobile/reference/countries               → 200  count=1   [TR]   (only Turkey seeded in locations; endpoint correct)
```
DB confirms 5/5/5 active, non-deleted rows for the 3 new groups. No 403 anywhere → role present.

## Files
**BFF (`Bff/src/Marine.Participant.Mobile/**`):**
- `Aizen.Bff.Marine.Participant.Mobile/Controllers/V1/ReferenceController.cs` (new)
- `Application/Contracts/Reference/ReferenceItemDto.cs` (new — `ReferenceItemDto` + `CountryItemDto`)
- `Application/Reference/Query/GetReferenceItems/{Query,Handler}.cs` (new)
- `Application/Reference/Query/GetCountries/{Query,Handler}.cs` (new)
- `Application/Common/RemoteClients/IReferenceDataRemoteCall.cs` (edit — `GetLookupItems` + `GetCountries`)

**ReferenceData (seed data):**
- `…Repository/Seed/Json/Lookup/lookup-items.json` (+15 items for FUEL_TYPE/ENGINE_TYPE/HULL_MATERIAL).

No Identity change. No BffAssertion needed (reads). Provider/Admin and the in-tree parallel work untouched.

## Scope note
The `addesso-project` working tree was committed by an external process mid-session, so `git status` is clean; the
M3b files above are tracked (the committed `lookup-items.json` contains the new FUEL_TYPE/ENGINE_TYPE/HULL_MATERIAL
items). FE repo (`inktavia-marine-mobile`) remains uncommitted.
