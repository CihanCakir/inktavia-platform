# Claude Code Prompt — Phase 2h: FileStorage hardening + dead-code cleanup

This supersedes `filestorage-phase2h-hardening-claude-code-prompt.md`. It is rewritten against findings from a
**real browser end-to-end run** of the provider document-upload flow, not from reading the code.

## Where we actually are

The upload chain is now **proven green in a real browser** (not curl):

| Step | Verified |
|---|---|
| `POST /provider/files/upload-session` | 200, presigned URL is `AWS4-HMAC-SHA256`, host `localhost:9000` |
| `PUT` → MinIO (real cross-origin, real CORS) | **200** |
| `POST /files/{fileId}/complete` | 200, `status: Uploaded` |
| `POST /provider/onboarding/documents` (attach) | 200, `success: true` |
| Document in `GET /onboarding`, survives reload | ✅ served from the server |
| Read URL | ✅ `localhost:9000`, SigV4, **TTL 300 s**, `GET` 200, `application/pdf`, `%PDF-` magic bytes |
| Read URL for a file **not** owned by the caller | ✅ refused |
| Delete | ✅ document disappears from the list |

Getting there required four fixes that the previous phase reports had claimed were already done or never noticed.
**These are now in the codebase. Do not regress them, and do not "fix" them again:**

1. **`AWSConfigsS3.UseSignatureVersion4 = true`** (static ctor) + `GetPreSignedUrlRequest.Protocol = _presignProtocol`
   in `S3ObjectStorageProvider`. Without the flag the SDK silently emits SigV2 and MinIO answers 403; `Protocol`
   defaults to HTTPS and overrides the ServiceURL scheme.
2. **The BFF returns `requiredHeaders` with a camelCased key (`content-Type`).** The frontend now dedupes header
   names case-insensitively — `setRequestHeader` *appends* rather than overwrites, so a second `Content-Type`
   produced `application/pdf, application/pdf` and broke the signature (`SignatureDoesNotMatch`).
3. **The BFF provider handlers must call `IProviderProfileResolver.ResolveAsync` before any module call.**
   `IProviderIdentityHolder` is scoped and *only* the resolver populates it. `AttachOnboardingDocument`,
   `CreateUploadSession`, `CompleteUpload`, `DeleteOnboardingDocument` and `GetDocumentAccessUrl` did not, so the
   asserted user was **0**: files were stored with `UploadedByUserId = 0` and Identity rejected every attach with
   *"You do not have permission to modify this profile."* All five now resolve first and **fail closed** when the
   user id is missing.
4. **`FileAccessService.CreateReadUrlAsync` required `FileStatus.Ready`** — a state nothing ever assigns (that is
   the processing/scan pipeline, i.e. this phase). It now allow-lists `Uploaded | Ready`. **Part 4 below changes
   this again; read it before touching that method.**

Two more defects were fixed along the way and are also load-bearing: the read-url endpoint had an **IDOR** (it
minted a signed URL for *any* `fileId` the caller named — it now verifies the file is attached to the caller's
profile), and a **`private` parameterless constructor on `VerificationDocumentEntity`** made EF Core's
lazy-loading proxy (Castle DynamicProxy subclasses the entity) throw on materialisation — so the very first
document row broke `organizers/profiles/by-subject/{sub}` with a 500, which is what every BFF profile resolution
depends on. Five sibling entities had the same latent bug. **All parameterless entity constructors in Identity are
now `protected`. Keep them that way, and never write a `private` one.**

---

# PART A — Hardening

## A1. CRITICAL — the uploaded object is never verified

`FileStorageService.CompleteUploadAsync` only calls `ObjectExistsAsync`. It checks that *an object exists* — not
**what** it is. `ValidateContentType` / `ValidateFileSize` run at *session-creation* time against values the
**client declared**, and nothing ever reconciles them with the real object.

So today a provider can declare `contentType: application/pdf, size: 1000`, `PUT` **arbitrary bytes** (an
executable, or 500 MB) to the presigned URL, call `complete`, and get a record that says "1 KB PDF" pointing at
arbitrary content, with no effective size ceiling. This contradicts the architecture directly:

> *"MIME type yalnızca request header üzerinden güvenilerek kabul edilmez."*
> *"Dosya boyutu upload öncesi **ve sonrası** kontrol edilir."*

**Fix — verify the real object on complete, fail closed:**

1. `GetObjectMetadataAsync` (not `ObjectExists`) → read the **actual** `ContentLength` and stored `ContentType`.
2. Reject when the actual size exceeds the category cap **and** when it deviates from the declared size beyond a
   small tolerance. Persist the **actual** size — never the declared one.
3. Reject when the stored `ContentType` is not in the allowlist.
4. **Sniff the magic bytes** (ranged GET of the first ~512 bytes): `%PDF-` for PDF, `\xFF\xD8\xFF` for JPEG,
   `\x89PNG\r\n\x1a\n` for PNG. A `.exe` renamed to `.pdf` and declared as `application/pdf` must be rejected
   here — this is the only check that looks at the content itself.
5. On any failure: mark the file `Rejected`, **delete the object**, return a business error.
6. If a `Checksum` was supplied, verify it (today it is accepted and ignored).

The snapshots Identity persists must come from this verified metadata, not from the session's declared values.

## A2. Deleting a document leaks the file forever

`DeleteOnboardingDocumentCommandHandler` removes the row from the profile and stops. Its own comment admits it:

```csharp
// NOTE: FileStorage file deletion (soft-delete) can be triggered asynchronously by Identity
```

That was never implemented. The file stays **claimed** in FileStorage, so the cleanup consumer (A3) will never
reap it either — a permanent leak. **We confirmed this in the browser: the document vanished from the list, the
object is still in the bucket, still claimed.**

**Fix:** on document delete → unlink the owner reference in FileStorage. If no other owner holds the file,
FileStorage raises a delete request and the **background consumer** performs the physical deletion. The
BFF/Identity never call the storage SDK, and the audit record survives the file.

## A3. Cleanup consumer (nothing reaps anything today)

Upload-then-abandon is the normal case — the user closes the tab. Without this the bucket fills with unclaimed
objects.

- Message contract in `Abstraction/Message`, consumer under `Api/Consumers`.
- Reap files that are **unclaimed** (no active `FileOwnerReference`) and older than N hours (configurable,
  default 24).
- **Never delete a file that has an active owner reference.**
- **Reap the mirror case:** the attach path claims *before* it persists, so a claim that succeeds followed by a
  failed `SaveChanges` leaves a file claimed to a **document that does not exist**. Detect owner references whose
  owner entity id has no matching document and release them, otherwise they are immortal.
- **One-off backfill:** the files uploaded during this debugging session were stored with
  `UploadedByUserId = 0` before fix (3) landed. They can never be attached and never be claimed. The cleanup pass
  must reap them like any other unclaimed file — verify it does, do not special-case them.

## A4. Quarantine / AV gate — and the `Ready` state

`FileStatus.Quarantined` exists in the design and is never used. `Ready` is never assigned by anything.

- On `FileUploadedMessage`, publish a scan request; the consumer sets `Quarantined` or **promotes the file to
  `Ready`**. Wire the scanner behind an `IFileScanner` interface with a no-op/stub implementation for local — the
  point is that the *gate* exists, so plugging in ClamAV later is configuration, not surgery.
- **While a file is `Quarantined` or still `Processing`: refuse to mint a read URL and refuse to attach it.**
- Once `Ready` is genuinely reachable, revisit `FileAccessService.CreateReadUrlAsync`, which currently allows
  `Uploaded | Ready` (see fix 4 above). Decide deliberately: either keep `Uploaded` readable (the provider must be
  able to see the document they just uploaded, before the scan finishes) or make the whole flow wait for `Ready`
  and give the UI a "processing" state. **State which you chose and why.** Do not silently narrow it back to
  `Ready`-only — that is the exact regression that broke every read URL, and the browser is the only place it
  shows up.

## A5. Read URLs are not short-lived at the source

The BFF asks for 5 minutes explicitly, so the flow is fine today — but
`S3ObjectStorageOptions.ReadUrlExpirationMinutes` still defaults to **60**. Any caller that omits `ExpiresIn`
gets an hour-long capability. Set the read default to **5 minutes** (upload stays 15 — a large file on a slow
link needs the headroom).

## A6. `skipOwnershipCheck` is a blanket bypass on the admin paths

`FileAttachmentValidationService` takes `skipOwnershipCheck`, and the admin handlers
(`AddOrganizerVerificationDocument`, `AddVenueVerificationDocument`) pass `true` — which disables **both** the
profile-ownership check *and* the file-ownership check. Those endpoints can attach **any file to any profile**
with no ownership validation. One boolean conflating two different concerns.

**Fix:** replace it with an explicit `actorIsAdmin`:

- caller-is-admin → skip `profile.UserId == callerUserId` (the admin legitimately does not own the profile),
- but still require **`file.UploadedByUserId == profile.UserId`** — a file may only be attached to the profile of
  the user who uploaded it.

Note while you are in here: `callerUserId` arrives from the BFF's identity assertion. If the resolver was not
called it is `0` and every check silently compares against zero. Make `ValidateAndAttachAsync` **reject a
non-admin call with `callerUserId <= 0`** outright rather than letting it fall through to a comparison.

## A7. No rate limit on upload sessions

A provider can request unbounded upload sessions and `PUT` unbounded bytes. Add, per provider: a cap on upload
sessions per hour, and a cap on total bytes in `Pending`/unclaimed state. Reuse the existing rate-limiting
convention (the `pwd-recovery-ip` fixed-window limiter is the in-repo precedent).

---

# PART B — Dead-code cleanup in FileStorage

The module carries a large speculative surface that nothing calls. It is not harmless: `CreateFileReadUrlConsumer`
still routes to `GetFileAccessUrlQuery` while the live HTTP path uses `CreateReadUrlCommand`, so there are **two
divergent read-url implementations** — exactly the kind of split that let the `Ready`-only bug hide.

**Method — verify before deleting. Do not trust this inventory; it is a starting point, not a work order.**
For each candidate: grep the whole solution (all modules, all BFFs, `Program.cs`, DI registrations, MassTransit
endpoint configuration) for the type name **and** for the message name on the wire. Delete only what has **zero**
callers/publishers. If something has exactly one caller and that caller is itself dead, say so and delete both.
List anything you keep, with the reason.

### B1. No-op event consumers (log-and-drop)

These consume a message and do nothing but `LogInformation` + `Task.CompletedTask`:

`FileUploadedConsumer`, `FileDeletedConsumer`, `FileLinkedToOwnerConsumer`, `FileProcessingRequestedConsumer`,
`FileProcessingCompletedConsumer`, `FileVirusScanRequestedConsumer`, `FileThumbnailRequestedConsumer`,
`FileMetadataExtractionRequestedConsumer`, `OrphanFileCleanupConsumer`.

Of these, **`FileUploadedConsumer` and `OrphanFileCleanupConsumer` become real** in A3/A4 — implement them, do not
delete them. `FileVirusScanRequestedConsumer` becomes real in A4. For the rest: no publisher exists anywhere in
the solution. Either delete the consumer **and its message contract**, or — if a message is genuinely part of the
intended contract for another module — leave it and say which module is expected to consume it. **A consumer that
logs and drops is worse than no consumer: it makes the message look handled.**

### B2. Async duplicates of the synchronous HTTP path

`CreateUploadSessionConsumer`, `CompleteUploadSessionConsumer`, `CreateFileReadUrlConsumer`,
`LinkFileToOwnerConsumer`, `ValidateFileOwnershipConsumer`, `DeleteFileConsumer` re-implement request/response
operations that already exist as controller endpoints and are what the BFF actually calls. `LinkFileToOwner`,
`ValidateFileOwnership` and `DeleteFile` process-messages have **no publisher at all**.

These are request/response operations — they do not belong on a bus. Delete the consumers and their
`*ProcessMessage` contracts unless you find a live publisher. **`CreateFileReadUrlConsumer` in particular must
go**: it is the second read-url implementation described above.

### B3. Unreferenced abstractions and repositories

Verify each, then remove code + DI registration:

- **`IFileStorageClient`** (`Abstraction/Client`) — appears to have zero references in the entire solution.
- **`IFileVersionRepository` / `FileVersionRepository` / `FileVersionEntity`** — referenced only by themselves,
  their EF configuration, and DI. No feature reads or writes versions.
- **`IFileAccessPolicyRepository` / `FileAccessPolicyRepository` / `FileAccessPolicyEntity`** — same shape.
- **`GetFileAccessUrlQuery` / `GetFileAccessUrlQueryHandler`** — only reachable through the dead
  `CreateFileReadUrlConsumer`.

**Schema:** do **not** write a destructive migration in this phase. Removing the C# types while the tables remain
is fine and reversible; dropping tables is not. If you remove an entity from `FileStorageDbContext`, generate the
migration, **inspect it**, and if it drops a table, leave that table in place (empty tables cost nothing) and note
it for a later, deliberate schema cleanup.

### B4. Consistency sweep

- One read-url path, one delete path, one complete path. Two implementations of the same operation is the bug
  class that produced the `Ready`-only failure — after this phase there must be exactly one of each.
- `IFileStorageRemoteCall` is consumed by Vessel (`VesselFileStorageService`). Do not break it. If its shape
  changes, update Vessel in the same commit and build both.

---

## Acceptance — real tests, real bytes

Write `docs/filestorage-phase2h-report.md`. Paste **actual** HTTP statuses and bodies. **Report failures plainly**
— a previous phase report claimed "PUT returns HTTP 200" when the browser was failing, because it tested with
curl. `curl` sends no `Origin` header and passes where a browser fails.

1. **Renamed executable:** session declaring `application/pdf`, `PUT` a real `.exe` payload → `complete` **must
   reject** (magic bytes) and the object must be **deleted** from the bucket. *Headline test — today it succeeds.*
2. **Size lie:** declare `size: 1000`, `PUT` 50 MB → `complete` rejects; the bucket does not retain the object.
3. **Truthful upload:** a real PDF → `complete` succeeds and the persisted `SizeInBytes` equals the **actual**
   object size, not the declared one.
4. **Quarantine gate:** force a file to `Quarantined` → minting a read URL and attaching both fail.
5. **Delete:** attach a document, delete it → the owner reference is released and the object is physically gone
   after the cleanup consumer runs. **Verify the object is gone from the bucket, not just the row from the DB.**
6. **Abandoned upload:** create a session, `PUT`, never attach → the cleanup consumer removes it after the TTL.
7. **Phantom claim:** claim-succeeds-then-persist-fails → the dangling owner reference is detected and released.
8. **Legacy orphans:** the `UploadedByUserId = 0` files already in the bucket are reaped by the cleanup pass.
9. **Read TTL:** a minted read URL is dead after 5 minutes.
10. **Admin path:** an admin attaching provider A's file to **provider B's** profile is **rejected**
    (`file.UploadedByUserId != profile.UserId`); attaching A's file to **A's** profile still succeeds.
11. **Non-admin with `callerUserId = 0`** is rejected outright.
12. **Rate limit:** exceeding the hourly session cap is rejected.
13. **Regression — the browser path still works.** Re-run the full provider flow in a **real browser**: upload a
    PDF → PUT 200 → complete → attach → document listed → survives reload → opens via a signed URL → deletes.
    This is the only test that catches the SigV4/`Content-Type`/resolver/`Ready` class of bugs. If you cannot run
    a browser, say so explicitly in the report instead of substituting curl and calling it verified.
14. **Regression — EF proxies.** A query that materialises `VerificationDocumentEntity` (e.g.
    `organizers/profiles/by-subject/{sub}` for a provider **that has at least one document**) returns 200. Add a
    test that fails if any entity in the solution has a `private` parameterless constructor — that defect is
    invisible until the first row exists.
15. **Nothing was silently removed.** After the cleanup, the solution builds, Vessel builds, and every deleted
    type is listed in the report with the evidence that it had no callers.

## Constraints

- Fail closed everywhere. A check that cannot be performed is a rejection, not a pass.
- Persist only **verified** metadata (from `GetObjectMetadata` / sniffed content) — never the client's declaration.
- Never delete a file that an active document references.
- Never fabricate an identifier (`?? Guid.NewGuid()`, `?? Guid.Empty`) and never default a user id to `0` — a
  missing id is a bug; reject or throw.
- The domain keeps `FileId` + snapshots only: no bucket, object key, signed URL or storage provider.
- Sync = `AizenRemoteCall`; background = RabbitMQ. No Refit-by-hand, no raw `HttpClient`.
- Parameterless entity constructors are `protected`, never `private`.
- If something cannot be finished, leave the TODO **and say so in the summary** — do not describe an unfinished
  item as done.
