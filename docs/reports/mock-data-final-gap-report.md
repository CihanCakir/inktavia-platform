# Mock Data Final Gap Report

**Date**: 2026-06-14  
**Scope**: Outstanding gaps, risks, and follow-up items for the admin-demo mock data seeding implementation

---

## 1. Implementation Summary

The following was delivered:

| Item | Status |
|------|--------|
| Identity module mock seeder | ✅ Complete |
| Vessel module mock seeder | ✅ Complete |
| ServiceRequest module mock seeder | ✅ Complete |
| Cross-module ID manifest | ✅ Complete |
| Startup wiring with environment guards | ✅ Complete |
| Production-safe configuration defaults | ✅ Complete |
| Build validation (0 errors) | ✅ Complete |
| 8 documentation reports | ✅ Complete |

---

## 2. Known Gaps

### 2.1 FileStorage Mock Data — Not Seeded

**Impact**: Medium  
**Reason**: FileStorage documents and media require actual uploaded file records with S3/MinIO object keys. This cannot be faked without a running MinIO instance or pre-created fixture files.  
**Affected entities**:
- `VesselDocumentEntity.FileId` — set to placeholder string in vessel seed
- `VesselMediaEntity.FileId` — not seeded at all
- `ServiceRequestAttachmentEntity` — not seeded
- `ServiceRequestWorkLogEntity.AttachmentFileId` — null in seed data
- `ServiceRequestCompletionEntity.EvidenceFileId` — null in seed data

**Follow-up**: Create a FileStorage mock seeder that inserts `StoredFile` records with MinIO object keys pointing to static fixture files bundled with the FileStorage module.

---

### 2.2 Keycloak Subject IDs — Not Set

**Impact**: Low (for admin panel demo; medium for auth flows)  
**Reason**: `UserEntity.KeycloakSubjectId` is not set in mock users. If an admin panel feature depends on Keycloak subject lookup, it will fail for mock users.  
**Follow-up**: Create a Keycloak realm import JSON (`keycloak-admin-demo-users-import.json`) with matching UUIDs and map `KeycloakSubjectId` in the Identity mock data.

---

### 2.3 VesselDocumentEntity — Not Seeded

**Impact**: Low  
**Reason**: Depends on FileStorage. See gap 2.1.  
**Follow-up**: After FileStorage mock seeder is implemented, add vessel documents referencing the mock file IDs.

---

### 2.4 VesselMediaEntity — Not Seeded

**Impact**: Low (admin panel can show vessels without photos)  
**Reason**: Depends on FileStorage. See gap 2.1.  
**Follow-up**: Same as 2.3.

---

### 2.5 VesselLocationSnapshotEntity — Not Seeded

**Impact**: Low  
**Reason**: Not identified as a critical demo scenario in the orchestration prompt.  
**Follow-up**: Can be added with static Bodrum/Marmara/Istanbul marina GPS coordinates.

---

### 2.6 VesselStatusHistoryEntity — Not Seeded

**Impact**: Low  
**Reason**: Not identified as critical; SR status history was prioritized.  
**Follow-up**: Add 2-3 status transition entries per vessel with realistic reasons.

---

### 2.7 Open Status (SR Code 10) — Not Covered

**Impact**: Very Low  
**Reason**: All 12 demo SRs skip the `Open=10` transient state (they go directly to `WaitingForOffer=11`).  
**Follow-up**: Add one SR in the `Open` state if the admin panel has a specific view for Open requests.

---

### 2.8 ReferenceData Not Validated Against Mock Service Categories

**Impact**: Low  
**Reason**: Mock SRs use `ServiceCategoryCode` values (e.g., "MAINTENANCE", "CLEANING", "ENGINE_REPAIR"). These codes must exist in the ReferenceData module's lookup tables.  
**Follow-up**: Validate that the ReferenceData seed includes the service category codes used in mock SRs. If not, add them to the ReferenceData seed.

---

### 2.9 No Rollback / Reset Mechanism

**Impact**: Low (dev workflow)  
**Reason**: If mock data needs to be cleared (e.g., after a corrupt run), there is no automated reset.  
**Follow-up**: Consider adding a `MockDataCleanupService` that deletes records in the mock ID ranges (10001-99999) when triggered by a CLI flag or environment variable.

---

### 2.10 No Seed CLI Command

**Impact**: Low  
**Reason**: Seeding only runs at application startup. There is no `dotnet run --seed` equivalent.  
**Follow-up**: Optionally expose seed as an `IHostedService` command or a minimal API endpoint guarded by `Local` environment.

---

### 2.11 Profile Module Not Activated

**Impact**: Not applicable  
**Reason**: Profile module is in the inactive/future list. Mock users have no `Profile` module records.  
**Follow-up**: When Profile module is activated, extend the mock seeder to create matching profile entries.

---

### 2.12 Cross-Service Startup Order Not Enforced

**Impact**: Low (soft references only)  
**Reason**: Vessel and ServiceRequest reference Identity UserIds as soft references (no cross-DB FK). If Identity hasn't seeded yet when Vessel starts, vessel owner records reference non-existent users. In practice, all 3 services are typically started together or in sequence.  
**Follow-up**: Document the recommended local startup order: Identity → Vessel → ServiceRequest.

---

## 3. Security Notes

| Item | Status |
|------|--------|
| No secrets in JSON seed files | ✅ Passwords are in plaintext in appsettings only (`Test!123`, `Admin!123`) — these are hashed at seed time |
| No production appsettings enables seeding | ✅ `Enabled: false` in base `appsettings.json` |
| No raw tokens stored in Redis during seeding | ✅ Not applicable |
| FileStorage S3 credentials not in seed | ✅ Placeholder strings used |

---

## 4. Recommended Follow-up Priority

| Priority | Item |
|----------|------|
| High | FileStorage mock seeder (vessel documents + media, SR attachments) |
| High | Keycloak subject ID mapping for mock users |
| Medium | ReferenceData service category code validation |
| Medium | Vessel status history entries |
| Low | Vessel location snapshots |
| Low | VesselDocumentEntity (after FileStorage mock seeder) |
| Low | Mock data cleanup mechanism |
| Low | SR Open status coverage |
| Low | Seed CLI command |

---

## 5. Files Delivered — Complete List

### Cross-module
- `docs/mock-data/admin-demo/admin-demo-id-manifest.json`

### Identity module (7 files)
- `Modules/Identity/src/Aizen.Modules.Identity.Repository/Context/Seed/MockData/IdentityMockDataSeeder.cs`
- `Modules/Identity/src/Aizen.Modules.Identity.Repository/Seed/MockData/MockDataSeedOptions.cs`
- `Modules/Identity/src/Aizen.Modules.Identity.Repository/Seed/Json/MockData/admin-demo/identity-users.json`
- `Modules/Identity/src/Aizen.Modules.Identity.Repository/Seed/Json/MockData/admin-demo/identity-profiles.json`
- `Modules/Identity/src/Aizen.Modules.Identity/configuration/appsettings.json` (modified)
- `Modules/Identity/src/Aizen.Modules.Identity/configuration/appsettings.Local.json` (modified)
- `Modules/Identity/src/Aizen.Modules.Identity.Repository/DependencyInjection.cs` (modified)

### Vessel module (8 files)
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/MockDataSeedOptions.cs`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessels.json`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-owners.json`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-specifications.json`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-engines.json`
- `Modules/Vessel/src/Aizen.Modules.Vessel/configuration/appsettings.json` (modified)
- `Modules/Vessel/src/Aizen.Modules.Vessel/configuration/appsettings.Local.json` (modified)
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/DependencyInjection.cs` (modified)

### ServiceRequest module (16 files)
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/MockDataSeedOptions.cs`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-requests.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-items.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-offers.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-offer-items.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-assignments.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-worklogs.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-completions.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-disputes.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-messages.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-status-history.json`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/configuration/appsettings.json` (modified)
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/configuration/appsettings.Local.json` (modified)
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/DependencyInjection.cs` (modified)

### Reports (8 files under `docs/reports/`)
- `mock-data-module-entity-audit-report.md`
- `mock-data-cross-module-id-manifest-report.md`
- `identity-mock-data-seeding-report.md`
- `vessel-mock-data-seeding-report.md`
- `service-request-mock-data-seeding-report.md`
- `mock-data-startup-import-report.md`
- `admin-panel-demo-data-validation-report.md`
- `mock-data-final-gap-report.md` (this file)
