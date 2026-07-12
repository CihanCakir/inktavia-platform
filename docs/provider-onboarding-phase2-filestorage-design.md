# Provider Onboarding — Phase 2: FileStorage integration + review loop

Phase 1 shipped onboarding draft/step/submit and made `/me/status` onboarding-aware. Phase 2 does two things:

1. Wire **FileStorage** into the MarineProvider BFF the way the module was actually designed — signed-URL upload,
   `FileId`-only in the domain, `AizenRemoteCall` for sync, RabbitMQ for background work, BFF as the
   authorization + enrichment layer.
2. Close the **review loop**: admin sends steps back, provider revises and re-submits, document review states are
   visible, and every status change notifies the provider.

---

## What already exists (do not rebuild)

`Aizen.Modules.FileStorage` is already built to the intended shape:

| Endpoint | Purpose |
|---|---|
| `POST /api/v1/upload-sessions` | create session → signed upload URL (`FileUploadSessionDto`) |
| `POST /api/v1/upload-sessions/{code}/complete` | finalize → `FileDto` |
| `POST /api/v1/files/{fileId}/access/read-url` | short-lived signed read URL (`FileAccessUrlDto`) |
| `POST /api/v1/files/{fileId}/access/validate-ownership` | `FileValidationResultDto` |
| `POST /api/v1/files/{fileId}/owners` | **claim / link to a domain entity** (`FileOwnerReferenceDto`) |
| `GET /api/v1/files/{fileId}` · `/metadata` · `DELETE` | file detail / metadata / delete |

`Aizen.Modules.FileStorage.Abstraction` already ships `RemoteCall/File/Responses/*`
(`CreateUploadSessionRemoteCallResponse`, `CreateFileReadUrlRemoteCallResponse`,
`ValidateFileOwnershipRemoteCallResponse`), the DTOs, the enums (`FileStatus`, `FileCategory`, `FileOwnerModule`,
`FileVisibility`, `UploadSessionStatus`) and `Message/FileLinkedToOwnerMessage`.

**The MarineProvider BFF has no FileStorage integration at all** — that is the gap.

---

## Defect 1 — documents are persisted without validating the file (must fix)

`AddOrganizerVerificationDocumentCommandHandler` takes `request.FileId` and writes the row. It never asks FileStorage
whether the file exists, was actually uploaded, belongs to this user, or is of an allowed type/size. This is exactly
the rule the architecture forbids: *a domain module must not attach a `FileId` without verifying ownership, upload
status and permitted use.* Any authenticated provider can currently attach **someone else's file id** to their own
profile.

**Fix (Identity):** before persisting a verification document, call FileStorage and require all of:
`IsValid == true` (ownership), `Status ∈ { Uploaded, Ready }`, allowed `ContentType`, size within limit, and the file
not already claimed by another owner. Only then create the row, and immediately **claim** it
(`POST /files/{fileId}/owners` with `OwnerModule = Identity`, `OwnerEntityType = "OrganizerVerificationDocument"`,
`OwnerEntityId = <document id>`). If the domain write fails after the claim, publish the cleanup message so the file
does not linger as `Orphaned`.

## Defect 2 — `FileId` type mismatch (Guid vs long)

FileStorage DTOs expose `Guid FileId`, but `VerificationDocumentEntity.FileId` is a **`long`**, and the FileStorage
controllers route on `{fileId:long}` (the internal PK). Domain modules and the frontend must use the **opaque `Guid`**
— sequential longs are enumerable, and the security rules explicitly require that file ids not be guessable.

**Fix:** `VerificationDocumentEntity.FileId` becomes `Guid` (migration `ChangeVerificationDocumentFileIdToGuid`,
with back-fill), and every provider-facing contract uses the `Guid`. Route the FileStorage endpoints by `Guid` too, or
keep `long` internally but never let it cross a module or BFF boundary.

## Defect 3 — `FileUploadSessionDto` leaks storage internals

It carries `BucketName` and `ObjectKey`. Those must never reach the BFF response or the frontend. The BFF returns a
narrowed contract: `{ fileId, uploadSessionCode, uploadUrl, expiresAt, requiredHeaders }` — nothing else. No bucket,
no object key, no provider, and **no signed URL is ever persisted** (not in the domain DB, not in frontend state).

---

## Token / identity propagation — use the CURRENT model, not the dual-token one

The older `X-Aizen-User-Token` dual-token model is **obsolete for this BFF**. `MarineProviderBffAuthDelegatingHandler`
already implements the current model and says so explicitly ("never forwards an Identity `X-Aizen-User-Token` and
never fabricates an Identity JWT"):

```
Authorization:                 Bearer <Keycloak service-account token>   (client_credentials, cached)
X-Aizen-Bff-Assertion:         <shared secret>            // module trusts the BFF's asserted identity
X-Aizen-User-Id:               <identity user id>
X-Aizen-Provider-Profile-Id:   <provider profile id>
```

FileStorage calls from the MarineProvider BFF go through the **same delegating handler** — nothing new is invented.
Two requirements follow:

- The BFF's Keycloak service-account token must be accepted by `file-storage-api` (audience `file-storage-api`; the
  per-module audience mapper on `provider-portal-bff` must include it).
- `file-storage-api` must honour the BFF assertion (`BffAssertion:SharedSecret` + `BffAssertion:AllowedClientIds`)
  exactly as `identity-api` does, so `UploadedByUserId` / ownership are attributed to the **real provider**, not to
  the BFF service account. Without this, every provider's files would look like they belong to one service user and
  ownership validation becomes meaningless.

## Sync vs async

- **Sync (`AizenRemoteCall`)** — anything the user waits for: create upload session, complete upload, validate
  ownership, create read URL, claim file, unlink.
- **Async (RabbitMQ)** — cleanup of unclaimed/orphaned uploads, virus scan, thumbnails, physical deletion,
  retention. Contracts under `Abstraction/Message`, consumers under `Api/Consumers`.

No Refit, no ad-hoc `HttpClient`.

---

## Flows

### Upload (onboarding document)

```
Frontend                     MarineProvider BFF                 FileStorage
  │ POST /provider/files/upload-session                          │
  │  {fileName, contentType, size, category}                     │
  │────────────────────────►│ authorize provider                 │
  │                         │ enforce allowlist (pdf/jpg/png),   │
  │                         │ max size, category                 │
  │                         │──── POST /api/v1/upload-sessions ─►│ create session, Pending
  │◄── {fileId, uploadSessionCode, uploadUrl, expiresAt} ────────│  (bucket/objectKey stripped)
  │ PUT {uploadUrl}  (direct to S3/MinIO, Content-Type header)   │
  │──────────────────────────────────────────────────────────────►
  │ POST /provider/files/{fileId}/complete {uploadSessionCode}   │
  │────────────────────────►│──── POST /upload-sessions/{code}/complete ─►│ verify object, size, mime → Uploaded
  │ POST /provider/onboarding/documents {fileId, docType, issuer}│
  │────────────────────────►│──── Identity: AddOrganizerVerificationDocument
  │                         │       ├─ validate-ownership + status + mime + size   ← Defect 1
  │                         │       ├─ persist row (FileId Guid + snapshots only)
  │                         │       └─ claim: POST /files/{fileId}/owners
```

The document row stores **only**: `FileId (Guid)`, `OriginalFileName`, `ContentType`, `SizeInBytes`, `DocumentType`,
`Issuer`, `UploadedAt`, `UploadedByUserId`. Never `BucketName`, `ObjectKey`, `SignedUrl`, `StorageProvider`.

### Read (deliberately lazy)

`GET /provider/onboarding` returns document **metadata only** — no URLs. A signed read URL is minted on demand:

```
POST /provider/onboarding/documents/{fileId}/access-url  →  { readUrl, expiresAt }
```

This is the pattern the architecture calls for: generating signed URLs for every row of a long list is wasteful and
leaks capability far beyond what the user actually opens.

### Delete

Domain row is removed/deactivated → BFF asks FileStorage to unlink the owner reference → if no other owner holds the
file, FileStorage raises a delete request → **physical deletion happens in a background consumer**. The BFF never
calls the storage SDK, and an audit record survives the file.

---

## Review loop (the rest of Phase 2)

- **Admin → revision:** `RequestOnboardingRevision(profileId, steps[], note)` (Identity, admin-facing) → onboarding
  `NeedsRevision`, the named steps flip to `NeedsRevision`, note stored. `/me/status` already returns
  `NeedsRevision`, so the guard routes the provider to `/status/needs-revision`; from there they resume, fix only the
  flagged steps, and re-submit (`Submitted`, revision cleared).
- **Document review states:** surface `NEEDS_REVIEW` / `REJECTED` + resolution note per document (admin sets them);
  the provider sees them on the Compliance step and can replace a rejected file.
- **Notifications:** publish on every onboarding status transition (`Submitted`, `NeedsRevision`, `Approved`,
  `Rejected`) via the existing Notification module — the provider currently learns nothing about their application.

---

## Risks

- **Assertion trust on file-storage-api is a prerequisite.** If it is not enabled, every uploaded file is attributed
  to the BFF service account, ownership validation degenerates, and Defect 1's fix is worthless. Verify this first.
- **`FileId` migration touches existing rows.** Back-fill `long → Guid` by resolving each id against FileStorage;
  rows whose file no longer exists become `Orphaned` and are reported, not silently dropped.
- **Signed URLs must stay short-lived and unstored.** A URL cached in frontend state or a DB column is a permanent
  capability leak.
- **Orphan cleanup must actually run.** Upload-then-abandon is the normal case (user closes the tab), so without the
  background consumer the bucket fills with unclaimed objects.
