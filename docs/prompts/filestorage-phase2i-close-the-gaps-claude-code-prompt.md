# Claude Code Prompt — Phase 2i: close the gaps Phase 2h left open

Phase 2h landed most of the hardening. This phase closes what it did **not**, and proves the parts that were
reported as done but could not actually run.

Read this whole file before writing code. Several items look like they are already implemented — they are not, or
they are implemented in a way that can never execute.

---

## Ground truth — what 2h actually delivered

Verified in the code, keep and do not regress:

- **A1 content verification** — `CompleteUploadAsync` reads real `GetObjectMetadata`, enforces the content-type
  allowlist and the category size cap against the **actual** object, sniffs magic bytes (`%PDF-`, JPEG, PNG),
  persists the **actual** size, and on failure marks the file `Rejected` and deletes the object.
- **A4 quarantine gate** — `IFileScanner` + `NoOpFileScanner`; `FileUploadedConsumer` → `FileVirusScanRequested`
  → `PromoteToReady()` / `MarkQuarantined()`. Both the attach path and the read-url path reject `Quarantined`.
  `Uploaded` stays readable on purpose (a provider must see the document they just uploaded, before the scan
  finishes). **Do not narrow the read gate back to `Ready`-only** — that regression made every read URL throw and
  is invisible outside a real browser.
- **A5** — `ReadUrlExpirationMinutes` default is now 5.
- **A6** — `skipOwnershipCheck` is gone. `actorIsAdmin` skips only the profile-ownership check; **every** path
  still enforces `file.UploadedByUserId == profile.UserId`, and a non-admin call with `callerUserId <= 0` is
  rejected outright.
- **Part B** — dead code removed, one implementation per operation.

Two things were reported as done but **could not run**, and have since been fixed by hand. Verify they still hold,
and cover them with the tests in §5:

- **`OrphanFileCleanupJob`** (`Modules/FileStorage/.../Jobs/OrphanFileCleanupJob.cs`) — an `AizenRecurringJob`
  that publishes `OrphanFileCleanupRequestedMessage` hourly. The 2h consumer was correct but **nothing published
  the message**, so nothing was ever reaped.
- **`FileStorageService.DeleteFileAsync`** — now releases active owner references, deletes the object, then
  soft-deletes the row (the row survives as the audit record). Before, it *only* soft-deleted: the owner reference
  stayed **active** and the cleanup sweep skips both claimed and soft-deleted files, so the object could never be
  purged. If the object delete fails, the row is deliberately **not** soft-deleted, so the sweep retries.

---

## 1. Phantom claims are still immortal

`FileAttachmentValidationService` **claims the file before it persists the document**. If `LinkToOwner` succeeds
and `SaveChangesAsync` then fails, FileStorage holds an **active owner reference pointing at a document that does
not exist**. The cleanup sweep explicitly skips files with an active owner reference, so that file is never reaped
and its object is never purged. 2h's own acceptance criterion for this (test 7) was not met.

Fix, in FileStorage:

- Add a detection pass to the orphan cleanup sweep (or a sibling sweep, your call — say which and why): for owner
  references whose `OwnerModule`/`OwnerEntityType` is a domain document, ask the owning module whether the
  `OwnerEntityId` still exists. If it does not, **release the reference**; the file then falls into the normal
  unclaimed path and is reaped on the next pass.
- The ownership check must go over `AizenRemoteCall` to the owning module (Identity for verification documents) —
  FileStorage must not read another module's tables.
- **Fail closed in the safe direction here:** if the owning module cannot be reached, do **not** release the
  reference. An unreachable module is not evidence that the document is gone. Retry next sweep.

Alternatively, close it at the source as well (do both — defence in depth): make the attach path **persist first,
claim second**, so a failed `SaveChanges` never leaves a claim behind. If you reorder it, a failed *claim* must
roll back the document row — state how you guarantee that.

## 2. The size lie is logged, not rejected

`CompleteUploadAsync` computes the deviation between the declared and the actual size and then only calls
`LogWarning`. The 2h prompt required a rejection, and acceptance test 2 ("declare `size: 1000`, `PUT` 50 MB →
`complete` rejects") passes today only by accident — when the actual size happens to exceed the category cap.

Fix: when the deviation exceeds the tolerance, **reject**: `RejectAndDeleteAsync` + a business error, exactly like
the content-type and magic-byte failures. Keep persisting the actual size for the cases that pass.

Do not widen the tolerance to make the test pass. If the current tolerance is too tight for legitimate uploads,
say so and justify the number you choose.

## 3. Upload-session rate limiting (2h deferred this)

2h reported this as deferred. Implement it now, per provider:

- a cap on **upload sessions created per hour**, and
- a cap on **total bytes held in `Pending`/unclaimed state**.

Reuse the existing rate-limiting convention — the `pwd-recovery-ip` fixed-window limiter is the in-repo
precedent. Redis is already wired (`IAizenDistributedCache`). Exceeding either cap is a business error, not a
silent drop.

## 4. `CreateFileReadUrlConsumer` is still dead

2h rewired it to `CreateReadUrlCommand` instead of deleting it, so at least there is no longer a second read-url
implementation. But **nothing publishes `CreateFileReadUrlProcessMessage`** — it is a consumer with no producer.
Minting a signed URL is a request/response operation and does not belong on a bus. Delete the consumer and its
message contract, unless you find a live publisher — in which case name it.

While you are there, apply the same test to the other survivors (`CreateUploadSessionConsumer`,
`CompleteUploadSessionConsumer`, `FileDeletedConsumer`, `FileLinkedToOwnerConsumer`,
`FileProcessingRequestedConsumer`): a consumer that no one publishes to, or that only logs and drops, is worse
than no consumer — it makes the message look handled. List what you keep and why.

## 5. Tests — there is no safety net at all

The repository has **no test project** for this area, which is why every one of these defects was found by hand in
a browser. Create one (`tests/Aizen.Modules.FileStorage.Tests` or wherever the solution's convention puts it — if
there is no convention, establish one and say so) and cover:

1. **Entity constructor guard.** A reflection test that scans every assembly for `AizenEntity` subclasses and
   **fails if any has a `private` parameterless constructor.** EF Core's lazy-loading proxies subclass the entity
   via Castle DynamicProxy, so a `private` ctor throws at materialisation — and only once the first row exists.
   This exact defect on `VerificationDocumentEntity` broke `organizers/profiles/by-subject/{sub}` with a 500 the
   moment a provider uploaded their first document, taking down **every** BFF profile resolution for that account.
   It was invisible until then. This test is the single highest-value thing in this phase.
2. **`CompleteUploadAsync`**: renamed executable → rejected + object deleted; size lie → rejected; truthful PDF →
   accepted with the **actual** size persisted.
3. **Read gate**: `Uploaded` → URL minted; `Quarantined` / `Rejected` / `Deleted` → refused.
4. **Attach**: admin attaching A's file to B's profile → rejected; `callerUserId <= 0` non-admin → rejected.
5. **Delete**: owner references released, object purged, row soft-deleted; object-delete failure → row **not**
   soft-deleted and the file is left reapable.
6. **Cleanup sweep**: unclaimed + old → reaped; claimed → untouched; phantom claim → released.

## 6. Prove it in a real browser

Code that compiles is not evidence. Every defect in this chain — SigV4, the duplicated `Content-Type`, the
unresolved identity, the `Ready`-only read gate — passed a build and passed curl, and failed in the browser.
`curl` sends no `Origin` header and no duplicate headers; it will not reproduce any of them.

Run the full provider flow in a real browser and paste the actual network results:

upload a PDF → `PUT` 200 → `complete` → attach → document listed → **survives a page reload** → opens via a signed
URL (`localhost:9000`, SigV4, 5 min TTL, `application/pdf`) → delete → **the object is gone from the bucket**
(check MinIO, not just the API response).

Then: upload and abandon a file, run the cleanup sweep, and show the object being reaped.

If you cannot drive a browser, **say so plainly in the report** instead of substituting curl and calling it
verified.

---

## Report

Write `docs/filestorage-phase2i-report.md`. Paste actual HTTP statuses, bodies, and bucket listings.

**Report failures plainly.** Three consecutive phase reports in this module described work as complete that the
code contradicted — a consumer with no publisher, a delete that could never purge, a size check that only logged.
An honest "not done" is worth more than a confident "done" that the next browser run disproves. If something
cannot be finished, leave the TODO and say so in the summary.

## Constraints

- Fail closed everywhere — except where §1 says otherwise: an unreachable module is not proof that a document was
  deleted.
- Persist only **verified** metadata — never the client's declaration.
- Never delete a file that an active document references.
- Never fabricate an identifier and never default a user id to `0` — a missing id is a bug; reject or throw.
- The domain keeps `FileId` + snapshots only: no bucket, object key, signed URL, or storage provider.
- Sync = `AizenRemoteCall`; background = RabbitMQ; scheduled = `AizenRecurringJob`. No raw `HttpClient`.
- Parameterless entity constructors are `protected`, never `private`.
