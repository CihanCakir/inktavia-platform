# FileStorage Object Storage — Current State Analysis

_Generated as part of the object storage environment setup task._

---

## 1. `S3ObjectStorageOptions`

**Location:** `Modules/FileStorage/src/Aizen.Modules.FileStorage.Repository/Providers/S3/S3ObjectStorageOptions.cs`

**Fields before this change:**

| Field | Type | Default | Description |
|---|---|---|---|
| `AccessKey` | `string` | required | AWS/MinIO access key |
| `SecretKey` | `string` | required | AWS/MinIO secret key |
| `Region` | `string` | required | AWS region |
| `BucketName` | `string` | required | S3/MinIO bucket name |
| `UploadUrlExpirationMinutes` | `int` | `15` | Pre-signed upload URL TTL |
| `ReadUrlExpirationMinutes` | `int` | `60` | Pre-signed read URL TTL |

**Missing fields (added by this change):** `Provider`, `ServiceUrl`, `ForcePathStyle`, `UseHttp`

---

## 2. `S3ObjectStorageProvider`

**Location:** `Modules/FileStorage/src/Aizen.Modules.FileStorage.Repository/Providers/S3/S3ObjectStorageProvider.cs`

**Before this change:** Only supported AWS S3. The `AmazonS3Client` was always constructed with `Amazon.RegionEndpoint`, making MinIO incompatible.

**Capabilities:**
- `GenerateUploadUrlAsync` — produces a pre-signed PUT URL
- `GenerateReadUrlAsync` — produces a pre-signed GET URL
- `ObjectExistsAsync` — HEAD-based existence check
- `GetObjectMetadataAsync` — returns `ObjectMetadataResult` (ContentLength, ContentType, ETag, LastModified)
- `DeleteObjectAsync` — deletes an object from the bucket

---

## 3. `IObjectStorageProvider` Interface

**Location:** `Modules/FileStorage/src/Aizen.Modules.FileStorage.Domain/Interface/Service/IObjectStorageProvider.cs`

Defines the five methods listed above. Registered as `IObjectStorageProvider` singleton via `DependencyInjection.cs`.

---

## 4. `StorageProviderType` Enum

**Location:** `Modules/FileStorage/src/Aizen.Modules.FileStorage.Abstraction/Enum/StorageProviderType.cs`

```csharp
public enum StorageProviderType
{
    AwsS3 = 1,
    Minio = 2,
    Local = 3
}
```

---

## 5. Appsettings Locations and S3 Config (Before This Change)

| File | Path |
|---|---|
| `appsettings.Local.json` | `Modules/FileStorage/src/Aizen.Modules.FileStorage/configuration/appsettings.Local.json` |
| `appsettings.Development.json` | `Modules/FileStorage/src/Aizen.Modules.FileStorage/configuration/appsettings.Development.json` |
| `appsettings.Production.json` | `Modules/FileStorage/src/Aizen.Modules.FileStorage/configuration/appsettings.Production.json` |

**S3 section before this change (Local):**
```json
{
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "Region": "us-east-1",
  "BucketName": "inktavia-filestorage-local",
  "UploadUrlExpirationMinutes": 15,
  "ReadUrlExpirationMinutes": 60
}
```

**S3 section before this change (Development):**
Credentials were `minio`/`minio123` — inconsistent with local MinIO defaults.

---

## 6. Docker Compose (Before This Change)

- `file-storage-api` service was present on port `7106`
- **No MinIO service** and **no `minio_data` volume** existed
- `file-storage-api` did not depend on MinIO and had no S3 env var overrides

---

## 7. FileStorage Controllers and Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/v1/upload-sessions` | Create upload session → returns `signedUploadUrl` |
| `POST` | `/api/v1/upload-sessions/{code}/complete` | Complete upload → returns `FileDto` |
| `GET` | `/api/v1/files/{fileId}` | Get file by ID |
| `GET` | `/api/v1/files/{fileId}/metadata` | Get file metadata |
| `DELETE` | `/api/v1/files/{fileId}` | Soft delete file |
| `PUT` | `/api/v1/files/{fileId}/visibility` | Update visibility |
| `POST` | `/api/v1/files/{fileId}/owners` | Link file to owner |
| `POST` | `/api/v1/files/{fileId}/access/read-url` | Create pre-signed read URL |
| `POST` | `/api/v1/files/{fileId}/access/validate-ownership` | Validate ownership |

---

## 8. What Was Missing

- `Provider`, `ServiceUrl`, `ForcePathStyle`, `UseHttp` fields in `S3ObjectStorageOptions`
- MinIO branching in `S3ObjectStorageProvider` constructor
- `IValidateOptions<S3ObjectStorageOptions>` startup validator
- MinIO and `minio-init` services in `docker-compose.yaml`
- `minio_data` volume in `docker-compose.yaml`
- `file-storage-api` S3 environment variable overrides in docker-compose
- `.env.example` at repo root
- S3 env vars in Helm values files (`values-dev.yaml`, `values-test.yaml`, `values-prod.yaml`)
