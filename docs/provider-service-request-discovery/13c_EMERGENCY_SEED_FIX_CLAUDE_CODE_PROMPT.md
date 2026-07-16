# 13c — Emergency seed (the half of 13b that was skipped)

The `ContentUpdatedAt` half of 13b is done and verified — cards now show AÇIK, not GÜNCELLENDİ. **But the
emergency seed was never added.** Verified: `ServiceRequestMockDataSeeder.cs` has no Emergency request, and
`provider2`'s discovery returns 48 items, all Low/Normal/High — no Urgent/Emergency at all, so the ACİL card and
the "Acil Talepler" KPI can't be seen.

## Add exactly one Emergency seed

In `Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs`, add **one** request:

- `Priority = ServiceRequestPriority.Emergency`
- `Status = Open` **with `PublishedAt` set** (so it's biddable and appears in discovery — an unpublished row does
  not show)
- `LocationCityCode = "35"` (Izmir — provider2's city, so provider2 sees it)
- `LocationMarinaName` = e.g. "Çeşme Marina"; coordinates near Çeşme (`LocationLatitude = 38.32`,
  `LocationLongitude = 26.30`) so it also shows on the map and gets a distance
- a realistic title/description (e.g. "Acil: Dümen sistemi arızası — Çeşme")
- `ServiceCategoryCode` / `ServiceTypeCode` that already exist in ReferenceData — **reuse codes another seed in
  this file uses** (unknown codes are rejected elsewhere; do not invent one)
- a valid `VesselId` if the seeder references vessels; otherwise leave vessel fields null
- **Idempotent** on a fixed `RequestCode` (`SR-SEED-EMERGENCY-1`), following whatever duplicate-guard the seeder
  already uses for its other rows

## Check the seeder actually runs against the current DB

The mock seeder may only run on an empty database or behind a flag (`MockDataSeedOptions`). Confirm how it is
gated — if it skips when data already exists, adding the row won't help until it runs. If needed, make **this one
row** insert idempotently regardless of the "already seeded" guard (upsert on `RequestCode`), or document the
exact command to re-run seeding. State which you did.

## Acceptance — observed
- `GET /api/v1/provider/service-requests/discovery?sort=PriorityDesc` for provider2 returns the Emergency request
  **first**, with `priority = "Emergency"` and `locationCityCode = "35"`. Paste the row.
- Re-running the seeder does not create a duplicate.
- The "Acil Talepler" KPI (`emergencyCount`) is ≥ 1.

## Not in scope
- No "TEKLİF VERİLDİ" seed — that state is produced by actually submitting an offer through the flow (a seeded
  offer needs a valid provider profile id). Note it in the report; the human creates it by bidding.

## Report
Append to `REPORT_BACKEND_09c.md` (section "13c"): the seeded RequestCode, the discovery row proving it appears
for provider2, and how the seeder is gated / re-run. Unfinished is **not done**.
