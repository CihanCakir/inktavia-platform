# 05 - Service Registration and Provider Selection

Update FileStorage service registration so the same module supports both MinIO and AWS S3.

Rules:

- Read `S3ObjectStorage` options through .NET configuration/options pattern.
- Use `AmazonS3Client` or existing S3-compatible implementation.
- For MinIO:
  - `ServiceURL = options.ServiceUrl`
  - `ForcePathStyle = true`
  - `UseHttp = true`
- For AWS:
  - Use `RegionEndpoint.GetBySystemName(options.Region)`.
  - Do not require `ServiceUrl`.
  - `ForcePathStyle = false`.
  - HTTPS only.

Do not create a separate MinIO-only implementation unless current architecture requires it. Prefer one S3-compatible provider configured by options.

Ensure signed upload/read URLs use:

```text
UploadUrlExpirationMinutes
ReadUrlExpirationMinutes
```

Add safe startup validation:

- Missing bucket name fails fast.
- Missing credentials fail fast outside local only.
- Production must not use `minioadmin`.
- Production must not use `UseHttp=true`.
