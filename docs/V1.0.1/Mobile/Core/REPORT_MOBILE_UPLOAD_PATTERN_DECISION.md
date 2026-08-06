# DECISION — Canonical mobile file-upload pattern (before M4f)

**Type:** READ-ONLY investigation. No code / config / seed changed.
**Question:** M3c (avatar) and M4e (documents) ship a **server-side relay** (client → multipart → BFF → FileStorage). Before M4f (media/photos — larger + numerous), decide the canonical mobile upload pattern.

**TL;DR:** A public, device-reachable presigned PUT endpoint **already exists** (it's the default). The relay was an unnecessary M3c inheritance. **Adopt CLIENT-SIDE PRESIGNED direct-to-storage** as canonical; use it for M4f; retrofit M4e (medium) and M3c (low). The only real prerequisite is a device-reachable `PublicServiceUrl` for physical-device/prod (config, not code).

---

## 1. Findings

### 1a. Upload-session mechanics
- `CreateUploadSession` (`FileUploadSessionService`) → creates a `FileEntity` (status `UploadUrlGenerated`) + a `FileUploadSessionEntity`, computes the object key, and mints a **presigned PUT URL** via `IObjectStorageProvider.GenerateUploadUrlAsync(bucket, key, contentType, expiresIn, useInternalEndpoint: request.ServerSideUpload)`. Default PUT expiry **15 min**.
- `CompleteUploadSession` → `FileStorageService` reads the object back from storage (`GetObjectMetadataAsync(bucket, key)` — **throws if the client never PUT it**), then `file.MarkUploaded(checksum)` → status `Ready` and publishes `FileUploadedMessage`. **This is the integrity guarantee that makes a client-side upload safe** — the server trusts the file only after confirming the bytes landed.
- Reads: `CreateReadUrl` → `GenerateReadUrlAsync` always signs against the **public** endpoint.

### 1b. Internal vs public endpoint — the decisive mechanism
`S3ObjectStorageProvider` keeps **two** signing clients:
- `_client` → `ServiceUrl` = `http://minio:9000` (container-internal).
- `_presignClient` → `PublicServiceUrl` = `http://localhost:9000` (browser/device-reachable).

`GenerateUploadUrlAsync(..., useInternalEndpoint)` maps **directly** to the request's `ServerSideUpload` flag:
| `ServerSideUpload` | signs against | reachable by |
|---|---|---|
| `true` | `ServiceUrl` (`minio:9000`) | **server only** (the BFF must relay the bytes) |
| `false` **(default)** | `PublicServiceUrl` (`localhost:9000`) | **the device/browser** (client PUTs directly) |

`CreateUploadSessionRequest.ServerSideUpload` **defaults to `false`** → the out-of-the-box behaviour is **client-side presigned**. READ URLs already use the public endpoint (proven in M4e: the returned download URL was `http://localhost:9000/...` and the client fetched it byte-identically). So a device-reachable presigned **PUT** is the same host the client already reaches for GET.

### 1c. A public presigned PUT endpoint EXISTS today (no new infra)
- `docker-compose.yaml`: `S3ObjectStorage__PublicServiceUrl: ${MINIO_PUBLIC_URL:-http://localhost:9000}`, `S3ObjectStorage__ServiceUrl: http://minio:9000`. MinIO port `9000:9000` published. `MINIO_API_CORS_ALLOW_ORIGIN` set for web origins.
- SigV4 + path-style + explicit protocol are already handled (comments in the provider document the MinIO SigV2/TLS pitfalls — already solved).

### 1d. Provider/Admin reference pattern
- The AdminPanel BFF **`RequestOrganizerDocumentUploadUrlBff`** is explicitly designed as client-side presigned: *"Calls FileStorage to create an upload session and returns a pre-signed PUT URL for browser-direct upload"*, with a **separate Complete** endpoint — i.e. the intended architecture is client-issues-URL → browser-PUTs → BFF-completes.
- **Caveat:** that admin handler targets the **phantom `/api/v1/file-storage/*` prefix** (its `IFileStorageRemoteCall` BaseUrl is `http://file-storage-api:8080` **direct, no gateway**, and no such route exists — the same dead prefix M4e had to fix for vessel). It also has an empty `try{}catch` — it is **non-functional scaffolding**. So the design *intent* is unambiguous, but there is **no currently-working client-side reference in production code**.
- The **working** upload routes are the mobile BFF's own `IFileStorageRemoteCall`: `POST /api/v1/upload-sessions`, `POST /api/v1/upload-sessions/{code}/complete`, `POST /api/v1/files/{id}/access/read-url` — proven in M3c + M4e.

### 1e. Current mobile usage
Both mobile write paths explicitly set `ServerSideUpload = true` (relay): M3c avatar, M4e documents. (Payment invoice/receipt PDF services also use `true` — correct: server-generated, no client.)

### 1f. Sizes (per-category limits, `FileValidationService`)
| Category | Limit | Mobile relevance |
|---|---|---|
| Image | **10 MB** | vessel photos (M4f) — **multiple per vessel** |
| Document | **50 MB** | vessel docs (M4e) — a 50 MB PDF through BFF memory is real |
| Video | 500 MB | (future) — relay is a non-starter |
| Certificate | 10 MB | — |
Allowed types include images, pdf, office, mp4/mov/avi, zip. **Relaying N × up-to-10 MB photos (M4f) through BFF process memory is the worst case** the relay imposes.

---

## 2. Decision — CLIENT-SIDE PRESIGNED direct-to-storage (canonical)

A public presigned PUT already exists (`ServerSideUpload=false` → `PublicServiceUrl`), the Complete handshake verifies the object, read URLs are already device-reachable, and it matches the codebase's design intent. **The BFF issues a session + presigned PUT URL + session code; the RN client PUTs the bytes straight to storage; the BFF completes + attaches. Bytes never traverse the BFF.**

### Caveats to honor
1. **Content-Type binding (must-fix in impl):** the presigned PUT URL is signed with `GetPreSignedUrlRequest.ContentType = request.ContentType`. The RN client's `PUT` **must** send the identical `Content-Type` header, or MinIO/S3 rejects the signature. The BFF must sign with the exact type the client will send (echo it back as `requiredContentType`).
2. **Device-reachable public host (the one real prerequisite):** `localhost:9000` works for the **iOS simulator** (shares host localhost). A **physical device / production** needs `MINIO_PUBLIC_URL` set to a host the device can reach — a LAN IP for on-device dev, a public MinIO/S3/CDN URL in prod. This is **config, not code** — but it must be resolved before real-device upload testing. (The relay path does not need this, which is exactly why M3c reached for it.)
3. **CORS:** irrelevant for RN **native** networking (no browser Origin enforcement); only web clients need `MINIO_API_CORS_ALLOW_ORIGIN`.
4. **Auth:** the presigned URL is self-signed (S3 signature) — the PUT carries **no** Bearer/assertion (as M3c/M4e already do server-side). Owner-gating stays on the BFF's issue + complete calls.

### Fallback (only if a device-reachable public host is refused)
Keep the **server-side relay** as a documented, **size-bounded** fallback — cap at the **Image 10 MB** limit, one file at a time, and note the BFF holds the whole file in process memory (`MemoryStream` in the current handlers). If chosen, **file a ticket to set `MINIO_PUBLIC_URL`** so client-side can be adopted. Not recommended for M4f.

---

## 3. Retrofit plan

| Slice | Files | Recommendation | Effort |
|---|---|---|---|
| **M4f media/photos** | new | **Client-side presigned (mandatory)** — multiple × up-to-10 MB; relay would pin BFF memory | build fresh on the new pattern |
| **M4e documents** | `UploadMobileVesselDocumentCommandHandler` + FE | **Migrate (medium)** — 50 MB PDFs through BFF memory is a real risk | ~1 BFF endpoint split + FE PUT step |
| **M3c avatar** | `UploadParticipantAvatarCommandHandler` + FE | **Migrate opportunistically (low)** — single 10 MB image | small |

### Client-side presigned flow (target)
**BFF (per surface — media / document / avatar):**
1. `POST …/media/upload-url` (owner-gated) → `CreateUploadSession{ ServerSideUpload=false, Category, ContentType, SizeInBytes, Visibility }` → return `{ fileId, uploadUrl, uploadSessionCode, expiresAt, requiredContentType }`.
2. *(client PUTs bytes to `uploadUrl` — see below)*
3. `POST …/media/complete` (owner-gated) → `CompleteUploadSession(code)` → attach to the vessel (`AddVesselMedia` / `AddVesselDocument` / set avatar) → return the created entity (+ presigned read URL).

**RN client:**
1. pick file → call `/upload-url`.
2. `fetch(uploadUrl, { method:'PUT', headers:{ 'Content-Type': requiredContentType }, body: blob })` (RN can PUT a file URI as a blob; **exact** Content-Type).
3. call `/complete` with `uploadSessionCode`.
4. invalidate the list (fresh via the M4e consumer-side rule).

Reuses the existing, working `/api/v1/upload-sessions` + `/complete` remote calls — the only change is **flip `ServerSideUpload` to `false` and return the URL to the client** instead of PUTting server-side, plus a client PUT step.

---

## 4. Recommendation

Adopt **client-side presigned direct-to-storage** as the canonical mobile upload pattern and build **M4f on it**. It requires **no new infrastructure** — only setting `MINIO_PUBLIC_URL` to a device-reachable host for physical-device/prod (the sole prerequisite; simulator already works). Retrofit **M4e (medium)**; retrofit **M3c (low/opportunistic)**. Keep the relay only as a size-bounded ≤10 MB fallback if a public host is refused. Trade-off: client-side adds one round-trip (issue-URL, PUT, complete) and needs the exact-Content-Type discipline, but removes all upload bytes from BFF memory/latency and matches both the architecture and the read-URL path already in use.

**Approve the client-side-presigned pattern before M4f.**
