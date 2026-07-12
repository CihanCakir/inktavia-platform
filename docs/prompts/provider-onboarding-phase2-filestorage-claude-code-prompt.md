# Claude Code Prompt — Provider Onboarding Phase 2: FileStorage integration + review loop

Read `docs/provider-onboarding-phase2-filestorage-design.md` first. It contains the decisions and the three defects
this prompt fixes. Do not re-litigate them.

Follow existing Aizen conventions exactly: CQRS (`AizenCommand`/`AizenCommandHandler`/`AizenQueryHandler`/
`AizenValidator`), `IAizenCQRSProcessor.ProcessAsync`, `AizenWebApiController` + `SetResponse`, FluentValidation,
EF Core, repository pattern, UTC `DateTime`, `AizenRemoteCall` for sync service calls (**never Refit, never a raw
`HttpClient`**), RabbitMQ (`IAizenMessagePublisher` / `AizenBaseMessageConsumer<T>`) for background work.
Produce complete, compile-ready files. No TODOs.

Phase 1 (onboarding draft/step/submit + `/me/status` → `CompleteOnboarding`) is already implemented. **Phase 1's
document upload used a multipart proxy — this prompt replaces it with the signed-URL flow. Delete the multipart
path; do not leave both.**

---

## Step 0 — Prerequisite: does `file-storage-api` trust the BFF assertion?

`MarineProviderBffAuthDelegatingHandler` already sends the current model (and must keep doing so):

```
Authorization:               Bearer <Keycloak service-account token>
X-Aizen-Bff-Assertion:       <shared secret>
X-Aizen-User-Id:             <identity user id>
X-Aizen-Provider-Profile-Id: <provider profile id>
```

**Do NOT introduce `X-Aizen-User-Token`.** That dual-token model is obsolete for this BFF.

Verify and, if missing, wire up:
1. `file-storage-api` accepts the BFF's Keycloak service token → audience `file-storage-api` must be in the
   `provider-portal-bff` audience mapper (same as `identity-api`).
2. `file-storage-api` honours the BFF assertion (`BffAssertion:SharedSecret`, `BffAssertion:AllowedClientIds__0 =
   provider-portal-bff`) and resolves `UserInfo.UserId` from `X-Aizen-User-Id`, exactly as `identity-api` does.
   Add the env wiring in `docker-compose.yaml` (`BffAssertion__SharedSecret: ${AIZEN_BFF_ASSERTION_SECRET}`).

**This is a hard prerequisite.** Without it, `UploadedByUserId` and file ownership are attributed to the BFF service
account, every provider looks like the same user, and the ownership validation in Part B is worthless. Prove it with
a smoke call before continuing.

---

## Part A — MarineProvider BFF ↔ FileStorage remote call

1. Reference `Aizen.Modules.FileStorage.Abstraction` from `Aizen.Bff.MarineProvider.Application`.
2. `Common/RemoteClients/IProviderFileStorageRemoteCall.cs` using `AizenRemoteCall` attributes (mirror
   `IProviderIdentityRemoteCall`):

```csharp
[AizenRemoteCallPost("/api/v1/upload-sessions")]
Task<AizenApiResponse<FileUploadSessionDto>> CreateUploadSession([AizenRemoteCallBody] CreateUploadSessionRequest request);

[AizenRemoteCallPost("/api/v1/upload-sessions/{uploadSessionCode}/complete")]
Task<AizenApiResponse<FileDto>> CompleteUploadSession(string uploadSessionCode, [AizenRemoteCallBody] CompleteUploadSessionRequest request);

[AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
Task<AizenApiResponse<FileAccessUrlDto>> CreateReadUrl(Guid fileId, [AizenRemoteCallBody] CreateReadUrlRequest request);

[AizenRemoteCallPost("/api/v1/files/{fileId}/access/validate-ownership")]
Task<AizenApiResponse<FileValidationResultDto>> ValidateOwnership(Guid fileId, [AizenRemoteCallBody] ValidateOwnershipRequest request);

[AizenRemoteCallPost("/api/v1/files/{fileId}/owners")]
Task<AizenApiResponse<FileOwnerReferenceDto>> LinkToOwner(Guid fileId, [AizenRemoteCallBody] LinkFileToOwnerRequest request);
```

3. Register it in `Aizen.Bff.MarineProvider.Application/DependencyInjection.cs` next to the existing remote calls, so
   it goes through `MarineProviderBffAuthDelegatingHandler`.
4. Config: `RemoteCalls__IProviderFileStorageRemoteCall__BaseUrl` → `http://file-storage-api:8080` in
   `docker-compose.yaml` + the appsettings files.

## Part B — Fix Defect 1: never persist an unvalidated `FileId` (Identity)

`AddOrganizerVerificationDocumentCommandHandler` currently writes `request.FileId` straight to the DB with **no
checks**. Any provider can attach another provider's file. Rewrite it to, in order:

1. Resolve the caller's profile; confirm the target profile is theirs.
2. Call FileStorage `validate-ownership` for the `FileId` → require `IsValid == true`.
3. Fetch the file (`GET /files/{fileId}`) → require `Status ∈ { Uploaded, Ready }`, `ContentType` in the allowlist
   (`application/pdf`, `image/jpeg`, `image/png`), `SizeInBytes <= 10 MB`, and the file **not already claimed by a
   different owner**.
4. Persist the document row with **snapshots only**: `FileId (Guid)`, `OriginalFileName`, `ContentType`,
   `SizeInBytes`, `DocumentType`, `Issuer`, `UploadedAt`, `UploadedByUserId`.
   **Never store `BucketName`, `ObjectKey`, `SignedUrl`, `StorageProvider`.**
5. Claim the file: `POST /files/{fileId}/owners` with `OwnerModule = Identity`,
   `OwnerEntityType = "OrganizerVerificationDocument"`, `OwnerEntityId = <document id>`.
6. If step 4 or 5 fails, publish the cleanup message so the file does not linger as `Orphaned`.

Identity calls FileStorage through its own `AizenRemoteCall` contract (`IFileStorageRemoteCall` in
`Aizen.Modules.Identity.Application/Common/RemoteClients/`), configured with
`RemoteCalls__IFileStorageRemoteCall__BaseUrl`.

## Part C — Fix Defect 2: `FileId` becomes `Guid`

`VerificationDocumentEntity.FileId` is `long`; FileStorage's public id is a `Guid`. Sequential longs are enumerable
and must not cross a boundary.

- Migration `ChangeVerificationDocumentFileIdToGuid`: add `FileId uuid`, back-fill by resolving each existing `long`
  against FileStorage, drop the old column. Rows whose file no longer exists are marked `Orphaned` and **reported in
  the migration output — do not silently drop them**.
- Every provider-facing contract (Identity DTO, BFF contract, frontend type) uses the `Guid`.
- The internal `long` id must never appear in a BFF response.

## Part D — BFF provider-facing file endpoints

`Controllers/V1/Files/ProviderFileController.cs`, route `api/v1/provider/files`, provider-authenticated:

| Method | Path | Notes |
|---|---|---|
| POST | `/upload-session` | `{fileName, contentType, size, category}` → creates the session |
| POST | `/{fileId:guid}/complete` | `{uploadSessionCode}` → finalizes |

and on the onboarding controller:

| Method | Path | Notes |
|---|---|---|
| POST | `/onboarding/documents` | `{fileId, documentType, issuer?}` → validate + persist + claim |
| DELETE | `/onboarding/documents/{fileId:guid}` | only while onboarding is not `Submitted` |
| POST | `/onboarding/documents/{fileId:guid}/access-url` | **on-demand** short-lived signed read URL |

**Fix Defect 3 here:** the BFF's upload-session response must be a narrowed contract —
`{ fileId, uploadSessionCode, uploadUrl, expiresAt, requiredHeaders }`. `FileUploadSessionDto` carries `BucketName`
and `ObjectKey`; **strip them.** They must never reach the frontend.

`GET /onboarding` returns document **metadata only — no URLs**. Signed read URLs are minted only by the `access-url`
endpoint, when the user actually opens a document. Never persist a signed URL anywhere.

Validators (BFF): content-type allowlist, max size 10 MB, `category = Document | Certificate`, filename sanitized
against path traversal. Do not trust the client's `contentType` alone — FileStorage re-checks on complete.

## Part E — Review loop

1. **Identity:** `RequestProviderOnboardingRevisionCommand(profileId, steps[], note)` (admin) → onboarding
   `NeedsRevision`, named steps → `NeedsRevision`, note stored. Re-submit clears the revision state.
2. **Document review states:** add `ReviewStatus` (`Pending | Approved | NeedsReview | Rejected`) + `ResolutionNote`
   to the verification document; admin sets them; the provider sees them and can replace a rejected file.
3. **Notifications:** publish on every onboarding transition (`Submitted`, `NeedsRevision`, `Approved`, `Rejected`)
   through the existing Notification module (new `NotificationType` values + email templates, tr + en). The provider
   currently receives nothing about their application.
4. **Background cleanup consumer:** consume the orphan/cleanup message → ask FileStorage to delete unclaimed files
   older than N hours. Contracts in `Abstraction/Message`, consumer in `Api/Consumers`.

## Part F — Frontend (`inktavia-marine-provider-web`)

1. `onboardingApi.ts`: replace the multipart `uploadDocument` with the three-step flow:
   `createUploadSession(file)` → `PUT uploadUrl` (**direct to S3/MinIO, plain `fetch`, `Content-Type` header, no
   Authorization header**) → `completeUpload(fileId, uploadSessionCode)` → `attachDocument(fileId, documentType,
   issuer?)`. Show real progress; on failure at any step, do not attach.
2. `ComplianceVerificationPage`: document list from the server (metadata only). A document opens via
   `POST /onboarding/documents/{fileId}/access-url` **at click time** — never pre-fetch URLs for the whole list, and
   never store a signed URL in component state beyond its use.
3. Show `ReviewStatus` + `ResolutionNote` per document; allow replacing a `Rejected` one.
4. `NeedsRevision`: `/status/needs-revision` lists the flagged steps + the admin note and deep-links into each.
5. Types: `fileId` is a **`string` (Guid)**, never a number. i18n keys in tr + en.

---

## Tests + smoke

Unit (Identity):
- Attaching a `FileId` the caller does not own → rejected.
- Attaching a file in `Created` / `UploadUrlGenerated` (not yet uploaded) → rejected.
- Disallowed content type / oversize → rejected.
- Already-claimed file → rejected.
- Successful attach → row persisted **and** file claimed; document row contains no bucket/objectKey/URL.
- Domain write fails after claim → cleanup message published.

Smoke (write results to `docs/provider-onboarding-phase2-report.md`):
1. **Assertion check:** upload as provider A → the file's `UploadedByUserId` is provider A's user id, **not** the BFF
   service account. If it is the service account, stop — Step 0 is not done.
2. Upload session → `PUT` to MinIO → complete → attach → document appears in `GET /onboarding`.
3. `GET /onboarding` response contains **no** URL, bucket or object key anywhere.
4. `access-url` returns a working short-lived URL; it expires.
5. Provider B attempts to attach provider A's `fileId` → **rejected** (this is the vulnerability being closed).
6. Delete a document → owner reference unlinked → cleanup message published.
7. Admin requests revision → provider lands on `/status/needs-revision`, fixes the step, re-submits → `Submitted`.
8. Approve → `Completed` + `EnterWorkspace` + notification received.

## Constraints

- No `X-Aizen-User-Token`. Use the existing service-token + BFF-assertion handler.
- No Refit / raw `HttpClient`. Sync = `AizenRemoteCall`; background = RabbitMQ.
- Domain entities store `FileId` + snapshots only — never bucket, object key, signed URL or provider.
- Signed URLs are short-lived, minted on demand, and never persisted (DB or frontend state).
- A `FileId` is never attached to a domain entity without ownership + upload-status + type/size validation.
