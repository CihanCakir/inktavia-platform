# REPORT — BE_M4f_VESSELS_MEDIA (client-side presigned photos + gallery + list-cover — closes M4)

**Goal:** vessel PHOTOS via the canonical **client-side presigned** upload (bytes bypass the BFF), a gallery (add/view/delete/set-cover), and list-cover enrichment so Home/list/picker show a real image. Establishes the reusable `directUpload` primitive.
**Status:** **DONE + live-verified** — the byte PUT hits the presigned URL (`localhost:9000` = PublicServiceUrl) directly, the BFF only brokers session+complete+attach; gallery fresh, byte-identical download, set-cover immediate on `GET /mobile/vessels`, delete, clean not-found. `npx tsc --noEmit` = 0; mock parity added. NOT committed.
**Prereq flagged:** physical device / prod needs `MINIO_PUBLIC_URL` set to a device-reachable host (the iOS simulator reaches the default `http://localhost:9000`).

---

## A) Reusable client-side presigned upload (`directUpload`)

- **BFF (reusable):** `POST /api/v1/mobile/uploads/session` → `CreateUploadSession(ServerSideUpload=false)` → returns `{ fileId, uploadUrl, uploadSessionCode, expiresAt, requiredContentType }` (the presigned PUT is signed against the **public** endpoint). `POST /api/v1/mobile/uploads/complete { uploadSessionCode }` → `CompleteUploadSession` (module reads the object back + magic-byte verifies) → `{ fileId }`. **No bytes flow through the BFF.**
- **RN (`src/core/api/directUpload.ts`):** `directUpload(file, category)` = (1) request session, (2) `fetch(uploadUrl, {method:'PUT', headers:{'Content-Type': requiredContentType}, body: blob})` — plain `fetch`, no BFF, no auth header, exact Content-Type, (3) call complete → `fileId`. The single upload primitive going forward (docs/avatar retrofit onto it). Under mock the raw PUT is skipped (mock URL isn't real storage; `fetch` isn't intercepted).

---

## B) Vessel media — BFF (`Bff/src/Marine.Participant.Mobile/**`)

| Endpoint | Behaviour |
|---|---|
| `GET /mobile/vessels/{id}/media` | Owner-gated list (cover first); the BFF reads the module's **includeAccessUrls=false** page (the key `InvalidateMediaAsync` evicts) and resolves each item's presigned read URL per-item. Active-only. |
| `POST /mobile/vessels/{id}/media { fileId }` | Attach a completed upload as a `Photo`. The module validates the file via the FileStorage S2S path (fixed in M4e). **First photo auto-covered.** |
| `DELETE /mobile/vessels/{id}/media/{mediaId}` | Owner + media-membership gated (module 500s on missing id → clean not-found). |
| `POST /mobile/vessels/{id}/media/{mediaId}/cover` | Set cover; returns the re-read gallery. |

Same consumer-side freshness rule as M4e docs; delete/set-cover pre-check media membership for clean not-found.

---

## C) List-cover enrichment (module)

- `GetUserVesselsQueryHandler` previously hardcoded `CoverMediaUrl=null`. Now it fetches each vessel's **active cover photo FileId** (second small query over `VesselMediaEntity`) and resolves a **presigned read URL** (30-min TTL, comfortably above the 10-min list cache), populating `CoverMediaUrl` — so `GET /mobile/vessels` + Home card + top-bar picker render a real cover.
- **Freshness companion (justified deviation beyond the single-file scope):** the cached user-list must be evicted when the cover changes, so `AddVesselMedia` / `RemoveVesselMedia` / `SetCoverVesselMedia` each now call `InvalidateUserVesselListAsync(currentUserId)` (the list is keyed by the owner's UserId; the participant edits their own vessel). Without this the populated cover would lag to TTL — the cover fix is only "immediate" with it (mirrors the M4d status→list 1-liner). 3 one-line additions.

---

## D) Verification — live (`localhost:17003`; `qa.owner.aug5`; vessel `100013`; **no flush**)

```
1) POST /mobile/uploads/session  → 200  requiredContentType=image/jpeg
   uploadUrl host = http://localhost:9000   ← PublicServiceUrl, NOT the BFF, NOT minio:9000 ✅
2) PUT bytes DIRECTLY to uploadUrl (plain client → storage) → HTTP 200   ← bytes bypass the BFF ✅
3) POST /mobile/uploads/complete → 200  fileId=…   (magic-byte verified real JPEG) ✅
4) POST /mobile/vessels/100013/media { fileId } → 200  isCover=true (first photo)  url present ✅
5) GET  /mobile/vessels/100013/media → count=1 fresh, presigned url ✅
6) GET  /mobile/vessels → vessel 100013 coverMediaUrl POPULATED ✅   ← list-cover enrichment
7) download the media url → HTTP 200 image/jpeg, md5(downloaded)==md5(original) ✅  byte-identical
8) 2nd photo + set-cover on it → gallery covers move [(100002,true),(100001,false)];
   GET /mobile/vessels cover updated immediately (user-list invalidated on set-cover) ✅
9) delete both → gallery count=0 ✅
   foreign vessel 20001 media list → business not-found (9999) ✅
   foreign/unknown media delete on own vessel → business not-found (9999) ✅
```
(Negative control earlier: random bytes with `image/jpeg` → complete 500 "magic byte verification failed" — the storage content-check works; a real JPEG passes.)

BFF + vessel-api builds **0 errors**; both images rebuilt + redeployed.

---

## E) FE (`inktavia-marine-mobile`) — `npx tsc --noEmit` = 0

- **`directUpload`** primitive (`src/core/api/directUpload.ts`) + `UPLOADS.SESSION/COMPLETE` endpoints.
- **`vesselMediaApi.ts`**: `useVesselMedia`, `uploadVesselPhoto` (= directUpload → attach), `attachVesselMedia`, `deleteVesselMedia`, `setVesselMediaCover`; `MEDIA`/`MEDIA_BY_ID`/`MEDIA_COVER` endpoints; `vessels.media` query key.
- **`VesselGallery`** on VesselDetail: library picker → `uploadVesselPhoto` (client-side direct) → attach; horizontal thumbnails with cover badge; tap → set-cover / delete (confirm); loading/empty. Invalidates media + detail + list + all.
- **Home active-vessel card** now renders `coverMediaUrl` as the cover image (was an empty placeholder). The **list** already binds `coverMediaUrl` via `VesselImage` — lit up by the module fix.
- **Mock parity:** RegExp handlers for `uploads/session|complete` and numeric-id media GET/POST/DELETE/cover (in-memory per-vessel store; reflects cover onto the list); mock-ON skips the raw PUT.

---

## F) Files (scoped)

**BE — mobile BFF** (`Bff/src/Marine.Participant.Mobile/**`): `Common/RemoteClients/IVesselRemoteCall.cs` (+4 media calls); `Contracts/Upload/MobileUploadDtos.cs`, `Contracts/Vessel/MobileVesselMediaDtos.cs`; `Upload/Command/{CreateMobileUploadSession,CompleteMobileUpload}/**`; `Vessel/MobileVesselMediaMapper.cs`; `Vessel/Query/GetMobileVesselMedia/**`; `Vessel/Command/{AttachMobileVesselMedia,DeleteMobileVesselMedia,SetCoverMobileVesselMedia}/**`; `Controllers/V1/{UploadsController,VesselMediaController}.cs`.
**BE — module:** `Query/Vessel/GetUserVessels/GetUserVesselsQueryHandler.cs` (cover populate) + `Command/Media/{AddVesselMedia,RemoveVesselMedia,SetCoverVesselMedia}CommandHandler.cs` (user-list invalidation — cover-freshness companion).
**FE:** `src/core/api/{directUpload.ts,endpoints.ts,queryKeys.ts}`; `src/features/vessels/api/vesselMediaApi.ts`; `src/features/vessels/components/VesselGallery.tsx`; `src/features/vessels/screens/VesselDetailScreen.tsx`; `src/features/home/screens/HomeScreen.tsx`; `src/core/mock/handlers/vessels.handlers.ts`; `src/core/i18n/locales/{en,tr}.json`.

No other feature. Pre-existing unrelated working-tree changes (ServiceRequest dispute WIP, AdminPanel envelope-handler WIP) are **not** from this slice. **Closes the M4 vessel milestone.** NOT committed.
