# 14d — Seed a REAL attachment image so the read-URL opens end-to-end

14c proved the access-control chain: the gallery mints a signed read URL only for an attachment on a request the
provider may see. But the seeded attachment (`fileId a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4` on SR 9011) points at
**no real MinIO object**, so clicking it shows "couldn't open". This seeds a real object + a matching FileStorage
`File` row with that exact `PublicId`, so the presigned URL resolves to an actual image and opens in the lightbox.

Small, FileStorage-only change. No BFF, no ServiceRequest, no frontend change.

## Verified in source (2026-07-15)

- BFF `CreateReadUrl(fileId)` → module `CreateReadUrlCommandHandler`: `_fileRepository.GetByGuidAsync(fileId)`
  which matches **`FileEntity.PublicId == fileId`** (not Id, not FileCode). Then `FileAccessService.CreateReadUrlAsync`
  requires `Status ∈ { Uploaded, Ready }` and presigns `BucketName`+`ObjectKey` via the **presign client**
  (`PublicServiceUrl`, browser-reachable — local: `http://localhost:9000`).
- `FileEntity.Create(...)` sets `PublicId = Guid.NewGuid()` internally (no setter) and `Status = Created`. So the
  seed must (a) **override PublicId** to the fixed Guid, and (b) move status to `Uploaded`/`Ready`.
- Real upload writes bytes to the bucket via a **presigned PUT** (browser). A seeder runs **inside the pod**, so it
  must write bytes to the **internal** endpoint (`ServiceUrl`), not the public presign endpoint. The provider's
  internal `_client` already targets `ServiceUrl`; it just has no "put bytes" method yet → add one.
- Bucket `inktavia-filestorage-local` is created by docker-compose (`mc mb`), so it already exists locally.
- The ServiceRequest attachment seed (`SeedAttachmentsAsync`, id `50001`) already uses this exact Guid on SR 9011.
  **The File's PublicId MUST equal that literal** — that shared literal is the only coupling between the two seeds.

## Work

### 1. Add a "put bytes" method to the storage provider (internal client)
`IObjectStorageProvider` (Domain) + `S3ObjectStorageProvider` (Repository):

```csharp
Task PutObjectAsync(string bucketName, string objectKey, byte[] content, string contentType,
    CancellationToken cancellationToken = default);
```

Implement with the **internal** `_client` (the one built on `ServiceUrl`, NOT `_presignClient`):

```csharp
public async Task PutObjectAsync(string bucketName, string objectKey, byte[] content, string contentType,
    CancellationToken cancellationToken = default)
{
    using var stream = new MemoryStream(content);
    await _client.PutObjectAsync(new PutObjectRequest
    {
        BucketName = bucketName,
        Key = objectKey,
        InputStream = stream,
        ContentType = contentType,
        DisablePayloadSigning = true // MinIO + path-style: avoid chunked-signature rejection
    }, cancellationToken);
}
```
(If `DisablePayloadSigning` isn't on this SDK version, omit it; add `DisableDefaultChecksumValidation = true` only
if MinIO rejects the checksum header. Verify the PUT succeeds against local MinIO.)

### 2. Seed the object + File row (idempotent) in `SeedFileStorageAsync`
Extend `DependencyInjection.SeedFileStorageAsync` (after `MigrateAsync`) — or a small `FileStorageMockDataSeeder`
it calls. Resolve `FileStorageDbContext`, `IObjectStorageProvider`, `IOptions<S3ObjectStorageOptions>`.

```csharp
// MUST equal ServiceRequest SeedAttachmentsAsync's fileId (id 50001) on SR 9011.
var publicId = new Guid("a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4");

if (await db.Files.AnyAsync(f => f.PublicId == publicId, ct)) return; // idempotent

var bucket = options.Value.BucketName;
const string objectKey = "image/seed/service-requests/9011/dumen-hasar.png";
var bytes = Convert.FromBase64String(SeedPhotoPngBase64); // constant below

await storageProvider.PutObjectAsync(bucket, objectKey, bytes, "image/png", ct);

var file = FileEntity.Create(
    fileCode: "SEEDSR9011PHOTO",
    originalFileName: "dumen-hasar.png",
    storedFileName: "dumen-hasar.png",
    bucketName: bucket,
    objectKey: objectKey,
    contentType: "image/png",
    extension: "png",
    sizeInBytes: bytes.Length,
    storageProvider: StorageProviderType.Minio,   // match the configured provider
    visibility: FileVisibility.Internal,
    category: FileCategory.Image,
    uploadedByUserId: null);

file.MarkUploaded();  // sets UploadedAt + Status=Uploaded
file.MarkReady();     // Status=Ready (readable; scan clean)

db.Files.Add(file);
// Override the auto-generated PublicId to the fixed Guid (private setter → set through EF):
db.Entry(file).Property(nameof(FileEntity.PublicId)).CurrentValue = publicId;
await db.SaveChangesAsync(ct);
```

Add the image as a `private const string SeedPhotoPngBase64` (a real 480×320 PNG placeholder — a framed
"SEED PHOTO / SR 9011 dumen hasar" card, enough to confirm the pipeline; swap a nicer asset later):

```csharp
private const string SeedPhotoPngBase64 =
    "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";
```

## Constraints
- `PublicId` MUST be the exact literal above (couples to the existing attachment seed). Idempotent on `PublicId`.
- Write bytes via the **internal** client (`ServiceUrl`), presign stays on the public client — do not change presign.
- Status must end `Ready` (or `Uploaded`); anything else is unreadable (fail-closed by design).
- FileStorage-only. No BFF/ServiceRequest/frontend change. No new public endpoint.

## Acceptance — observed
- App boots; seeder runs once (second boot: no duplicate — guard hit).
- MinIO console/`mc ls local/inktavia-filestorage-local/image/seed/service-requests/9011/` shows `dumen-hasar.png`.
- `db.Files` has one row: `PublicId = a0a0a0a0-…`, `Status = Ready`, `BucketName/ObjectKey` set.
- Provider hits `GET …/service-requests/9011/attachments/a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4/read-url` → returns a
  `url` that, opened in a browser, **renders the image** (200, image/png). Paste the response (URL host may be
  redacted, but confirm it opens).
- Wrong fileId / no-relationship still rejected (14c unchanged).

## Report
Append to `REPORT_BACKEND.md` (section "14d"): the `mc ls` line, the File row (PublicId/Status/ObjectKey), and a
confirmation the read-url renders the image. Unfinished is **not done**.
