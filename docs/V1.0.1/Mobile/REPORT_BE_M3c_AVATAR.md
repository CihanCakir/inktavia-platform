# BE_M3c — Mobile avatar upload (FileStorage) + FE avatar picker

**Date:** 2026-08-06 · **Repos:** `addesso-project` (BFF) + `inktavia-marine-mobile` (RN). Third M3 slice.
Build 0 errors, redeployed, upload→/me→retrieve verified. No FileStorage source changes (config + remote-call only).

## Endpoint (mobile BFF, `[Authorize]` mobile_user)
- `POST /api/v1/mobile/profile/avatar` — multipart form field `file` → stores via FileStorage → sets the
  participant `ProfilePhotoUrl` → returns the updated `GetParticipantProfileResponse` (M3a shape) whose
  `avatarUrl` is a fresh presigned read URL. 10 MB request limit.

## Flow (server-side upload — client just sends the image)
1. Resolve the participant by Keycloak subject (sets the identity holder for the later asserted Identity update).
2. `IFileStorageRemoteCall.CreateUploadSession` → module `POST /api/v1/upload-sessions` with **`ServerSideUpload=true`**
   (Category=Image, Visibility=Private, OwnerModule=Identity). The presigned PUT URL is signed for the **internal**
   S3 endpoint (`http://minio:9000`) so the BFF can push the bytes.
3. BFF PUTs the image bytes to that presigned URL with a **plain HttpClient** (`IHttpClientFactory.CreateClient()` —
   no delegating handler, so no Bearer/assertion that would break the S3 signature).
4. `CompleteUploadSession(code)` → `FileDto` → `fileId`.
5. Asserted `IIdentityRemoteCall.UpdateParticipantProfile { ProfilePhotoUrl = fileId }` (M3a path; writes the
   Identity profile table only).
6. Re-resolve → map → **resolve avatar read URL** and return.

**Read-URL resolution (M3a read path enhanced):** `ProfilePhotoUrl` now stores the FileStorage **fileId**. The
GET/PUT/avatar responses run `ResolveAvatarUrlAsync` — if `avatarUrl` parses as a Guid, mint a fresh 6h presigned
read URL via `CreateReadUrl` (signed for the public `http://localhost:9000`, reachable from the device); legacy
non-Guid values pass through; a resolution failure nulls the avatar (never 500s).

## BffAssertion allow-list — HIT (as the task predicted)
The resolve-first flow sets the identity holder, so the delegating handler attaches the BFF assertion to the
**upload-session** calls too. `file-storage-api` only allow-listed `provider-portal-bff`/`admin-panel-bff`, so
without the fix the upload would 400 (AizenUserInfoMiddleware 1102). Added
`BffAssertion__AllowedClientIds__2: marine-mobile-bff` to `file-storage-api` (docker-compose env, no rebuild).
(M3a had added it only to identity-api.)

## Files
**BFF (`Bff/src/Marine.Participant.Mobile/**`):**
- `Aizen.Bff.Marine.Participant.Mobile/Controllers/V1/ProfileController.cs` (edit — `POST avatar`)
- `Application/Profile/Command/UploadParticipantAvatar/{Command,Handler}.cs` (new)
- `Application/Common/RemoteClients/IFileStorageRemoteCall.cs` (new — CreateUploadSession/Complete/CreateReadUrl)
- `Application/Profile/Query/GetParticipantProfile/GetParticipantProfileQueryHandler.cs` (edit — `ResolveAvatarUrlAsync` + IFileStorageRemoteCall)
- `Application/Profile/Command/UpdateParticipantProfile/UpdateParticipantProfileBffCommandHandler.cs` (edit — resolve avatar url on the update response too)
- `Application/DependencyInjection.cs` (edit — register `IFileStorageRemoteCall`)
- `…Application.csproj` (edit — ProjectReference to `Aizen.Modules.FileStorage.Abstraction`)

**Config (`docker-compose.yaml`):**
- `bff-marine-mobile`: `RemoteCalls__IFileStorageRemoteCall__BaseUrl: http://file-storage-api:8080`
- `file-storage-api`: `BffAssertion__AllowedClientIds__2: marine-mobile-bff`

No FileStorage/Identity source changed. Service account already holds `file_storage_read/write` (M1) — no grant needed.

## Verification (redeployed `bff-marine-mobile` + recreated `file-storage-api`; `localhost:17003`; user `qa.owner.aug5@inktavia.com`)
```
POST /api/v1/mobile/profile/avatar  (multipart file=1x1 png, image/png)
 → 200  body.profile.avatarUrl = http://localhost:9000/inktavia-filestorage-local/image/2026/08/BB870481….png?<presigned>

GET /api/v1/mobile/profile/me
 → 200  body.profile.avatarUrl = http://localhost:9000/…/BB870481….png?<presigned>

GET <that avatarUrl>
 → HTTP 200  bytes=70  content-type=image/png   (downloaded = valid PNG, the exact uploaded file)

PUT /api/v1/mobile/profile/me {firstName,lastName}   (M3a regression)
 → 200  fullName="QA Owner Aug5"  avatarUrl still resolves
```

## Scope
Only the mobile BFF + two docker-compose env lines. The `addesso-project` tree was committed by an external
process mid-session (M3c files tracked). FE (`inktavia-marine-mobile`) uncommitted. No other feature/backend source touched.
