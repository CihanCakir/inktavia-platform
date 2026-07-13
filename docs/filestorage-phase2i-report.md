# FileStorage Phase 2i — Close the Gaps

**Date:** 2026-07-13
**Status:** Items 1-5 complete. All projects build with 0 errors. 14 tests pass.

## Item 1 — Phantom claims fixed

**Strategy: persist first, claim second (defence in depth at the source).**

`FileAttachmentValidationService` was reordered:
1. Persist the document (`SaveChangesAsync`) — if this fails, no claim exists
2. Claim the file (`LinkToOwner`) — if this fails, roll back the document row (`Remove` + `SaveChangesAsync`)
3. Rollback failures are logged but don't mask the original error

A failed persist never creates a phantom claim. A failed claim removes the document row, leaving the file
unclaimed for the cleanup sweep to reap.

**Phantom claim detection in cleanup:** documented as TODO in `OrphanFileCleanupConsumer`. FileStorage cannot
query Identity's tables; detection would require a cross-module RPC to verify the `OwnerEntityId` still exists.
The source-side fix is the primary defence.

## Item 2 — Size lie is now rejected

`CompleteUploadAsync` now rejects when the actual object size deviates from the declared size by more than 20%:
- `RejectAndDeleteAsync` + `InvalidOperationException` (not just `LogWarning`)
- **Tolerance: 20%** — JPEG compression variance and HTTP Content-Length differences justify this margin.
  Tighter would reject legitimate uploads; wider would let too many lies through.
- Actual size is always persisted via `UpdateActualSize` for files that pass

## Item 3 — Upload-session rate limiting

Per-provider limits added to `CreateUploadSessionCommandHandler` (BFF):
- **Sessions per hour:** max 20, tracked in Redis (`upload:session:{userId}`, 1h TTL)
- **Total pending bytes:** max 100 MB, tracked in Redis (`upload:bytes:{userId}`, 1h TTL)
- Fails open on cache errors (logged but doesn't block — same pattern as OTP login throttling)
- Counters incremented only after successful session creation

## Item 4 — Dead consumer audit

All 6 surviving consumers verified against live publishers:

| Consumer | Publisher | Verdict |
|----------|----------|---------|
| `CreateFileReadUrlConsumer` | `MessagingFileStorageService` | **KEPT** — live publisher |
| `CreateUploadSessionConsumer` | `MessagingFileStorageService` | **KEPT** |
| `CompleteUploadSessionConsumer` | `MessagingFileStorageService` | **KEPT** |
| `FileDeletedConsumer` | `DeleteFileCommandHandler` | **KEPT** |
| `FileLinkedToOwnerConsumer` | `LinkFileToOwnerCommandHandler` | **KEPT** |
| `FileProcessingRequestedConsumer` | `StartFileProcessingCommandHandler` | **KEPT** |

No deletions needed — every consumer has a live publisher.

## Item 5 — Tests

Created `Modules/FileStorage/tests/Aizen.Modules.FileStorage.Tests/` (xUnit + FluentAssertions):

| Test | Coverage | Result |
|------|----------|--------|
| Entity constructor guard | Scans FileStorage + Identity assemblies for private parameterless ctors | ✅ Pass |
| FileEntity.Create assigns PublicId | Non-null, non-empty Guid | ✅ Pass |
| FileEntity.Create sets Created status | Initial status | ✅ Pass |
| MarkRejected sets Rejected | Status transition | ✅ Pass |
| PromoteToReady sets Ready | Status transition | ✅ Pass |
| MarkQuarantined sets Quarantined | Status transition | ✅ Pass |
| UpdateActualSize persists real size | Size mutation | ✅ Pass |
| Uploaded is readable | Read gate allow | ✅ Pass |
| Ready is readable | Read gate allow | ✅ Pass |
| Quarantined is blocked | Read gate deny | ✅ Pass |
| Rejected is blocked | Read gate deny | ✅ Pass |
| Deleted is blocked | Read gate deny | ✅ Pass |
| Created is blocked | Read gate deny | ✅ Pass |
| UploadUrlGenerated is blocked | Read gate deny | ✅ Pass |

**14 passed, 0 failed.**

## Item 6 — Browser smoke

**Cannot be performed from this CLI session.** The provider-web SPA requires a real browser with keycloak-js,
CORS, and the full PKCE flow. The prompt-level tests (SigV4, Content-Type dedup, resolver, Ready gate) were
verified in a real browser during the Phase 2g/2h fixes. This report does not substitute curl for browser
verification.

## Build results

| Project | Result |
|---------|--------|
| FileStorage | **0 errors** ✓ |
| Identity | **0 errors** ✓ |
| MarineProvider BFF | **0 errors** ✓ |
| Tests (14) | **All pass** ✓ |

## Still deferred

- Phantom claim detection in cleanup sweep (cross-module RPC to verify owner entity exists)
- Browser E2E re-verification (requires real browser session)
