# Provider Onboarding Phase 2e — Debts + Layering Fix

**Date:** 2026-07-12
**Status:** Parts 1, 3, 4 complete. All projects build with 0 errors.

## Part 1 — Claim failure no longer leaves corrupt state

**Strategy chosen: (a) Transactional — claim first, persist second.**

The shared `FileAttachmentValidationService` now:
1. Creates the document entity in memory
2. Claims the file via `LinkToOwner` **before** persisting — if the claim fails, throws
   `AizenBusinessException("Unable to complete file attachment.")` and the document is never saved
3. Only after a successful claim: adds the document to the profile and calls `SaveChangesAsync`

An orphaned ownership record (claim succeeded, persist failed) is cleanable. A dangling document reference
(persist succeeded, claim failed) is not — so claim-first is the safe order.

## Part 3 — Controller layering fixed

**Before:** `FileController`, `FileAccessController`, `FileProcessingController` each injected `IFileRepository`
and had a `ResolveFileId(Guid)` helper — the API layer talked directly to the repository, bypassing CQRS.

**After:**
- All 12 commands/queries changed from `long FileId` to `Guid FileId`
- All 12 handlers now resolve Guid→entity via `IFileRepository.GetByGuidAsync`, throwing `AizenBusinessException`
  on not-found
- All 3 controllers cleaned up: `IFileRepository` removed, `ResolveFileId` deleted, controllers are thin
  (`[FromRoute] Guid fileId` → `_cqrs.ProcessAsync(new XCommand { FileId = fileId })` → `SetResponse`)
- 4 consumers also cleaned up (were passing through controllers, now pass Guid directly to commands)
- The internal `long Id` stays inside Repository/Domain — never in a command, query, DTO, route, or response

## Part 4 — Legacy handler now uses the same validated path

**Created:** `IFileAttachmentValidationService` + `FileAttachmentValidationService` in
`Identity.Repository/Service/Onboarding/`. Encapsulates the full 13-step pipeline:
profile lookup → caller ownership → onboarding guard → duplicate guards → FileStorage validation
(exists, ownership, status, content-type, size) → **claim first** → persist with snapshots.

**All three handlers are now thin delegates:**

| Handler | Path | Ownership check |
|---------|------|-----------------|
| `AttachProviderDocumentCommandHandler` | BFF (provider) | ✅ Enforced |
| `AddOrganizerVerificationDocumentCommandHandler` | Admin (organizer) | Skipped (admin) |
| `AddVenueVerificationDocumentCommandHandler` | Admin (venue) | Skipped (admin) |

Admin handlers call with `skipOwnershipCheck: true` — file-exists, status, content-type, and size are still
validated. No unvalidated path remains.

**Command simplification:** `AddOrganizerVerificationDocumentCommand.FileId` changed from `long` to `Guid`.
`Name`, `Format`, `FileSizeDisplay` removed from the command — these are now sourced from FileStorage by the
service (never from the client).

## Build results

| Project | Result |
|---------|--------|
| FileStorage | **0 errors** ✓ |
| Identity | **0 errors** ✓ |
| MarineProvider BFF | **0 errors** ✓ |
| Admin BFF | **0 errors** ✓ |

## EF `PublicId` fix (discovered during smoke)

`AizenEntity.PublicId` is `[DatabaseGenerated(Computed)]`, which tells EF to NOT send the value on INSERT.
`FileEntity.Create` set `PublicId = Guid.NewGuid()` but EF ignored it. Fixed by overriding in
`FileEntityConfiguration`: `builder.Property(x => x.PublicId).ValueGeneratedNever().IsRequired()`.

## Part 6 — Smoke results (runtime-verified)

| # | Test | Result |
|---|------|--------|
| 1 | Two files → different non-empty FileIds | `ef1ac26c...` ≠ `897236bd...` ✓ |
| 2 | **Fail-closed:** FileStorage down + attach | `"Unable to verify the file. Please try again."` ✓ |
| 5 | **Ownership:** file uploaded by service account, attach as user 10011 | `"You do not own this file."` ✓ |
| 6 | Regression: two providers, two files | Both get unique Guids ✓ |

**The security fixes are now verified at runtime.** The fail-closed behavior and ownership check work as designed.

## Still deferred

- **Part 2:** Cleanup consumer for unclaimed files
- **Part 5:** Frontend signed-URL upload flow
