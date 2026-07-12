# Claude Code Prompt — Phase 2e: pay down the FileStorage debts + fix the controller layering

Phase 2d closed the security gap for real (verified in code): the attach path is fail-closed, ownership is checked
(`profile.UserId == request.UserId` and `file.UploadedByUserId == request.UserId`), no identifiers are fabricated, and
the BFF passes the caller's user id. Keep all of that.

This phase pays off what was deferred, fixes a layering violation introduced by the Guid migration, and finally runs
the smoke that has never executed (Phase 2d's run died on Docker OOM, so **none of the security fixes are verified at
runtime**).

Order matters: **Part 1 must land before Part 2**, or the cleanup job will start deleting files that live documents
reference.

---

## Part 1 (blocking) — a failed claim currently leaves a corrupt state

`AttachProviderDocumentCommandHandler` persists the document, then claims the file:

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to claim file {FileId}. Document persisted but file unclaimed.", request.FileId);
}
```

The document row now references a file that FileStorage still considers **unclaimed**. Harmless today — and a data
loss bug the moment Part 2 ships, because the cleanup job deletes unclaimed files and would delete a file an active
document points at, leaving a document whose file is gone.

Fix it properly. Pick one and say which you chose in the report:

- **(a) Transactional:** claim first (or claim inside the same unit of work) and **roll back the document** if the
  claim fails → the attach returns an error, nothing is persisted, the file stays unclaimed and is later cleaned up
  normally. Simplest and safest.
- **(b) Reconciliation:** persist the document, mark it `ClaimPending`, and have a background job retry the claim.
  Cleanup must then **skip files referenced by any `ClaimPending` document**.

Do **not** publish a cleanup/delete message on claim failure — the document exists, so deleting the file is the wrong
repair.

## Part 2 — cleanup consumer for unclaimed files

Upload-then-abandon is the normal case (the user closes the tab), so without this the bucket fills with unclaimed
objects.

- Message contract in `Abstraction/Message` (e.g. `FileCleanupRequestedMessage` / a scheduled sweep).
- Consumer under `Api/Consumers`: delete files that are **unclaimed** (no active `FileOwnerReference`) **and** older
  than N hours (configurable, default 24).
- **Never delete a file that has an active owner reference** — and, if you chose (b) above, never delete one
  referenced by a `ClaimPending` document.
- Soft-delete + audit first; physical deletion happens in the background, as the architecture requires.

## Part 3 — fix the controller → repository layering violation

The Guid migration (Phase 2c) took a shortcut: the **handlers were left keyed on the internal `long`**, and the
translation was pushed into the controllers. All three inject `IFileRepository` and do their own DB lookup:

`FileController`, `FileAccessController`, `FileProcessingController` each hold `private readonly IFileRepository
_fileRepo;` and a private `ResolveFileId(Guid publicId) → long` that calls `_fileRepo.GetByGuidAsync(...)` and
`throw new KeyNotFoundException(...)`.

Three problems:
1. **Layering:** the API layer talks to the repository directly, bypassing CQRS. The project rule is
   Command/Query → Handler → Repository. A controller must bind, dispatch, and map the response — nothing else.
2. **Double DB round-trip:** the controller loads the file to translate the id, then the handler loads the same file
   again.
3. **Broken error contract:** `KeyNotFoundException` escapes the CQRS/business-exception pipeline, so a missing file
   does not come back in the standard `AizenApiResponse` envelope the way `AizenBusinessException` does.

**Fix:** make the commands/queries take the **public `Guid`**, and let the handler resolve it once via the repository,
throwing `AizenBusinessException` ("File not found.") when it does not exist. Then:

- Delete `IFileRepository` from all three controllers and remove every `ResolveFileId` helper.
- Controllers become thin: `[FromRoute] Guid fileId` → `_cqrs.ProcessAsync(new XCommand { FileId = fileId, ... })` →
  `SetResponse(result)`.
- The internal `long` stays inside the Repository/Domain layer — it must not appear in a command, a query, a DTO, a
  route or a response.

## Part 4 — legacy handler must not stay unvalidated

`AddOrganizerVerificationDocumentCommandHandler` (admin/internal path) still attaches documents **without** the
FileStorage validation that `AttachProviderDocumentCommandHandler` now performs. One validated path and one
unvalidated path is the same hole with a longer walk to it.

Extract the validation + persist + claim into a shared domain service and have **both** handlers call it. Do not
duplicate the logic.

## Part 5 — frontend: document upload still does not work from the UI

Part F has been deferred three times. There is currently **no way for a provider to upload a document from the
browser**; the onboarding Compliance step is non-functional.

`onboardingApi.ts`:
1. `createUploadSession(file)` → `POST /api/v1/provider/files/upload-session`
2. `PUT uploadUrl` **directly to S3/MinIO** — plain `fetch`, `Content-Type` header, **no Authorization header**
3. `completeUpload(fileId, uploadSessionCode)` → `POST /api/v1/provider/files/{fileId}/complete`
4. `attachDocument(fileId, documentType, issuer?)` → `POST /api/v1/provider/onboarding/documents`

Any step failing → do not attach; surface the error. `ComplianceVerificationPage`: render the document list from the
server (**metadata only**); mint a signed read URL **only when the user clicks a document**
(`POST /onboarding/documents/{fileId}/access-url`); never pre-fetch URLs for the list and never store a signed URL.
`fileId` is a `string` (Guid), never a number. Delete the old multipart path. i18n in tr + en.

---

## Part 6 — run the smoke that has never run

Phase 2d's smoke died on Docker OOM, so **the ownership fix and the fail-closed behaviour are unverified at runtime**.
If Docker is memory-constrained, bring up only what is needed (postgres, redis, keycloak, identity-api,
file-storage-api, bff-marineprovider, minio).

Write `docs/provider-onboarding-phase2e-report.md` with real HTTP statuses and bodies:

1. **Ownership (the decisive test):** provider A uploads a file and does **not** attach it. Provider B calls attach
   with A's `fileId` → **must be rejected** ("You do not own this file."). *If it succeeds, the security fix does not
   work — say so plainly.*
2. **Fail-closed:** stop `file-storage-api`, then attach → **rejected** ("Unable to verify the file."). It must not
   fall through to client-supplied data.
3. Attach a `fileId` still in `UploadUrlGenerated` (session created, never uploaded) → rejected.
4. Oversize file, and a `.exe` renamed to `.pdf` → rejected.
5. Happy path: A attaches their own file → succeeds; the FileStorage owner reference is active and points at the
   document's real `PublicId`; the row carries `UploadedByUserId = A`; no bucket / object key / URL anywhere in the
   response or the row.
6. Two different providers each attach a different file → **both succeed** (regression guard for the `Guid.Empty` bug).
7. **Claim failure (Part 1):** make `LinkToOwner` fail (e.g. stop FileStorage between persist and claim, or point the
   claim at a bad URL) → verify the chosen strategy holds: (a) the document is **not** persisted, or (b) it is marked
   `ClaimPending` and the cleanup job leaves the file alone.
8. **Cleanup (Part 2):** upload a file and never attach it → it is deleted after the TTL. Upload and attach → it
   **survives** the sweep.
9. **Browser:** on the Compliance step pick a PDF → it uploads, appears in the list, and opens via a short-lived
   signed URL that expires.

## Constraints

- Fail closed. A validation that cannot be performed is a rejection, never a pass.
- Controllers never touch repositories. Command/Query → Handler → Repository.
- The internal `long` file id never appears in a command, query, DTO, route or response.
- Never fabricate an identifier (`?? Guid.NewGuid()`, `?? Guid.Empty`) — a missing id is a bug; throw.
- Never delete a file that an active document references.
- Snapshots come from FileStorage's `FileDto`, never from the client.
- If something cannot be finished, leave the TODO **and say so in the summary**. Do not describe an unfinished item
  as done — the last three summaries did, and each time the code said otherwise.
