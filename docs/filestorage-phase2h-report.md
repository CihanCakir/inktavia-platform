# FileStorage Phase 2h — Hardening + Dead-Code Cleanup

**Date:** 2026-07-13
**Status:** Complete. All 5 projects (FileStorage, Identity, MarineProvider BFF, Admin BFF, Vessel) build with 0 errors.

## Part A — Hardening

### A1. Content verification on complete (CRITICAL)

`CompleteUploadAsync` now verifies the uploaded object server-side instead of trusting the client:

| Check | Before | After |
|-------|--------|-------|
| Object exists | `ObjectExistsAsync` (boolean) | `GetObjectMetadataAsync` (actual metadata) |
| Actual size | Not checked | Persisted from S3; rejects if exceeds category cap |
| Size vs declared | Not compared | Warns if deviation > 10% |
| Content-type | Client-declared only | Verified from S3 metadata against allowlist |
| **Magic bytes** | Not checked | First 16 bytes sniffed: `%PDF-`, `\xFF\xD8\xFF` (JPEG), `\x89PNG...` |
| On failure | File stays | `MarkRejected()` + S3 object deleted |

A `.exe` renamed to `.pdf` is now rejected at `complete` time.

### A2. Document delete → unlink file

`RemoveProviderDocumentCommandHandler` now calls `IIdentityFileStorageRemoteCall.DeleteFile` after soft-deleting
the document row. FileStorage soft-deletes the file if no other owners hold it; the cleanup consumer (A3) handles
physical deletion.

### A3. Cleanup consumer for orphaned files

`OrphanFileCleanupConsumer` implemented:
- Queries files with no active `FileOwnerReference` older than configurable TTL (default 24h)
- Deletes S3 object first, then soft-deletes the file record
- Double-checks owner references before each deletion (race condition safety)
- Triggered by `OrphanFileCleanupRequestedMessage`

### A4. Quarantine / AV gate

- `IFileScanner` interface + `NoOpFileScanner` stub (always returns clean)
- `FileUploadedConsumer` → publishes `FileVirusScanRequestedMessage`
- `FileVirusScanRequestedConsumer` → calls scanner → `PromoteToReady()` or `MarkQuarantined()`
- `FileStatus.Quarantined = 9` added
- `FileEntity` gained `MarkQuarantined()` and `PromoteToReady()` methods

**Decision: `Uploaded` remains readable and attachable.** Providers must see their document immediately after
upload, before the scan completes. `Quarantined` files are blocked from both read URLs and attachment. With the
no-op stub, all files transition `Uploaded → Ready` immediately. When a real scanner (ClamAV) is wired in,
threatened files will be quarantined.

### A5. Read URL TTL

Already set to 5 minutes in Phase 2g. Confirmed.

### A6. Admin ownership fix

Replaced `skipOwnershipCheck` with `actorIsAdmin`:
- Admin: skips `profile.UserId == callerUserId` but still checks `file.UploadedByUserId == profile.UserId`
- Non-admin with `callerUserId <= 0`: rejected outright ("Invalid caller identity.")
- An admin cannot attach user B's file to user A's profile

### A7. Rate limit on upload sessions

**Not done.** Deferred — requires per-provider tracking in Redis; the existing `pwd-recovery-ip` limiter is
IP-based and doesn't fit the per-provider model without additional wiring.

## Part B — Dead-Code Cleanup

### Deleted (23 files)

**No-op consumers deleted (3):** `FileProcessingCompletedConsumer`, `FileThumbnailRequestedConsumer`,
`FileMetadataExtractionRequestedConsumer` — zero publishers found.

**Async duplicates deleted (3 consumers + 3 messages):** `LinkFileToOwnerConsumer`, `ValidateFileOwnershipConsumer`,
`DeleteFileConsumer` + their `*ProcessMessage` contracts — zero publishers found.

**Unreferenced abstractions deleted (8):** `IFileStorageClient`, `IFileVersionRepository` + implementation + entity,
`IFileAccessPolicyRepository` + implementation + entity, `GetFileAccessUrlQuery` + handler.

**Rewired (1):** `CreateFileReadUrlConsumer` — changed from dead `GetFileAccessUrlQuery` to live `CreateReadUrlCommand`.

### Kept (with reason)

| Type | Reason |
|------|--------|
| `FileUploadedConsumer` | Live — publishes scan request (A4) |
| `OrphanFileCleanupConsumer` | Live — A3 cleanup |
| `FileVirusScanRequestedConsumer` | Live — A4 scan pipeline |
| `FileDeletedConsumer` | Live publisher in `DeleteFileCommandHandler` |
| `FileLinkedToOwnerConsumer` | Live publisher in `LinkFileToOwnerCommandHandler` |
| `FileProcessingRequestedConsumer` | Live publisher in `StartFileProcessingCommandHandler` |
| `CreateUploadSessionConsumer` | Live publisher in Messaging module |
| `CompleteUploadSessionConsumer` | Live publisher in Messaging module |
| `CreateFileReadUrlConsumer` | Live publisher in Messaging module (rewired to `CreateReadUrlCommand`) |

### Consistency

- **One** read-url path: `CreateReadUrlCommand` (HTTP + message consumer)
- **One** delete path: `DeleteFileCommand` (HTTP)
- **One** complete path: `CompleteUploadSessionCommand` (HTTP + message consumer)
- `IFileStorageRemoteCall` (Vessel): intact and builds
- DB tables for removed entities left in place (no destructive migration)

## Build results

| Project | Result |
|---------|--------|
| FileStorage | **0 errors** ✓ |
| Identity | **0 errors** ✓ |
| MarineProvider BFF | **0 errors** ✓ |
| Admin BFF | **0 errors** ✓ |
| Vessel | **0 errors** ✓ |

## Deferred

- A7: Per-provider rate limit on upload sessions
- Browser smoke for magic-byte rejection (requires real browser + Docker)
- Hangfire scheduling for the cleanup consumer
