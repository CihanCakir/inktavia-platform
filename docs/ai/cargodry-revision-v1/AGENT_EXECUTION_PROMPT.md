# AGENT EXECUTION PROMPT — CargoDry Backend Revision v1

## Purpose

This package contains 4 revision prompts that improve the existing CargoDry module backend
(originally built from `cargodry-module-v1/`).

These prompts are **additive revisions**, not a full rebuild.
Run them in order after the original `cargodry-module-v1` prompts are complete.

---

## Pre-Conditions

Before running these prompts, confirm:
- [ ] `cargodry-module-v1` prompts (A through E) have been executed successfully
- [ ] `dotnet build` passes with 0 errors on the existing CargoDry solution
- [ ] PostgreSQL is accessible (for migration validation)
- [ ] MongoDB is available locally (Docker: `docker run -d -p 27017:27017 mongo:7`)

---

## Execution Order

### REV-A — Domain Audit Fixes (run first)
**File:** `PROMPT_REV_A_DOMAIN_AND_AUDIT.md`

Changes:
- All 5 domain entities: `AizenEntity<long>` → `AizenEntityWithAudit`
- Remove manual `CreatedAt` fields (provided by base)
- Add `[DocumentationInfo]` to all entities
- Update EF configurations (remove re-mapped audit fields)
- Remove `CargoDryActivationLogEntity` and its EF config (migrated to MongoDB in REV-B)
- Run migration: `AuditBaseRefactor`

**Estimated files changed:** 9 files

---

### REV-B — MongoDB Integration (run after REV-A)
**File:** `PROMPT_REV_B_MONGODB.md`

Changes:
- Add `CargoDryActivationLogDocument` MongoDB document
- Add `CargoDryKitUsageSnapshotDocument` MongoDB document
- Add `ICargoDryActivationLogRepository` + `ICargoDrySnapshotRepository` interfaces
- Implement both MongoDB repositories
- Add `MongoIndexBootstrap` class
- Add `DailySnapshotJob` background service
- Update `ActivateKitCommandHandler`, `RevokeKitCommandHandler`, `KitExpiredMarkingJob` to write MongoDB log documents
- Update `DependencyInjection.cs` (PostgreSQL + MongoDB DI)
- Add `MongoDB.Driver` package reference

**Estimated files changed:** 12 files (7 new, 5 modified)

---

### REV-C — Cacheable Queries (run after REV-B)
**File:** `PROMPT_REV_C_CACHEABLE_QUERIES.md`

Changes:
- Perform discovery grep for `AizenQueryHandlerCacheable` — use Section A or B accordingly
- `GetCargoDryStatsQueryHandler` → cacheable (2 min, key: `cargodry:stats:global`)
- New `GetCargoDryProductListQuery` + cacheable handler (30 min)
- `GetCargoDryAnalyticsQueryHandler` → cacheable (10 min)
- Add cache invalidation to `ActivateKitCommandHandler`, `RevokeKitCommandHandler`, `RenewKitCommandHandler`, `KitExpiredMarkingJob`, `DailySnapshotJob`
- Add `GET /api/v1/cargodry/admin/products` endpoint

**Estimated files changed:** 8 files (2 new, 6 modified)

---

### REV-D — Configuration Files (run after REV-C)
**File:** `PROMPT_REV_D_CONFIGURATION.md`

Changes:
- Create `Configuration/local.json`
- Create `Configuration/development.json`
- Create `Configuration/production.json`
- Update `.csproj` with content file declarations
- Replace `Program.cs` with full updated version (MongoDB + Cache + Config-driven rate limiting)

**Estimated files changed:** 5 files (3 new, 2 modified)

---

### REV-E — Response Model Standardization (run after REV-D)
**File:** `PROMPT_REV_E_RESPONSE_MODELS.md`

Changes:
- `RevokeKitCommand<bool>` → `RevokeKitCommand<RevokeKitResponse>` (create `RevokeKitResponse` DTO)
- `GetMyKitsQuery<List<CargoDryKitDto>>` → `GetMyKitsQuery<GetMyKitsResponse>` (create `GetMyKitsResponse` with aggregate counters)
- Update both handlers to return the new response types
- Update controller endpoints accordingly
- Verify zero `AizenCommand<bool>` or `AizenQuery<List<` patterns remain

**Estimated files changed:** 6 files (2 new, 4 modified)

---

## Final Verification

After all 4 prompts are complete:

```bash
# 1. Build
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.sln

# 2. Verify all entities use AizenEntityWithAudit
grep -r "AizenEntity<long>" Modules/CargoDry/src/ --include="*.cs"
# Expected: 0 results

# 3. Verify DocumentationInfo on all domain entities
grep -rL "DocumentationInfo" Modules/CargoDry/src/*/Domain/Entities/ --include="*.cs"
# Expected: 0 results (all entities have the attribute)

# 4. Verify MongoDB document classes exist
ls Modules/CargoDry/src/Aizen.Modules.CargoDry.Domain/MongoDocuments/
# Expected: CargoDryActivationLogDocument.cs, CargoDryKitUsageSnapshotDocument.cs

# 5. Verify configuration files exist
ls Modules/CargoDry/src/Aizen.Modules.CargoDry/Configuration/
# Expected: local.json, development.json, production.json

# 6. Verify DailySnapshotJob registered
grep -r "DailySnapshotJob" Modules/CargoDry/src/ --include="*.cs"
# Expected: 2+ results (definition + DI registration)

# 7. Run migration (PostgreSQL must be up)
ASPNETCORE_ENVIRONMENT=local dotnet ef database update \
  --project Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository \
  --startup-project Modules/CargoDry/src/Aizen.Modules.CargoDry
```

---

## Architecture Summary After Revisions

```
CargoDry Module — Storage Architecture
├── PostgreSQL (schema: cargodry)
│   ├── products         ← CargoDryProductEntity (AizenEntityWithAudit)
│   ├── batches          ← CargoDryBatchEntity (AizenEntityWithAudit)
│   ├── kits             ← CargoDryKitEntity (AizenEntityWithAudit)
│   └── renewals         ← CargoDryRenewalEntity (AizenEntityWithAudit)
│
├── MongoDB (database: aizen_cargodry)
│   ├── cargodry_activation_logs   ← CargoDryActivationLogDocument (append-only)
│   └── cargodry_usage_snapshots   ← CargoDryKitUsageSnapshotDocument (daily upsert)
│
└── Redis
    ├── cargodry:stats:global        ← Stats cache (TTL: 2 min)
    ├── cargodry:products:all        ← Product list cache (TTL: 30 min)
    ├── cargodry:analytics:snapshot  ← Analytics cache (TTL: 10 min)
    └── cargodry:jti:{uuid}          ← Activation token JTI blacklist (TTL: 6 min)
```
