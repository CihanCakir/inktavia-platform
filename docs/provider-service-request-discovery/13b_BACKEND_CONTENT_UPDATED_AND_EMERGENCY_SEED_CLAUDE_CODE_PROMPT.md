# 13b — Backend: precise "updated" signal + an emergency seed (Claude Code)

Self-contained. Fixes the `IsUpdated` field from 13a, which is **always true**, and adds one **Emergency** seed
request so the card's ACİL state is visible. Do not touch the frontend.

## Why `IsUpdated` is wrong (not just approximate — always true)

13a derived `IsUpdated = ModifyDate > PublishedAt`. Observed in the browser: **every** request shows GÜNCELLENDİ.

Root cause: `PublishServiceRequestCommandHandler` calls `entity.Publish()` (which sets `PublishedAt`) and then
`_repository.Update(entity)`. The audit interceptor stamps `ModifyDate` at save time — a few milliseconds **after**
`PublishedAt` was set in the same handler. So `ModifyDate > PublishedAt` holds for **every published request by
construction**. The chip is meaningless.

The cheap derivation cannot work, because publishing the row *is* a modification of the row. We need a field that
moves **only** on an owner content edit.

## Fix — a precise `ContentUpdatedAt`

Verified in source:
- `ServiceRequestEntity.UpdateProfile(...)` (line ~124) is the **owner content edit** (title, description,
  category/type, dates, location). Called from `UpdateServiceRequestCommandHandler`.
- `ServiceRequestEntity.Publish()` (line ~158) is separate and must **not** set the new field.

Do:

1. Add `public DateTime? ContentUpdatedAt { get; private set; }` to `ServiceRequestEntity`.
2. Set it **inside `UpdateProfile(...)`**: `ContentUpdatedAt = DateTime.UtcNow;`. Do not set it in `Publish()`,
   `ChangeStatus()`, offer flows, or anywhere else — only a genuine owner edit lights it up.
3. **Migration**: add the nullable `ContentUpdatedAt` column. No backfill — existing rows are correctly "not
   edited since publish" (null).
4. Change the projection in `ServiceRequestRepository.GetDiscoveryAsync`:
   ```csharp
   // Precise: set only by the owner-edit command (UpdateProfile), never by publish. So a freshly published,
   // untouched request is NOT "updated".
   IsUpdated = x.ContentUpdatedAt != null && x.PublishedAt != null && x.ContentUpdatedAt > x.PublishedAt,
   ```
   (Guarding on `> PublishedAt` too, so an edit made while still a Draft — before publication — does not count.)
5. Remove the "approximate" code comment from 13a; replace with a one-line note that this is now precise.

The BFF DTO and mapping already carry `IsUpdated` — no change there.

## Emergency seed — one request, so the ACİL card is visible

The mock seeder is `Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs`
(+ `MockDataSeedOptions.cs`). Add **one** seed request with:

- `Priority = ServiceRequestPriority.Emergency`
- `Status` = a biddable status (`Open`) with `PublishedAt` set (so it appears in discovery)
- `LocationCityCode = "35"` (Izmir — the test provider's city, so provider2 sees it), a marina name, and
  **coordinates** near Izmir (e.g. Çeşme `38.32, 26.30`) so it also shows on the map and gets a distance
- a realistic title/description (e.g. "Acil: Dümen sistemi arızası — Çeşme")
- `ServiceCategoryCode` / `ServiceTypeCode` that exist in ReferenceData (reuse codes other seeds use)
- a real `VesselId` if the seeder references vessels, else leave the vessel snapshot fields null

**Idempotent**: guard on a fixed `RequestCode` (e.g. `SR-SEED-EMERGENCY-1`) so re-running the seeder does not
create duplicates — follow whatever duplicate-guard the existing seeder already uses.

Do **not** invent a bid-on ("TEKLİF VERİLDİ") seed — that state is better produced by actually submitting an
offer through the flow, and a seeded offer needs a valid provider profile id. Note this in the report; the human
can create it by bidding.

## Acceptance — observed, not asserted

- After the fix: a freshly published, unedited request shows **`isUpdated = false`**. Edit one through the update
  command and it flips to **`true`**. Paste both rows.
- The emergency seed appears in `provider2`'s discovery (city 35), with `priority = "Emergency"`, on the map, and
  — when the browser sends a location near Izmir — with a distance.
- Re-running the seeder does not duplicate it.
- Build succeeds; discovery still returns rows.

## Constraints

- `ContentUpdatedAt` set **only** in `UpdateProfile`. Not in publish, status change, or any offer/assignment flow.
- Migration is additive, nullable, no backfill.
- Seed is idempotent and uses a real ReferenceData city/category; unknown codes are rejected elsewhere, so use
  ones that exist.
- No frontend changes. No `object`/`JsonElement` on the wire.

## Report

Append to `REPORT_BACKEND_09c.md` (section "13b"): the before/after `isUpdated` rows, the emergency seed's
RequestCode and that it appears in discovery, and confirmation that publishing no longer sets `ContentUpdatedAt`.
Unfinished is **not done**.
