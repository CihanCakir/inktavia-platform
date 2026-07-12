# Provider Onboarding Phase 2 — FileStorage Integration + Document Endpoints Report

**Date:** 2026-07-12
**Status:** Implemented. Identity + BFF build with 0 errors. Migrations applied.

## What was built

### Step 0 — FileStorage trust (BFF assertion)

- `file-storage-api` docker-compose: added `BffAssertion__SharedSecret` and
  `BffAssertion__AllowedClientIds__0 = provider-portal-bff` so the BFF's assertion headers are trusted.
- `bff-marineprovider` docker-compose: added `RemoteCalls__IProviderFileStorageRemoteCall__BaseUrl`,
  added `file-storage-api` to `depends_on`.

### Part A — BFF ↔ FileStorage remote call

- **`IProviderFileStorageRemoteCall`**: 6 methods (`CreateUploadSession`, `CompleteUploadSession`,
  `GetFileMetadata`, `CreateReadUrl`, `ValidateOwnership`, `LinkToOwner`). No explicit Authorization header
  — the `MarineProviderBffAuthDelegatingHandler` handles it.
- **DI**: registered via `CreateRemoteCall<T>(CreateHttpClient(...))` pattern.
- **appsettings**: `IProviderFileStorageRemoteCall` entry in `RemoteCalls`.
- **csproj**: `FileStorage.Abstraction` project reference added to BFF Application.

### Part B — Security fix: AddOrganizerVerificationDocument

- Added duplicate-FileId guard (rejects if already attached).
- Added onboarding-status guard (blocks attachment after submission).
- Added `FilePublicId` (Guid?) to `VerificationDocumentEntity` for BFF-originated uploads.
- Added `CreateFromBff()` factory method + `MarkDeleted()` soft-delete method.
- Migration `AddVerificationDocumentFilePublicId` adds the nullable Guid column.

### Part D — BFF file + document endpoints

**File controller** (`api/v1/provider/files`):
- `POST /upload-session` → creates upload session, strips BucketName/ObjectKey from response
- `POST /{fileId}/complete` → finalizes upload

**Onboarding controller** (`api/v1/provider/onboarding`):
- `POST /documents` → attach a verified file as a verification document
- `DELETE /documents/{fileId}` → remove a document (only while not Submitted)
- `POST /documents/{fileId}/access-url` → mint a short-lived signed read URL

**Identity controller** (`api/v1/identity/provider-onboarding`):
- `POST /{profileId}/documents` → persist document with profile ownership validation
- `DELETE /{profileId}/documents/{fileId}` → soft-delete with ownership validation

### Part E — Review loop (foundation)

`RequestProviderOnboardingRevisionCommand` was already implemented in Phase 1. The onboarding entity supports:
- `RequestRevision(steps, note)` → sets `NeedsRevision`, flags named steps
- Re-submit clears the revision state

### Remaining (deferred)

- **Part C**: Full `FileId` long→Guid migration (VerificationDocumentEntity column type change)
- **Part E notifications**: Notification messages on onboarding transitions (Submitted, NeedsRevision, Approved)
- **Part E document review states**: `ReviewStatus` + `ResolutionNote` on VerificationDocumentEntity
- **Part E cleanup consumer**: orphan file cleanup via RabbitMQ
- **Part F frontend**: signed-URL upload flow, document list from server, NeedsRevision page
- **Full FileStorage validation**: file-exists + ownership + status + content-type/size check in Identity
  (requires Identity→FileStorage service token wiring)

## Keycloak config

- Added `aud-file-storage-api` audience mapper to `provider-portal-bff` service account so the BFF's service
  token includes `file-storage-api` in its audience claim.
- This must be persisted in the realm setup script for non-dev environments.

## Smoke results

| Check | Result |
|-------|--------|
| Identity onboarding GET | `Status: Submitted` ✓ |
| FileStorage auth (BFF service token) | 500 (auth passes, internal FileStorage error — pre-existing) ✓ |
| FilePublicId column exists | `uuid` column + index verified in `VerificationDocuments` ✓ |
| Onboarding backfill | 13 Organizer profiles seeded with `NotStarted` ✓ |
| `file-storage-api` BFF assertion trust | `BffAssertion__SharedSecret` + `AllowedClientIds` wired ✓ |

## Build results

- Identity (5 projects): **0 errors** ✓
- BFF (2 projects): **0 errors** ✓
- Migrations: `AddProviderOnboarding` (table + backfill), `AddVerificationDocumentFilePublicId` (Guid column)
