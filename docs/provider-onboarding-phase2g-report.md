# Provider Onboarding Phase 2g — Presigned URL Fix

**Date:** 2026-07-12
**Status:** Complete. FileStorage builds with 0 errors. Upload URL now uses `localhost:9000`.

## The bug

FileStorage signed presigned URLs with the internal Docker hostname `minio:9000`. Browsers can't resolve this.
SigV4 signs the Host header, so rewriting `minio` → `localhost` on an already-signed URL invalidates it.

## The fix

### Two S3 clients — one for server ops, one for presigned URLs

`S3ObjectStorageOptions` gained `PublicServiceUrl` — the browser-reachable endpoint used only for presigning.

`S3ObjectStorageProvider` now has two `AmazonS3Client` instances:
- `_client` — internal (`ServiceUrl: http://minio:9000`) for server-side ops
- `_presignClient` — browser-reachable (`PublicServiceUrl: http://localhost:9000`) for `GenerateUploadUrlAsync`
  and `GenerateReadUrlAsync`

For AWS S3 (no custom endpoint), both are the same client. For MinIO, they differ.

### docker-compose

```yaml
S3ObjectStorage__ServiceUrl:       http://minio:9000            # internal
S3ObjectStorage__PublicServiceUrl:  http://localhost:9000         # browser
```

### MinIO CORS

Added `MINIO_API_CORS_ALLOW_ORIGIN: http://localhost:3002,http://localhost:3000` to the minio container
so cross-origin PUTs from the provider web app are allowed.

### ReadUrl TTL reduced

`ReadUrlExpirationMinutes` default changed from 60 to 5 minutes.

## Smoke

| Test | Result |
|------|--------|
| Upload session URL | `http://localhost:9000/...` (not `minio:9000`) ✓ |
| PUT to presigned URL | HTTP 200 ✓ |
| Server-side ops (internal) | Still use `minio:9000` (unchanged) ✓ |

## Build

- FileStorage: **0 errors** ✓
