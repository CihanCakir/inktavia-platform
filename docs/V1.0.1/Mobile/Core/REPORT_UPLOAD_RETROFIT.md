# REPORT — CORE_UPLOAD_RETROFIT (avatar M3c + vessel docs M4e → client-side directUpload; relay removed)

**Goal:** migrate the two remaining server-side relay uploads (avatar, vessel documents) onto the M4f canonical **client-side presigned** flow (`directUpload` → bytes straight to storage → BFF attaches the fileId) and **delete the relay code**.
**Status:** **DONE + live-verified** — both now upload bytes directly to storage (bypassing the BFF); the old multipart endpoints return **415**; byte-identical round-trips. `npx tsc --noEmit` = 0; BFF build 0 errors. NOT committed.

---

## What moved to client-side

Both surfaces now: RN `directUpload(file)` → `POST /mobile/uploads/session` (`ServerSideUpload=false`) → **plain `fetch` PUT bytes to the presigned URL (PublicServiceUrl, not the BFF)** → `POST /mobile/uploads/complete` → fileId; then the BFF **attaches the fileId** (owner-gated).

| Surface | Old (relay) | New (attach-by-fileId) |
|---|---|---|
| **Avatar** | `POST /mobile/profile/avatar` multipart `IFormFile` → BFF `CreateUploadSession(ServerSideUpload=true)` → BFF PUTs bytes → complete → set ProfilePhotoUrl | `POST /mobile/profile/avatar { fileId }` → set ProfilePhotoUrl = fileId → return profile with a fresh presigned avatar URL (`ResolveAvatarUrlAsync` kept) |
| **Vessel docs** | `POST /mobile/vessels/{id}/documents` multipart `IFormFile` + fields → BFF session(true) → BFF PUTs bytes → complete → AddVesselDocument | `POST /mobile/vessels/{id}/documents { fileId, documentTypeCode, documentName?, expiresAt?, notes? }` → owner-gate → AddVesselDocument(fileId) → consumer-side invalidation kept |

## Relay code removed (BFF)
- `UploadParticipantAvatarCommandHandler`: dropped the `CreateUploadSession(ServerSideUpload=true)` + `UploadBytesAsync` (plain-client S3 PUT) + `CompleteUploadSession` + `IHttpClientFactory`. Command dropped `Content/FileName/ContentType/SizeInBytes` → `{ Guid FileId }`. Controller `[FromForm] IFormFile` → `[FromBody] { fileId }`.
- `UploadMobileVesselDocumentCommandHandler`: dropped the same session→PUT→complete relay + `IHttpClientFactory` + `UploadBytesAsync`. Command → `{ VesselId, FileId, DocumentTypeCode, … }`. Controller `[FromForm]` → `[FromBody] UploadMobileVesselDocumentRequest` (now carries `FileId`).
- Net on the two handlers+commands: **+30 / −122 lines** (relay deleted). Confirmed no remaining `ServerSideUpload=true` / `UploadBytesAsync` / `ByteArrayContent` / `IFormFile` / `[FromForm]` in the mobile BFF (the only `IHttpClientFactory` left is the Keycloak token provider — unrelated). The FileStorage `ServerSideUpload` capability itself is untouched (still used by Payment PDF services, server-generated, no client).

---

## Verification — live (`localhost:17003`, `qa.owner.aug5`, vessel `100013`)

**Avatar**
```
1) session uploadUrl host = http://localhost:9000   ← PublicServiceUrl, NOT the BFF ✅
2) direct PUT bytes to that URL → HTTP 200            ← bytes bypass the BFF ✅
3) POST /profile/avatar { fileId } → ok, avatarUrl set ✅
4) GET /profile/me → avatarUrl; download → 200 image/jpeg, md5 == original  (byte-identical) ✅
5) OLD multipart POST /profile/avatar → HTTP 415       ← relay endpoint gone ✅
```

**Vessel documents**
```
1) session uploadUrl host = http://localhost:9000 (NOT the BFF) ✅
2) direct PUT → HTTP 200 (bytes bypass the BFF) ✅
3) POST /vessels/100013/documents { fileId, documentTypeCode:INSURANCE } → ok, url present ✅
4) GET …/documents → count=1 fresh; download → 200 application/pdf, md5 == original (byte-identical) ✅
5) delete → ok;  OLD multipart POST …/documents → HTTP 415  ← relay endpoint gone ✅
```

---

## FE (`inktavia-marine-mobile`) — `npx tsc --noEmit` = 0
- `profileApi.uploadAvatar(asset)` — now `directUpload(image) → { fileId }` POST (was multipart FormData). Signature unchanged → **ProfileScreen untouched**; it still invalidates `profile.me`.
- `vesselDocumentsApi.uploadVesselDocument(vesselId, input)` — now `directUpload(file) → { fileId, documentTypeCode, … }` POST (was multipart). Signature unchanged → **VesselDocumentsScreen + create-wizard doc step untouched**.
- Mock parity: docs POST handler now parses JSON `{ fileId, documentTypeCode }` (was FormData `_parts`); the avatar mock already ignored the body. `directUpload` skips the raw PUT under mock (M4f).

---

## Files (scoped) — isolated BE + FE

**BE** (`Bff/src/Marine.Participant.Mobile/**`): `Profile/Command/UploadParticipantAvatar/{Command,Handler}.cs`, `Controllers/V1/ProfileController.cs`, `Contracts/Profile/UploadParticipantAvatarRequest.cs` *(new)*; `Vessel/Command/UploadMobileVesselDocument/{Command,Handler}.cs`, `Controllers/V1/VesselDocumentsController.cs`, `Contracts/Vessel/MobileVesselDocumentDtos.cs` (FileId on the request); `Common/RemoteClients/IFileStorageRemoteCall.cs` (doc comment).
**FE**: `src/features/profile/api/profileApi.ts`, `src/features/vessels/api/vesselDocumentsApi.ts`, `src/core/mock/handlers/vessels.handlers.ts`.

No infra changes (public presigned already exists). No other feature. `git diff` shows relay-code deletions (−122 lines on the two handlers). Pre-existing unrelated WIP (ServiceRequest/AdminPanel) not touched. NOT committed.
