# Claude Code Prompt — Phase 2g: presign with a browser-reachable endpoint (blocks all uploads)

Small, self-contained, backend/infra only. **Document upload is currently broken end to end** and this is the only
thing standing in the way — the frontend signed-URL flow is implemented and verified up to this point.

## The bug

Driving the real browser flow, `POST /provider/files/upload-session` succeeds, the SPA then `PUT`s the file straight
to storage, and the PUT fails ("Storage upload failed").

Cause: FileStorage signs the presigned URL with the **internal Docker hostname**.

```yaml
S3ObjectStorage__ServiceUrl: http://minio:9000    # docker-compose, file-storage-api
```

`S3ObjectStorageProvider` builds **one** `AmazonS3Client` from that `ServiceUrl` and uses it for *everything* —
both `GetPreSignedURL` (upload/read URLs handed to the browser) and the server-side calls
(`GetObjectMetadataAsync`, `DeleteObjectAsync`). So the browser is handed
`http://minio:9000/...`, and `minio` is a name only other containers can resolve.

**You cannot simply rewrite the host after signing.** With SigV4 the `Host` header is part of the signature, so
swapping `minio:9000` → `localhost:9000` on an already-signed URL invalidates it. The URL must be **signed against
the endpoint the browser will actually call**.

MinIO already publishes `9000:9000`, so `http://localhost:9000` is reachable from the browser today.

## The fix — two endpoints, one client each

1. `S3ObjectStorageOptions`: add

```csharp
/// <summary>
/// Browser-reachable endpoint used ONLY when signing presigned URLs (upload/read). The browser cannot resolve
/// the internal container hostname, and SigV4 signs the Host header, so the URL must be signed against the host
/// the client will actually call. Falls back to ServiceUrl when null (e.g. real AWS S3, where both are the same).
/// </summary>
public string? PublicServiceUrl { get; set; }
```

2. `S3ObjectStorageProvider`: build a **second** client, `_presignClient`, from `PublicServiceUrl ?? ServiceUrl`
   (same credentials/region/ForcePathStyle). Use it in `GenerateUploadUrlAsync` and `GenerateReadUrlAsync`.
   Keep the existing `_client` (internal `ServiceUrl`) for `GetObjectMetadataAsync`, `DeleteObjectAsync` and every
   other server-side call — those run inside the container network and must **not** go through `localhost`.

3. `docker-compose.yaml`, `file-storage-api`:

```yaml
S3ObjectStorage__ServiceUrl:       http://minio:9000        # internal, server-side ops
S3ObjectStorage__PublicServiceUrl: ${MINIO_PUBLIC_URL:-http://localhost:9000}   # what the browser gets
```

Add `MINIO_PUBLIC_URL` to `.env.example`. In production this becomes the real public S3/CDN endpoint; on AWS S3
(no custom endpoint) leave both null and nothing changes.

4. **CORS on MinIO.** The browser `PUT`s cross-origin from `http://localhost:3002` to `http://localhost:9000`.
   Verify MinIO answers the preflight and allows `PUT` + the `Content-Type` header from the provider-web origin; if
   it does not, configure it (`mc admin config set` / bucket CORS) in the existing `minio-init` container. Prove it
   with a real browser PUT, not curl — curl does not send an `Origin` header and will pass even when the browser
   fails.

## While you are here

`ReadUrlExpirationMinutes` defaults to **60**. A one-hour capability handed to a browser is not "short-lived" — the
architecture calls for signed URLs that expire quickly and are minted per click. Drop the read-URL default to
**5 minutes** (keep upload at 15, which is reasonable for a large file over a slow link).

## Test — must be done in a real browser

1. Provider opens onboarding → Compliance step → picks a PDF.
2. `POST /provider/files/upload-session` returns an `uploadUrl` whose host is **`localhost:9000`** (not `minio:9000`).
3. The browser `PUT` to that URL returns **200** (check the Network tab, not curl).
4. `POST /files/{fileId}/complete` flips the file to `Uploaded`, and the attach succeeds.
5. The document appears in `GET /onboarding` and survives a page reload.
6. Clicking the document mints a read URL, also on `localhost:9000`, and it opens.
7. Server-side ops still work: deleting the document reaches MinIO over the **internal** endpoint (nothing in the
   container tries to call `localhost:9000`).
