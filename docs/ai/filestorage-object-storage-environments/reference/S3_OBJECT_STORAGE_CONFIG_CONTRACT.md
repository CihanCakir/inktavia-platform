# S3ObjectStorage Configuration Contract

Use a single configuration shape across local, dev, test, and production.

## Local MinIO example

```json
{
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
}
```

## AWS S3 dev example

```json
{
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
}
```

## Environment variable names

Use .NET nested configuration naming:

```text
S3ObjectStorage__Provider
S3ObjectStorage__AccessKey
S3ObjectStorage__SecretKey
S3ObjectStorage__Region
S3ObjectStorage__BucketName
S3ObjectStorage__ServiceUrl
S3ObjectStorage__ForcePathStyle
S3ObjectStorage__UseHttp
S3ObjectStorage__UploadUrlExpirationMinutes
S3ObjectStorage__ReadUrlExpirationMinutes
```

## Rules

- Do not commit real AWS secrets.
- Local MinIO credentials may appear only in local examples.
- `ServiceUrl` is required for MinIO and must be null/empty for AWS S3 unless using a custom S3-compatible provider.
- `ForcePathStyle` must be true for MinIO and false for AWS S3.
- `Region` can be `us-east-1` for local MinIO and should match the real AWS bucket region in cloud.
