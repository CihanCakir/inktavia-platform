# Provider Onboarding Phase 2d — Fail-Closed + Ownership Check

**Date:** 2026-07-12
**Status:** All four defects fixed. Identity + BFF build with 0 errors. Smoke blocked by Docker OOM.

## Defects fixed

### Defect 1 (critical) — FAIL-OPEN → FAIL-CLOSED

**Before:** The `catch (Exception)` block swallowed FileStorage errors and proceeded with client-supplied data.
**After:** Any FileStorage call failure → `throw new AizenBusinessException("Unable to verify the file. Please try again.")`. The handler never proceeds with unvalidated data.

### Defect 2 (critical) — File ownership now checked

**Before:** `file.UploadedByUserId` was read and persisted but never compared to the caller. Provider B could attach provider A's file.
**After:**
- `UserId` added to `AttachProviderDocumentCommand`
- The BFF passes the caller's `UserId` from `IProviderIdentityHolder`
- The Identity controller maps it from `AttachProviderDocumentRequest.UserId`
- The handler checks:
  - `profile.UserId == request.UserId` (caller owns the profile)
  - `file.UploadedByUserId == request.UserId` (caller uploaded the file)
- Both checks fail-closed with clear error messages

### Defect 3 — `?? Guid.NewGuid()` fabrication removed

**Before:** `document.PublicId ?? Guid.NewGuid()` in the `LinkToOwner` call — fabricated a random Guid if the document had no PublicId.
**After:**
- `VerificationDocumentEntity.CreateFromBff` now assigns `PublicId = Guid.NewGuid()` at creation time
- `LinkToOwner` uses `document.PublicId ?? throw new InvalidOperationException(...)` — missing PublicId is a bug, not a silent fallback

### Defect 4 — Docstring matches code

The handler's XML doc now accurately describes the 12-step validation pipeline as implemented.

## Validation pipeline (complete)

| # | Check | Fail mode |
|---|-------|-----------|
| 1 | Profile exists + Organizer | Business exception |
| 2 | `profile.UserId == request.UserId` | "You do not have permission" |
| 3 | Onboarding not Submitted | Business exception |
| 4 | Duplicate FilePublicId (same profile) | Business exception |
| 5 | Cross-profile FilePublicId uniqueness | Business exception |
| 6 | FileStorage: file exists | Business exception (fail-closed) |
| 7 | **`file.UploadedByUserId == request.UserId`** | "You do not own this file" |
| 8 | Status ∈ {Uploaded, Ready} | Business exception |
| 9 | ContentType ∈ {pdf, jpeg, png} | Business exception |
| 10 | SizeInBytes ≤ 10 MB | Business exception |
| 11 | Persist with FileStorage snapshots | — |
| 12 | Claim via LinkToOwner (real PublicId) | Warning log (best-effort) |

## Build results

- Identity: **0 errors** ✓
- MarineProvider BFF: **0 errors** ✓

## Smoke

Blocked by Docker OOM (Keycloak container memory). Code is complete. When Docker stabilizes:

1. Provider B attaches provider A's fileId → should be **REJECTED** at step 7
2. FileStorage down + attach → should be **REJECTED** at step 6 (fail-closed)
3. UploadUrlGenerated file → rejected at step 8
4. Oversize/wrong type → rejected at steps 9-10
5. Happy path → success with real PublicId in FileStorage owner reference

## Still deferred (carried from 2c)

- Cleanup consumer for orphaned files
- Frontend signed-URL upload flow (Part F)
- Legacy `AddOrganizerVerificationDocumentCommandHandler` should delegate to the same validated path
