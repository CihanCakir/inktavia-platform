# REPORT — BE_M4e_VESSELS_DOCUMENTS (seed DOCUMENT_TYPE + vessel document upload/list/delete on mobile BFF + FE)

**Goal:** seed the DOCUMENT_TYPE lookup, expose vessel document list/upload/delete on the mobile BFF (M3c server-side FileStorage pattern), invalidate the consumer-side documents read so the list is fresh with no flush, and wire the FE manage surface + create-wizard doc step.
**Status:** **DONE + live-verified** (upload → list-fresh → byte-identical presigned download → delete-drops-immediately; ownership + foreign-id all clean). FE wired; `npx tsc --noEmit` = 0; mock parity added. Full RN device run blocked headless (per prior slices).
**NOT committed** (per instruction).

**Scope note / approved deviation:** M4e hit a **pre-existing broken S2S integration** — the vessel module's `AddVesselDocument` validates the file via a FileStorage remote-call contract that pointed at a **never-built** `/api/v1/file-storage/*` prefix with bare (un-enveloped) response bodies, so vessel document attach had *never worked*. With explicit approval, this slice fixes that contract (contained to the vessel module — the only consumer). Files therefore extend beyond Bff+seed+FE to `FileStorage.Abstraction` + `VesselFileStorageService` + a `docker-compose` env, all documented below.

---

## 0. DOCUMENT_TYPE seed (Step 0)

- The `DOCUMENT_TYPE` **group** already existed in `lookup-groups.json`; it had **zero items**. Added 7 items to `lookup-items.json` (additive; the seeder upserts + inserts new codes on an already-seeded DB): `REGISTRATION` (default), `INSURANCE`, `SAFETY_CERTIFICATE`, `TONNAGE_CERTIFICATE`, `RADIO_LICENSE`, `SURVEY_REPORT`, `OTHER`.
- Rebuilt reference-data-api (the JSON is copied into the image — needed `--no-cache` as the layer cache served the stale JSON), redeployed, **flushed reference Redis DB 12**.
- **Verified:** `GET /api/v1/mobile/reference/DOCUMENT_TYPE` → 7 items (tr names).

---

## 1. Endpoints exposed (BFF)

All `[Authorize(ParticipantAuthenticated)]`, owner-gated (caller's default-page owned set → clean not-found for a foreign/unknown vessel id).

| Mobile BFF | Behaviour |
|---|---|
| `GET /api/v1/mobile/vessels/{id}/documents` | Owner-gated list; each doc carries a freshly-resolved presigned `downloadUrl`. |
| `POST /api/v1/mobile/vessels/{id}/documents` (multipart) | Server-side FileStorage upload (M3c pattern) → attach to the vessel with its DOCUMENT_TYPE → returns the created doc. |
| `DELETE /api/v1/mobile/vessels/{id}/documents/{docId}` | Removes the doc (owner + doc-membership gated). |

**Upload flow (M3c mirror):** resolve participant → owner-gate → `CreateUploadSession(ServerSideUpload=true, Category=Document)` → PUT bytes with a plain HttpClient (URL carries its own S3 signature) → `CompleteUploadSession` → `AddVesselDocument(FileId, DocumentTypeCode, DocumentName)`.

### Consumer-side freshness (the M4e cache subtlety)
The module's documents read (`GetVesselDocumentsQueryHandler`) **is** decorator-cached, keyed by `(VesselId, PageIndex, PageSize, IncludeAccessUrls, AccessUrlExpiresInMinutes)`. `AddVesselDocument`/`RemoveVesselDocument` already invalidate the **`IncludeAccessUrls=false` default page**. So the BFF reads that exact page — `GetVesselDocuments(id, 0, 20, includeAccessUrls:false)` — and **resolves each doc's presigned URL itself** (per-doc `CreateReadUrl`; FileStorage has no read cache so it's always fresh). This keeps the list fresh with no flush **and** avoids the never-invalidated `includeAccessUrls=true` cache variant. (No module invalidation change was needed — the fix is "read the page the write path invalidates," the recurring M4 lesson.)

### Two BFF-side projection filters (mirror M4d)
- **List:** the module's "remove" **deactivates** (`IsActive=false`) rather than hard-deleting, and its list read returns inactive rows — so the BFF list filters `.Where(d => d.IsActive)` (a deleted doc drops out).
- **Delete:** the module returns a raw 500 for a missing doc id; the BFF pre-checks the doc is in the caller's active doc set → clean `"Document not found."` instead.

---

## 2. The vessel↔FileStorage S2S fix (approved deviation)

`Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.IFileStorageRemoteCall` (consumed **only** by `VesselFileStorageService`) targeted `/api/v1/file-storage/files/{id}` (no such route) and declared **bare** response types, while the real endpoints are `/api/v1/files/{id}/metadata` etc. returning **enveloped** `AizenApiResponse<T>`. Fixed the three methods the vessel module actually uses, mirroring the working `IIdentityFileStorageRemoteCall` pattern:

| Method | New route | New return |
|---|---|---|
| `GetFileMetadata` | `GET /api/v1/files/{fileId}/metadata` | `AizenApiResponse<FileMetadataDto>` |
| `CreateReadUrl` | `POST /api/v1/files/{fileId}/access/read-url` | `AizenApiResponse<FileAccessUrlDto>` |
| `LinkFileToOwner` | `POST /api/v1/files/{fileId}/owners` | `AizenApiResponse<FileOwnerReferenceDto>` |

- `VesselFileStorageService` updated to read `.Body` and use the module request types (`CreateReadUrlRequest`, `LinkFileToOwnerRequest`).
- `FileStorage.Abstraction.csproj` gained a `Aizen.Core.Api.Abstraction` reference (for `AizenApiResponse`; Identity/Payment abstractions get it transitively).
- The four unused methods (validate-ownership / upload-session×2 / delete) were left as-is (dead; still target the unbuilt prefix) to minimise churn.
- **`docker-compose.yaml`:** added `RemoteCalls__IFileStorageRemoteCall__BaseUrl: http://file-storage-api:8080` to vessel-api (it was absent — "BaseAddress must be set" was the first error).

Contained to the vessel module; these routes were 100%-broken before, so the change can only fix.

---

## 3. Verification — live (`localhost:17003`; `qa.owner.aug5`; vessel `100013`; **no DB11/12 flush during the doc run**)

```
GET /mobile/reference/DOCUMENT_TYPE → 7 items ✅

POST /mobile/vessels/100013/documents  (regcert.pdf, documentTypeCode=REGISTRATION)
  → 200  type=REGISTRATION ct=application/pdf size=58  downloadUrl=http://localhost:9000/...pdf

GET /mobile/vessels/100013/documents  (fresh, no flush)
  → count=1  id=100001  downloadUrl present   ← consumer-side freshness ✅

download presigned URL
  → HTTP 200  content-type application/pdf  size 58
  → md5(downloaded) == md5(original)  8920804f753a77d3293117d9ee7212c5   ← byte-identical ✅

DELETE /mobile/vessels/100013/documents/100001 → ok
GET …/documents → count=0   ← drops immediately (IsActive filter + module invalidation) ✅

foreign VESSEL 20001 list  → ok=false (business not-found)
foreign VESSEL 20001 upload → ok=false (business not-found)
foreign/unknown DOC id delete on own vessel → ok=false (business not-found)   ← all clean, no 500 ✅
```

BFF + vessel-api + reference-data-api builds: **0 errors**; images rebuilt + redeployed.

---

## 4. FE (`inktavia-marine-mobile`) — `npx tsc --noEmit` = 0

- **`VesselDocumentsScreen`** (rewritten off `MOCK_DOCS`) = primary manage surface: real list via `useVesselDocuments`; **upload** = DOCUMENT_TYPE picker sheet (M3b `useReferenceLookup('DOCUMENT_TYPE')`) → OS document picker → multipart upload → invalidate; **view/download** opens the presigned URL (`Linking`); **delete** with confirm. Loading / empty (`EmptyState`) / error states.
- **`AddVesselDocumentsScreen`** (wizard step 4): on submit it now creates the vessel **and** best-effort uploads the collected reg/insurance/BLC files to it (mapped to `REGISTRATION`/`INSURANCE`/`SAFETY_CERTIFICATE`); per-file failures are non-fatal ("add later" on the Documents screen). No inline/mock doc data.
- **`VesselDetailScreen`**: the Documents card shows a real 2-item preview (`useVesselDocuments`) and MANAGE navigates with `vesselId`.
- **API** (`vesselDocumentsApi.ts`): `useVesselDocuments`, `uploadVesselDocument` (FormData), `deleteVesselDocument`; endpoints `DOCUMENTS` (+ new `DOCUMENT_BY_ID`); nav `VesselDocuments: { vesselId }`.
- **Mock parity:** RegExp handlers for numeric-id GET/POST(multipart)/DELETE documents (per-vessel in-memory store) so mock-ON works; mock-OFF hits the BFF.

---

## 5. Files (scoped)

**BE — mobile BFF** (`Bff/src/Marine.Participant.Mobile/**`): `Common/RemoteClients/IVesselRemoteCall.cs` (+3 document calls); `Contracts/Vessel/MobileVesselDocumentDtos.cs` *(new)*; `Vessel/MobileVesselDocumentMapper.cs` *(new)*; `Vessel/Query/GetMobileVesselDocuments/**` *(new)*; `Vessel/Command/{UploadMobileVesselDocument,DeleteMobileVesselDocument}/**` *(new)*; `Controllers/V1/VesselDocumentsController.cs` *(new)*.
**BE — seed:** `Modules/ReferenceData/.../Seed/Json/Lookup/lookup-items.json` (7 DOCUMENT_TYPE items).
**BE — approved S2S fix:** `Modules/FileStorage/.../RemoteCall/File/IFileStorageRemoteCall.cs`, `Modules/FileStorage/.../Aizen.Modules.FileStorage.Abstraction.csproj`, `Modules/Vessel/.../Service/FileStorage/VesselFileStorageService.cs`, `docker-compose.yaml` (vessel-api file-storage BaseUrl).
**FE** (`inktavia-marine-mobile`): `src/features/vessels/api/vesselDocumentsApi.ts` *(new)*; `src/features/vessels/screens/{VesselDocumentsScreen,AddVesselDocumentsScreen,VesselDetailScreen}.tsx`; `src/app/navigation/types.ts`; `src/core/api/endpoints.ts`; `src/core/mock/handlers/vessels.handlers.ts`; `src/core/i18n/locales/{en,tr}.json`.

No media/photos (M4f), no other feature. Pre-existing unrelated working-tree changes (ServiceRequest dispute WIP, AdminPanel envelope-handler WIP) are **not** from this slice. NOT committed.
