# Vessel Demo Seed Final Gap Report

**Date**: 2026-06-19  
**Branch**: feature/service-request-registration  
**Scope**: Known gaps, risks, and follow-up tasks after Vessel demo seed implementation

---

## 1. Implemented in This Session

| Item | Status |
|------|--------|
| `vessel-media.json` — 12 media records (IDs 24001–24012) | ✅ Done |
| `vessel-documents.json` — 16 document records (IDs 25001–25016) | ✅ Done |
| `vessel-location-snapshots.json` — 4 location snapshots (IDs 26001–26004) | ✅ Done |
| `VesselMockDataSeeder.cs` — SeedMediaAsync, SeedDocumentsAsync, SeedLocationSnapshotsAsync | ✅ Done |
| `VesselMockDataSeeder.cs` — Updated AdvanceSequencesAsync for 3 new tables | ✅ Done |
| Build validation: 0 errors | ✅ Done |

---

## 2. Known Gaps

### P1 (Required for smoke tests to fully pass)

| Gap | Description | Workaround |
|-----|-------------|------------|
| `FileId` is null on all media/document seed records | No real FileStorage objects exist for demo data. `AccessUrl` will be null in API responses. | Acceptable for local dev — UI should handle null AccessUrl gracefully. |

### P2 (Minor, low impact)

| Gap | Description |
|-----|-------------|
| `VesselMediaEntity.Title`, `Description`, `ThumbnailUrl` not seeded | No public setter or domain method. These are null in seed records. |
| `VesselDocumentEntity.IssuingAuthority`, `DocumentCategory` not seeded | No public setter or domain method. Not exposed via BFF. |
| `IssuingAuthority` in JSON is ignored | Stored in seed model but not set on entity. |
| Document version history not seeded | `VesselDocumentEntity` has no version sub-entity in current schema — not a gap in schema, just in scope. |

### P3 (Future work)

| Gap | Description |
|-----|-------------|
| Vessels 20004, 20006, 20007, 20008 have no media seed data | Only first 4 distinct vessels (20001, 20002, 20003, 20005) have media |
| Vessels 20006, 20007, 20008 have no document seed data | Only 4 vessels covered |
| Only 4 of 8 vessels have location snapshots | Vessels 20005–20008 have no snapshot |

---

## 3. Migration Status

| Migration | Status |
|-----------|--------|
| `20260602083310_InitialVessel` | ✅ Applied (contains vessel_media, vessel_documents, vessel_location_snapshots tables) |
| `20260602135654_AddVesselFileStorageIntegration` | ✅ Applied |
| `20260619075529_AddVesselUiContractFields` | ✅ Applied |

---

## 4. Risks

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Seed data depends on stable vessel IDs (20001–20008) already in DB | Medium | If DB is fresh, seed runs in correct order. If vessels were inserted with different IDs, media/document/location FKs will fail with FK constraint error — caught by try/catch and logged. |
| Service Request seeder must run after Vessel seeder | Low | Each module seeds independently at startup. FK checks will skip if vessel doesn't exist yet. Re-running ServiceRequest API after Vessel API starts resolves this. |
| Sequence advance SQL uses table names with quotes | Low | SQL uses `GREATEST()` to avoid lowering sequence if already higher. Non-fatal if table name differs. |

---

## 5. Follow-up Tasks

1. Add `ThumbnailUrl` and `Title` to `VesselMediaEntity.Create()` factory (optional enhancement)
2. Add `IssuingAuthority` domain method or include in `Update()` signature
3. Expand media/document/location seed coverage to all 8 vessels
4. Implement FileStorage stub for local dev to return placeholder AccessUrls for demo FileIds
