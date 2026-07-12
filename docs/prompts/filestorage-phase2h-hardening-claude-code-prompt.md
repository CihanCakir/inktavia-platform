# Claude Code Prompt — Phase 2h: FileStorage hardening (post-upload verification, lifecycle, quarantine)

The identity/authorization half of the file pipeline is now solid: ownership is enforced, `PublicId` is real, the
public API is Guid-keyed, the internal `long` no longer crosses a boundary, bucket/objectKey never reach the client,
validation is fail-closed, and files are claimed via `LinkToOwner`. The bucket is private
(`mc anonymous set none`) and CORS is configured.

**What is not enforced is what was actually uploaded, and what happens to it afterwards.** This prompt closes that.

Run after Phase 2g (presign public endpoint), which unblocks the browser upload path.

---

## 1. CRITICAL — the uploaded object is never verified (violates the project's own rules)

`FileStorageService.CompleteUploadAsync` only does:

```csharp
var exists = await _storageProvider.ObjectExistsAsync(session.BucketName, session.ObjectKey, ct);
if (!exists) throw new InvalidOperationException("File object not found in storage.");
```

It checks that *an object exists* — **not what it is.** `ValidateContentType` / `ValidateFileSize` run at
*session-creation* time against values the **client declared**. Nothing ever reconciles them with the real object.

So today a provider can:
- declare `contentType: application/pdf`, `size: 1000` → passes the allowlist and the size cap;
- `PUT` **arbitrary bytes** — an executable, or a 500 MB file — to the presigned URL;
- call `complete` → the object exists → the file is marked `Uploaded`;
- Identity then validates against `file.ContentType` / `file.SizeInBytes`, which are the **declared** values.

The result is a record that says "1 KB PDF" pointing at arbitrary content, with no effective size ceiling. This
directly contradicts the architecture:
> *"MIME type yalnızca request header üzerinden güvenilerek kabul edilmez."*
> *"Dosya boyutu upload öncesi **ve sonrası** kontrol edilir."*

**Fix — verify the real object on complete, fail closed:**

1. `GetObjectMetadataAsync` (not just `ObjectExists`) → read the **actual** `ContentLength` and stored `ContentType`.
2. Reject when the actual size exceeds the category cap, **and** when it deviates from the declared size beyond a
   small tolerance. Persist the **actual** size — never the declared one.
3. Reject when the stored `ContentType` is not in the allowlist.
4. **Sniff the magic bytes** (read the first ~512 bytes via a ranged GET): `%PDF-` for PDF, `\xFF\xD8\xFF` for JPEG,
   `\x89PNG\r\n\x1a\n` for PNG. A `.exe` renamed to `.pdf` and declared as `application/pdf` must be rejected here —
   this is the only check that looks at the content itself.
5. On any failure: mark the file `Rejected`, **delete the object**, and return a business error. Do not leave a
   rejected object sitting in the bucket.
6. If a `Checksum` was supplied, verify it (today it is accepted and ignored).

The snapshots Identity persists must come from this verified metadata, not from the session's declared values.

## 2. Deleting a document leaves the file behind forever

`DeleteOnboardingDocumentCommandHandler` removes the row from the profile and stops. Its own comment admits it:

```csharp
// NOTE: FileStorage file deletion (soft-delete) can be triggered asynchronously by Identity
```

That was never implemented. The file stays **claimed** in FileStorage, so the cleanup job (Part 3 below) will never
reap it either — a permanent leak.

**Fix:** on document delete → unlink the owner reference in FileStorage
(`DELETE /files/{fileId}/owners/...` or equivalent). If no other owner holds the file, FileStorage raises a delete
request and the **background consumer** performs the physical deletion. The BFF/Identity never call the storage SDK,
and the audit record survives the file.

## 3. Cleanup consumer (nothing reaps anything today)

Upload-then-abandon is the normal case — the user closes the tab. Without this the bucket fills with unclaimed
objects.

- Message contract in `Abstraction/Message`, consumer under `Api/Consumers`.
- Reap files that are **unclaimed** (no active `FileOwnerReference`) and older than N hours (configurable, default 24).
- **Never delete a file that has an active owner reference.**
- **Also reap the mirror case:** the attach path claims *before* it persists, so a claim that succeeds followed by a
  failed `SaveChanges` leaves a file claimed to a **document that does not exist**. Those references must be detected
  (owner entity id with no matching document) and released, otherwise they are immortal.

## 4. Quarantine / AV scan

`FileStatus.Quarantined` exists in the design and is never used. A provider uploads a document; an admin later opens
it through a signed URL.

- On `FileUploadedMessage`, publish a scan request; the consumer sets `Quarantined` or `Ready`.
- **While a file is `Quarantined` or still `Processing`: refuse to mint a read URL and refuse to attach it to a
  domain entity.** Wire the actual scanner behind an interface (`IFileScanner`) with a no-op/stub implementation for
  local — the point is that the *gate* exists, so plugging in ClamAV later is configuration, not surgery.

## 5. Read URLs are not short-lived

`S3ObjectStorageOptions.ReadUrlExpirationMinutes` defaults to **60**. A one-hour capability handed to a browser is
not short-lived; the whole point of minting per click is that the URL dies quickly.

Set the read default to **5 minutes** (upload can stay at 15 — a large file on a slow link needs the headroom).

## 6. `skipOwnershipCheck` is a blanket bypass on the admin paths

`FileAttachmentValidationService` takes `skipOwnershipCheck`, and the admin handlers
(`AddOrganizerVerificationDocument`, `AddVenueVerificationDocument`) pass `true` — which disables **both** the
profile-ownership check *and* the file-ownership check. Those endpoints can therefore attach **any file to any
profile** with no ownership validation at all. A provider cannot reach them today (Identity is internal), but this is
a bypass by construction, and one boolean conflating two different concerns.

**Fix:** an admin acting on behalf of a provider should not skip the check — it should be evaluated against the
**target profile's owner**, not the caller:

- caller-is-admin → skip `profile.UserId == callerUserId` (the admin legitimately does not own the profile),
- but still require **`file.UploadedByUserId == profile.UserId`** — a file may only be attached to the profile of the
  user who uploaded it.

Replace the single `skipOwnershipCheck` flag with an explicit `actorIsAdmin` and this rule. The invariant survives;
admins can still act.

## 7. No rate limit on upload sessions

A provider can request unbounded upload sessions and `PUT` unbounded bytes. Add, per provider:
- a cap on upload sessions per hour, and
- a cap on total bytes in `Pending`/unclaimed state.

Reuse the existing rate-limiting convention (the `pwd-recovery-ip` fixed-window limiter is the in-repo precedent).

---

## Acceptance — real tests, real bytes

Write `docs/filestorage-phase2h-report.md`. Paste actual HTTP statuses and bodies. **Report failures plainly.**

1. **Renamed executable:** create a session declaring `application/pdf`, then `PUT` a real `.exe` payload → `complete`
   **must reject** (magic-byte check) and the object must be **deleted** from the bucket. *This is the headline test —
   today it succeeds.*
2. **Size lie:** declare `size: 1000`, `PUT` 50 MB → `complete` rejects; the bucket does not retain the object.
3. **Truthful upload:** a real PDF → `complete` succeeds, and the persisted `SizeInBytes` equals the **actual** object
   size (not the declared one).
4. **Quarantine gate:** force a file to `Quarantined` → minting a read URL and attaching it both fail.
5. **Delete:** attach a document, delete it → the owner reference is released in FileStorage and the object is
   physically gone after the cleanup consumer runs.
6. **Abandoned upload:** create a session, `PUT`, never attach → the cleanup consumer removes it after the TTL.
7. **Phantom claim:** simulate claim-succeeds-then-persist-fails → the dangling owner reference is detected and
   released (the file does not become immortal).
8. **Read TTL:** a minted read URL is dead after 5 minutes.
9. **Admin path:** an admin attaches a file uploaded by provider A to **provider B's** profile → **rejected**
   (`file.UploadedByUserId != profile.UserId`). Attaching A's file to **A's** profile still succeeds.
10. **Rate limit:** exceeding the hourly session cap is rejected.

## Constraints

- Fail closed everywhere. A check that cannot be performed is a rejection, not a pass.
- Persist only **verified** metadata (from `GetObjectMetadata` / the sniffed content) — never the client's declaration.
- Never delete a file that an active document references.
- Never fabricate an identifier (`?? Guid.NewGuid()`, `?? Guid.Empty`) — a missing id is a bug; throw.
- The domain keeps `FileId` + snapshots only: no bucket, object key, signed URL or storage provider.
- Sync = `AizenRemoteCall`; background = RabbitMQ. No Refit, no raw `HttpClient`.
- If something cannot be finished, leave the TODO **and say so in the summary** — do not describe an unfinished item
  as done.
