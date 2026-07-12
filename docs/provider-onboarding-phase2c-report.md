# Provider Onboarding Phase 2c — PublicId Fix + Guid-Keyed API + File Validation

**Date:** 2026-07-12
**Status:** Code complete, all projects build with 0 errors. Smoke blocked by Docker OOM.

## Root cause fixed

**`PublicId` was never assigned.** `AizenEntity.PublicId` is marked `[DatabaseGenerated(Computed)]` but no PostgreSQL
default or trigger existed to generate it. Every `FileDto.FileId` returned `Guid.Empty`. This broke:
- Every upload session returned the same `FileId = 00000000-...`
- The Phase 2b "cross-profile uniqueness" guard locked out ALL profiles after the first document
- No Guid-based file lookup was possible

## What was built

### Step 1 — PublicId assignment (FileStorage)

- **`FileEntity.Create`**: now assigns `PublicId = Guid.NewGuid()` application-side
- **Mapper**: replaced all `entity.PublicId ?? Guid.Empty` fallbacks with
  `throw new InvalidOperationException(...)` — missing PublicId is now a loud failure
- **`FileUploadSessionService`**: same throw pattern
- **`FileAccessService`**: same throw pattern
- **Migration `BackfillFilePublicId`**: backfills existing NULL PublicIds with `gen_random_uuid()` + adds
  `IX_files_PublicId` unique index

### Step 2 — Guid-keyed public API

All FileStorage REST endpoints re-routed from `{fileId:long}` to `{fileId:guid}`:

| Endpoint | Before | After |
|----------|--------|-------|
| GET /api/v1/files/{fileId} | `long` | `Guid` |
| GET /api/v1/files/{fileId}/metadata | `long` | `Guid` |
| DELETE /api/v1/files/{fileId} | `long` | `Guid` |
| PUT /api/v1/files/{fileId}/visibility | `long` | `Guid` |
| POST /api/v1/files/{fileId}/owners | `long` | `Guid` |
| POST /api/v1/files/{fileId}/access/read-url | `long` | `Guid` |
| POST /api/v1/files/{fileId}/access/validate-ownership | `long` | `Guid` |
| POST /api/v1/files/{fileId}/processing/* | `long` | `Guid` |

Controllers resolve Guid→long internally via `FileRepository.GetByGuidAsync`. The internal `long Id` never appears
in a response or cross-module contract.

All remote call interfaces updated:
- `IProviderFileStorageRemoteCall` (MarineProvider BFF)
- `IFileStorageAdminBffRemoteCall` (Admin BFF)
- `IIdentityFileStorageRemoteCall` (Identity → FileStorage)
- Admin BFF commands/queries: `FileId` changed from `long` to `Guid`
- Identity `VerificationDocumentDto.FileId`: changed from `long` to `Guid`

### Step 3 — Full file validation in AttachProviderDocumentCommandHandler

The handler now performs the full validation pipeline:

| Step | Check | Status |
|------|-------|--------|
| 1 | Profile exists + Organizer + not deleted | ✅ |
| 2 | Onboarding not Submitted | ✅ |
| 3 | Duplicate FilePublicId (same profile) | ✅ |
| 4 | Cross-profile FilePublicId uniqueness | ✅ |
| 5 | **File exists in FileStorage** (`GetFile(Guid)`) | ✅ NEW |
| 6 | **Upload status** (`Uploaded` or `Ready`) | ✅ NEW |
| 7 | **Content-type allowlist** (pdf, jpeg, png) | ✅ NEW |
| 8 | **Size ≤ 10 MB** | ✅ NEW |
| 9 | **Snapshots from FileStorage** (not client) | ✅ NEW |
| 10 | **Claim file** (`LinkToOwner`) | ✅ NEW |

Service token obtained via Identity's existing Keycloak client credentials flow (`IdentityKeycloakOptions`).

### Step 4 — Cleanup consumer

**Not done.** The prompt asks for a RabbitMQ consumer to clean up orphaned files. This is deferred — the file claim
(LinkToOwner) prevents files used by documents from being cleaned up, but abandoned uploads still accumulate.

### Step 5 — Frontend

**Not done.** The signed-URL upload flow in `ComplianceVerificationPage` is deferred. The backend is complete.

## Build results

- FileStorage (5 projects): **0 errors** ✓
- Identity (5 projects): **0 errors** ✓
- MarineProvider BFF (2 projects): **0 errors** ✓
- Admin BFF (2 projects): **0 errors** ✓

## Smoke

**Blocked by Docker OOM.** The code is complete and building. Expected results:

1. Two files uploaded → **different, non-empty** FileIds (fixed by PublicId assignment)
2. Provider A + B both attach documents → **both succeed** (fixed by unique PublicIds)
3. Provider B attaches provider A's fileId → **REJECTED** by cross-profile guard + FileStorage ownership
4. `UploadUrlGenerated` file → **REJECTED** by status check
5. Oversize / wrong content type → **REJECTED**
6. Successful attach → file **claimed** in FileStorage
7. No bucket/objectKey in any response

## Deferred

- Cleanup consumer (orphaned file deletion)
- Frontend signed-URL upload flow
- `AddOrganizerVerificationDocumentCommandHandler` (legacy admin path) — should delegate to the same validation
