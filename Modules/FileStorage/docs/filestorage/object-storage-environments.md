# S3ObjectStorage Configuration Contract

This document defines the `S3ObjectStorage` configuration section used by the FileStorage module.

---

## All Fields

| Field | Type | Default | Required | Description |
|---|---|---|---|---|
| `Provider` | `string` | `"AWS"` | Yes | `"AWS"` for Amazon S3, `"MinIO"` for MinIO/S3-compatible |
| `AccessKey` | `string` | — | Yes | Access key ID |
| `SecretKey` | `string` | — | Yes | Secret access key |
| `Region` | `string` | — | Yes for AWS | AWS region (e.g. `eu-central-1`). Ignored by MinIO. |
| `BucketName` | `string` | — | Yes | Target bucket name |
| `ServiceUrl` | `string?` | `null` | MinIO only | Full URL to S3-compatible endpoint (e.g. `http://minio:9000`) |
| `ForcePathStyle` | `bool` | `false` | MinIO only | Must be `true` for MinIO (path-style addressing) |
| `UseHttp` | `bool` | `false` | MinIO only | Use HTTP instead of HTTPS. Only `true` for local dev MinIO |
| `UploadUrlExpirationMinutes` | `int` | `15` | No | Pre-signed upload URL TTL in minutes |
| `ReadUrlExpirationMinutes` | `int` | `60` | No | Pre-signed read URL TTL in minutes |

---

## Environment Variable Names

ASP.NET Core configuration provider maps `__` to `:`, so:

| Config key | Environment variable |
|---|---|
| `S3ObjectStorage:Provider` | `S3ObjectStorage__Provider` |
| `S3ObjectStorage:AccessKey` | `S3ObjectStorage__AccessKey` |
| `S3ObjectStorage:SecretKey` | `S3ObjectStorage__SecretKey` |
| `S3ObjectStorage:Region` | `S3ObjectStorage__Region` |
| `S3ObjectStorage:BucketName` | `S3ObjectStorage__BucketName` |
| `S3ObjectStorage:ServiceUrl` | `S3ObjectStorage__ServiceUrl` |
| `S3ObjectStorage:ForcePathStyle` | `S3ObjectStorage__ForcePathStyle` |
| `S3ObjectStorage:UseHttp` | `S3ObjectStorage__UseHttp` |
| `S3ObjectStorage:UploadUrlExpirationMinutes` | `S3ObjectStorage__UploadUrlExpirationMinutes` |
| `S3ObjectStorage:ReadUrlExpirationMinutes` | `S3ObjectStorage__ReadUrlExpirationMinutes` |

---

## Example: Local MinIO

```json
"S3ObjectStorage": {
  "Provider": "MinIO",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "Region": "us-east-1",
  "BucketName": "inktavia-filestorage-local",
  "ServiceUrl": "http://localhost:9000",
  "ForcePathStyle": true,
  "UseHttp": true,
  "UploadUrlExpirationMinutes": 15,
  "ReadUrlExpirationMinutes": 60
}
```

## Example: Docker Compose (Development)

```json
"S3ObjectStorage": {
  "Provider": "MinIO",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "Region": "us-east-1",
  "BucketName": "inktavia-filestorage-local",
  "ServiceUrl": "http://minio:9000",
  "ForcePathStyle": true,
  "UseHttp": true,
  "UploadUrlExpirationMinutes": 15,
  "ReadUrlExpirationMinutes": 60
}
```

## Example: AWS S3 Dev

```json
"S3ObjectStorage": {
  "Provider": "AWS",
  "AccessKey": "__FROM_SECRET__",
  "SecretKey": "__FROM_SECRET__",
  "Region": "eu-central-1",
  "BucketName": "inktavia-filestorage-dev",
  "ServiceUrl": null,
  "ForcePathStyle": false,
  "UseHttp": false,
  "UploadUrlExpirationMinutes": 15,
  "ReadUrlExpirationMinutes": 60
}
```

## Example: AWS S3 Production

```json
"S3ObjectStorage": {
  "Provider": "AWS",
  "AccessKey": "__FROM_SECRET__",
  "SecretKey": "__FROM_SECRET__",
  "Region": "eu-central-1",
  "BucketName": "inktavia-filestorage-prod",
  "ServiceUrl": null,
  "ForcePathStyle": false,
  "UseHttp": false,
  "UploadUrlExpirationMinutes": 15,
  "ReadUrlExpirationMinutes": 60
}
```

---

## Rules

1. **Do not commit real AWS credentials** to any appsettings file. Use `__FROM_SECRET__` as a placeholder; actual values must be injected via Kubernetes secrets or CI/CD secret stores.
2. `ServiceUrl` must be `null` (or omitted) for AWS S3. It is only set for MinIO.
3. `ForcePathStyle` must be `true` for MinIO and `false` for AWS S3.
4. `UseHttp` must be `false` in any production or AWS environment. Only set `true` for local MinIO.
5. `AccessKey` must never be `"minioadmin"` when `Provider` is `"AWS"` — the startup validator will reject this.
6. MinIO credentials (`minioadmin`) are acceptable in `Local` and `Development` environments only.
