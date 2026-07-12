# Claude Code Prompt — Phase 2b: close the file-validation gap (security)

Phase 2 shipped the plumbing (BFF ↔ FileStorage remote call, provider file endpoints, Keycloak audience mapper, BFF
assertion trust, bucket/objectKey stripped from responses). **It did not ship the security fix it was written for**,
even though the report calls it "Part B — Security fix". Both document handlers still carry the same TODO:

- `AddOrganizerVerificationDocumentCommandHandler` → *"TODO (Part C): Validate that the FileId actually exists in
  FileStorage and that the caller owns it."*
- `AttachProviderDocumentCommandHandler` → *"TODO: Add FileStorage validation (file exists, is owned by caller,
  status = Uploaded)…"*

**The vulnerability is open: provider B can attach provider A's `FileId` to their own profile.** The guards that were
added (profile ownership, duplicate FileId, onboarding-status) do not touch this — none of them ask FileStorage who
owns the file. `LinkToOwner` is never called either, so every uploaded file stays unclaimed forever and there is no
cleanup consumer.

This prompt does exactly one thing: close that gap end to end. Do not add scope. **Do not mark it done unless the
smoke test in the last section proves provider B is rejected.**

---

## 1. Identity → FileStorage remote call (does not exist yet)

`Modules/Identity/src/Aizen.Modules.Identity.Application/Common/RemoteClients/IFileStorageRemoteCall.cs`, using
`AizenRemoteCall` attributes (mirror `IProviderIdentityRemoteCall`; **no Refit, no raw HttpClient**):

```csharp
[AizenRemoteCallGet("/api/v1/files/{fileId}")]
Task<AizenApiResponse<FileDto>> GetFile(Guid fileId);

[AizenRemoteCallPost("/api/v1/files/{fileId}/access/validate-ownership")]
Task<AizenApiResponse<FileValidationResultDto>> ValidateOwnership(Guid fileId, [AizenRemoteCallBody] ValidateOwnershipRequest request);

[AizenRemoteCallPost("/api/v1/files/{fileId}/owners")]
Task<AizenApiResponse<FileOwnerReferenceDto>> LinkToOwner(Guid fileId, [AizenRemoteCallBody] LinkFileToOwnerRequest request);
```

Reference `Aizen.Modules.FileStorage.Abstraction` from the Identity Application project. Register in
`Aizen.Modules.Identity.Repository/DependencyInjection.cs` with the service-token delegating handler Identity already
uses for outgoing module calls. Config: `RemoteCalls__IFileStorageRemoteCall__BaseUrl` → `http://file-storage-api:8080`
(docker-compose + appsettings). The Keycloak service token must carry audience `file-storage-api`.

## 2. Validate before persisting — `AttachProviderDocumentCommandHandler`

This is the provider-facing path. Insert **before** creating the entity, in this order (fail closed at every step):

1. **Profile belongs to the caller.** The command must carry the caller's `UserId` (the BFF already resolves it from
   `IProviderContext`; pass it through). Reject if `profile.UserId != request.UserId`. Right now the handler only
   matches on `ProfileId` + `Organizer` — it never checks the caller, so the Identity endpoint is undefended even if
   the BFF is correct.
2. **File exists** → `GetFile(fileId)`; reject if not found.
3. **Ownership** → `ValidateOwnership(fileId, …)` must return `IsValid == true` for this caller. **This is the check
   that closes the vulnerability.** A file uploaded by another user must be rejected here.
4. **Upload status** → `FileStatus ∈ { Uploaded, Ready }`. Reject `Created` / `UploadUrlGenerated` (never actually
   uploaded), `Rejected`, `Deleted`, `Orphaned`.
5. **Type + size** → `ContentType ∈ { application/pdf, image/jpeg, image/png }`, `SizeInBytes <= 10 MB`.
6. **Not already claimed by a different owner.**
7. Persist the row with **snapshots only** — `FilePublicId (Guid)`, `OriginalFileName`, `ContentType`, `SizeInBytes`,
   `DocumentType`, `Issuer`, `UploadedAt`, `UploadedByUserId`. Never `BucketName`, `ObjectKey`, `SignedUrl`,
   `StorageProvider`.
8. **Claim it** → `LinkToOwner(fileId, { OwnerModule = Identity, OwnerEntityType = "OrganizerVerificationDocument",
   OwnerEntityId = <document public id> })`. Without this the file stays unclaimed and the cleanup job will delete a
   file that a profile is actively referencing.
9. If persist or claim fails, publish the cleanup message so the file does not linger as `Orphaned`.

Take the snapshots (name/contentType/size) **from the `FileDto` returned by FileStorage**, not from the client — the
client's values are unverified.

Apply the same validation to `AddOrganizerVerificationDocumentCommandHandler` (the admin/legacy path), or make it
delegate to the same domain service. Do not leave one validated path and one unvalidated path.

## 3. Cleanup consumer (files are currently leaked forever)

Upload-then-abandon is the normal case (the user closes the tab). Add:
- message contract in `Abstraction/Message` (e.g. `FileCleanupRequestedMessage`),
- consumer under `Api/Consumers` that asks FileStorage to delete unclaimed files older than N hours,
- publish it from the failure paths above.

## 4. Frontend — wire the signed-URL flow (Part F was deferred; upload does not work end to end today)

`onboardingApi.ts`: three steps, then attach.
1. `createUploadSession(file)` → `POST /api/v1/provider/files/upload-session`
2. `PUT uploadUrl` **directly to S3/MinIO** — plain `fetch`, `Content-Type` header, **no Authorization header**
3. `completeUpload(fileId, uploadSessionCode)` → `POST /api/v1/provider/files/{fileId}/complete`
4. `attachDocument(fileId, documentType, issuer?)` → `POST /api/v1/provider/onboarding/documents`

Any step failing → do not attach. `ComplianceVerificationPage`: list documents from the server (metadata only); mint a
signed read URL **only when the user clicks a document**
(`POST /onboarding/documents/{fileId}/access-url`). Never pre-fetch URLs for the list, never store a signed URL.
`fileId` is a `string` (Guid), never a number. Delete the old multipart path. i18n in tr + en.

---

## Smoke — the only test that decides whether this is done

Write results to `docs/provider-onboarding-phase2b-report.md`. **Report failure honestly; do not soften it.**

1. Provider A uploads a file (session → PUT → complete) and attaches it → succeeds.
   - The persisted row contains **no** bucket, object key or URL.
   - The file is **claimed** in FileStorage (`FileOwnerReference` exists and is active).
   - `UploadedByUserId` is provider A's user id, **not** the BFF service account.
2. **Provider B takes provider A's `fileId` and calls `POST /onboarding/documents` → MUST be rejected** (ownership
   check). Paste the actual HTTP status and error body into the report. *If this attach succeeds, the task has
   failed — say so plainly rather than reporting the phase as complete.*
3. Attaching a `fileId` whose status is still `UploadUrlGenerated` (session created, never uploaded) → rejected.
4. Attaching an oversize file / a `.exe` renamed to `.pdf` (real content-type mismatch) → rejected.
5. Delete a document → the owner reference is unlinked → cleanup message published.
6. Upload a file and never attach it → the cleanup consumer removes it after the TTL.
7. From the browser: pick a PDF on the Compliance step → it uploads, appears in the list, and opens via a
   short-lived signed URL that expires.

## Constraints

- No `X-Aizen-User-Token`. Use the existing service-token + `X-Aizen-Bff-Assertion` model.
- `AizenRemoteCall` for sync; RabbitMQ for cleanup. No Refit, no raw `HttpClient`.
- Never attach a `FileId` without file-exists + ownership + upload-status + type/size validation.
- Snapshots come from FileStorage's `FileDto`, never from the client.
- Signed URLs: short-lived, minted on demand, never persisted.
- If any part cannot be completed, leave the TODO **and say so in the summary** — do not label an unfinished item as
  a "security fix".
