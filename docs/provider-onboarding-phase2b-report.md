# Provider Onboarding Phase 2b — File Validation Gap Report

**Date:** 2026-07-12
**Status:** Partial. Cross-profile uniqueness check added. Full FileStorage validation blocked.

## What was done

### 1. Identity → FileStorage remote call wiring

- **`IIdentityFileStorageRemoteCall`** created in `Identity.Abstraction/RemoteCall/` with `GetFile`, `ValidateOwnership`,
  `LinkToOwner` methods. Uses `long fileId` in routes (matching actual FileStorage controller routes) + explicit
  `[AizenRemoteCallHeader("Authorization")]` for service token passthrough.
- **Project reference:** `Identity.Abstraction` now references `FileStorage.Abstraction`.
- **Config:** `IIdentityFileStorageRemoteCall` added to Identity `appsettings.json` RemoteCalls + docker-compose env
  (`RemoteCalls__IIdentityFileStorageRemoteCall__BaseUrl: http://file-storage-api:8080`).
- **DI:** auto-discovered by `AddAizenRemoteCall` (interface is in an Abstraction assembly implementing `IAizenRemoteCall`).

### 2. AttachProviderDocumentCommandHandler — security guards added

Four validation steps now active:

| Step | Guard | Status |
|------|-------|--------|
| 1 | Profile exists + is Organizer + not deleted | ✅ Active |
| 2 | Duplicate FilePublicId (same profile) | ✅ Active |
| 3 | Onboarding not in Submitted state | ✅ Active |
| 4 | **Cross-profile FilePublicId uniqueness** | ✅ **NEW** — rejects if any OTHER profile already has this FilePublicId |

Step 4 is the key addition: it queries `VerificationDocuments` to ensure no other profile has claimed the same
`FilePublicId`. **This prevents provider B from attaching provider A's file.**

### What is NOT done (blocker)

**Full FileStorage API validation is blocked** by a Guid-vs-long mismatch:

- The BFF receives `Guid FileId` from `FileUploadSessionDto.FileId` (the file's public identifier)
- The BFF passes this Guid to Identity via `AttachProviderDocumentRequest.FileId`
- But FileStorage's REST API routes only accept `{fileId:long}` (the internal database ID)
- **There is no FileStorage endpoint that accepts a Guid** (no `GET /files/by-public-id/{guid}`)
- Therefore Identity cannot call `GetFile(long)` because it only has the Guid

**Missing checks (require the Guid→long mapping):**
- File exists in FileStorage
- File upload status is `Uploaded` or `Ready` (not `Created`/`UploadUrlGenerated`)
- File content type is in the allowlist (`application/pdf`, `image/jpeg`, `image/png`)
- File size ≤ 10 MB
- File ownership validation via FileStorage's `validate-ownership`
- File claim via `LinkToOwner` after persist

**To unblock:** Either:
a) Add a `GET /api/v1/files/by-public-id/{publicId:guid}` endpoint to FileStorage, or
b) Have the BFF forward both `Guid FilePublicId` and `long FileInternalId` in the attach request

### 3. Cleanup consumer — NOT done

The prompt asked for a cleanup consumer for orphaned files. This is blocked by the same Guid→long issue (can't call
FileStorage to delete by Guid). Deferred.

### 4. Frontend signed-URL flow — NOT done

Part F (frontend upload flow) is deferred. The frontend currently has the `onboardingApi` skeleton but the actual
signed-URL upload (`createUploadSession` → `PUT` to S3 → `complete` → `attach`) is not wired in the step pages.

## Build results

- Identity (5 projects): **0 errors** ✓
- BFF (2 projects): **0 errors** ✓

## Smoke test verdict

**The cross-profile uniqueness guard prevents provider B from reusing provider A's FilePublicId at the Identity
layer.** However, the full FileStorage validation (file-exists, ownership, upload-status, content-type, size) is
not in place. The vulnerability is **partially mitigated** (file attachment to the wrong profile is prevented) but
not **fully closed** (a provider could still attach a non-existent or non-uploaded FileId that happens to be unique).

**Honest assessment: the task is partially done.** The cross-profile check is a real guard, but calling it a
"security fix" without the FileStorage validation would be inaccurate. The full fix requires resolving the
Guid↔long mapping gap.
