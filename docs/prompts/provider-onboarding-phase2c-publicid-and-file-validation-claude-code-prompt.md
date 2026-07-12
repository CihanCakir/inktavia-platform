# Claude Code Prompt — Phase 2c: fix `PublicId`, make FileStorage's public API Guid-keyed, then finish the validation

Phase 2b correctly identified a blocker (Guid vs long) and stopped. Good. But the root cause is worse than reported,
and it must be fixed **before** anything else, because the current state is not merely insecure — it is broken.

## The root cause: `PublicId` is never assigned

- `AizenEntity` declares `public Guid? PublicId { get; set; }`.
- **Nothing in the entire repository ever sets it** — not `FileEntity.Create`, not a SaveChanges interceptor, not a
  value generator, not any module. Verified by grep across `Core/` and `Modules/`.
- Every mapper therefore falls back: `FileId = entity.PublicId ?? Guid.Empty`
  (`FileMapper`, `FileAccessService`, `FileUploadSessionService`, `FileStorageService`, `FileOwnershipService`).

**Consequence: every `FileDto.FileId` returned today is `00000000-0000-0000-0000-000000000000`.**

That cascades:

1. The BFF's upload-session response hands the frontend `fileId = Guid.Empty` for every file.
2. `VerificationDocumentEntity.FilePublicId` is `Guid.Empty` for every attached document.
3. The "cross-profile `FilePublicId` uniqueness" guard added in Phase 2b then **locks everyone out**: once *any*
   profile attaches *any* document, every other profile's attach is rejected with "this file is already associated
   with another profile" — because all files share the same empty Guid. Document upload is broken after the first
   document in the system.
4. Adding a Guid route without fixing this would still 404: `PublicId == Guid.Empty` never matches a `NULL` row.

Fix this first. Everything else depends on it.

## Note: the Guid lookup already exists

`FileRepository.cs:22` already does `_db.Files.FirstOrDefaultAsync(x => x.PublicId == fileId && !x.IsDeleted, ct)`.
The repository is Guid-ready; only the **controller routes** are still keyed by the internal `long`. This is a small
gap, not a redesign.

---

## Step 1 — Assign and enforce `PublicId` (FileStorage only)

- Assign `PublicId = Guid.NewGuid()` in `FileEntity.Create` (and any other creation path, e.g. upload-session file
  creation). Do **not** touch `Aizen.Core.Domain` — other modules are out of scope.
- Migration `BackfillFilePublicId`: give every existing `Files` row a fresh Guid, then make the column
  **NOT NULL + UNIQUE** with an index.
- Replace every `PublicId ?? Guid.Empty` fallback with a hard failure. A file without a `PublicId` is a bug, not an
  empty Guid — surfacing it as `Guid.Empty` is what hid this for so long.
- **Data repair:** existing `VerificationDocuments` rows have `FilePublicId = Guid.Empty`. Resolve them via the
  document's internal `FileId (long)` → the file's new `PublicId`. Rows whose file no longer exists are marked
  orphaned and **reported in the migration output — do not silently drop them**.

## Step 2 — Key FileStorage's public API by `Guid`

Re-route the public endpoints to the **public Guid**, resolving the internal `long` inside FileStorage:

```
GET    /api/v1/files/{fileId:guid}
GET    /api/v1/files/{fileId:guid}/metadata
DELETE /api/v1/files/{fileId:guid}
PUT    /api/v1/files/{fileId:guid}/visibility
POST   /api/v1/files/{fileId:guid}/owners
POST   /api/v1/files/{fileId:guid}/access/read-url
POST   /api/v1/files/{fileId:guid}/access/validate-ownership
```

Use the existing `FileRepository` PublicId lookup. The internal `long Id` stays internal and **must never appear in a
response, a route, or a cross-module contract**.

**Do not adopt the alternative suggested in the Phase 2b summary — "have the BFF forward both the Guid and the
internal long ID".** That pushes a sequential, enumerable database key across a trust boundary, which is exactly what
the architecture forbids ("object key/ids are not guessable; FileId enumeration must not yield access"). Reject it.

Update the three existing consumers to the Guid routes:
- `Modules/Vessel/.../VesselFileStorageService.cs`
- `Bff/src/AdminPanel/.../IFileStorageAdminBffRemoteCall.cs`
- `Bff/src/MarineProvider/.../IProviderFileStorageRemoteCall.cs`

## Step 3 — Now finish the validation that Phase 2/2b was written for

In `AttachProviderDocumentCommandHandler` (and the legacy `AddOrganizerVerificationDocumentCommandHandler`, or make
both delegate to one domain service), replace the commented-out `throw`s with real checks, in order, failing closed:

1. **Profile belongs to the caller** — the command must carry the caller's `UserId`; reject if
   `profile.UserId != request.UserId`. Today the handler only matches on `ProfileId` + `Organizer`, so the Identity
   endpoint is undefended even when the BFF is correct.
2. **File exists** → `GetFile(publicId)`.
3. **Ownership** → `ValidateOwnership` must return `IsValid == true` for this caller. **This is the check that closes
   the vulnerability.** The Phase 2b "cross-profile uniqueness" guard is *not* an ownership check — it only blocks a
   file already attached elsewhere, so provider B can still steal an *unattached* file A uploaded by attaching it
   first. Keep that guard as a secondary integrity check, but it must not be the security boundary.
4. **Upload status** → `FileStatus ∈ { Uploaded, Ready }`. Reject `Created` / `UploadUrlGenerated` (session created,
   never uploaded), `Rejected`, `Deleted`, `Orphaned`.
5. **Type + size** → `ContentType ∈ { application/pdf, image/jpeg, image/png }`, `SizeInBytes <= 10 MB`.
6. **Persist snapshots taken from FileStorage's `FileDto`** — never from the client (the client's values are
   unverified): `FilePublicId`, `OriginalFileName`, `ContentType`, `SizeInBytes`, `DocumentType`, `Issuer`,
   `UploadedAt`, `UploadedByUserId`. Never `BucketName`, `ObjectKey`, `SignedUrl`, `StorageProvider`.
7. **Claim it** → `LinkToOwner(publicId, { OwnerModule = Identity,
   OwnerEntityType = "OrganizerVerificationDocument", OwnerEntityId = <document public id> })`. Nothing claims files
   today, so every upload is orphaned forever.
8. On persist/claim failure → publish the cleanup message.

## Step 4 — Cleanup consumer

Message contract in `Abstraction/Message` (`FileCleanupRequestedMessage`), consumer in `Api/Consumers` that asks
FileStorage to delete unclaimed files older than N hours. Upload-then-abandon is the normal case (the user closes the
tab); without this the bucket fills with unclaimed objects.

## Step 5 — Frontend (Part F, still not done: upload does not work end to end)

`onboardingApi.ts`: `createUploadSession(file)` → `PUT uploadUrl` **directly to S3/MinIO** (plain `fetch`,
`Content-Type` header, **no Authorization header**) → `completeUpload(fileId, uploadSessionCode)` →
`attachDocument(fileId, documentType, issuer?)`. Any step failing → do not attach.
`ComplianceVerificationPage`: documents from the server (metadata only); mint a signed read URL **only on click**
(`POST /onboarding/documents/{fileId}/access-url`); never pre-fetch URLs for the list; never store a signed URL.
`fileId` is a `string` (Guid), never a number. Delete the old multipart path. i18n tr + en.

---

## Smoke — this decides whether the phase is done

Write `docs/provider-onboarding-phase2c-report.md`. Report failures plainly; do not relabel an unfinished item.

1. **`PublicId` is real:** upload two files → they have **different, non-empty** `FileId`s. (Today both are
   `Guid.Empty`.)
2. **Attachment works for more than one profile:** provider A attaches a document, then provider B attaches a
   *different* document → **both succeed**. (Today B is rejected because every file is `Guid.Empty`.)
3. **Provider B attaches provider A's `fileId` → REJECTED** by the ownership check. Paste the HTTP status and error
   body. *If this succeeds, the task has failed — say so; do not report the phase complete.*
4. Attach a `fileId` whose status is `UploadUrlGenerated` (never uploaded) → rejected.
5. Oversize file, and a `.exe` renamed to `.pdf` (real content-type mismatch) → rejected.
6. Successful attach → the file is **claimed** in FileStorage (an active `FileOwnerReference` exists), and
   `UploadedByUserId` is the provider's user id, **not** the BFF service account.
7. The persisted document row and every BFF response contain **no** bucket, object key or URL.
8. Upload and never attach → the cleanup consumer removes it after the TTL.
9. Browser: pick a PDF on the Compliance step → uploads, appears in the list, opens via a short-lived signed URL that
   expires.

## Constraints

- Do not modify `Aizen.Core.Domain`.
- Never let the internal `long` file id cross a module or BFF boundary.
- No `X-Aizen-User-Token`; use the existing service-token + `X-Aizen-Bff-Assertion` model.
- `AizenRemoteCall` for sync, RabbitMQ for background. No Refit, no raw `HttpClient`.
- A `FileId` is never attached without file-exists + ownership + upload-status + type/size validation.
- If something cannot be finished, leave the TODO **and say so in the summary** — do not label it a "security fix".
