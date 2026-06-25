# 03 - Object Storage Configuration Contract

Create or update the `S3ObjectStorage` configuration contract.

Required fields:

```text
Provider
AccessKey
SecretKey
Region
BucketName
ServiceUrl
ForcePathStyle
UseHttp
UploadUrlExpirationMinutes
ReadUrlExpirationMinutes
```

Local MinIO values:

```text
Provider=MinIO
AccessKey=minioadmin
SecretKey=minioadmin
Region=us-east-1
BucketName=inktavia-filestorage-local
ServiceUrl=http://localhost:9000
ForcePathStyle=true
UseHttp=true
UploadUrlExpirationMinutes=15
ReadUrlExpirationMinutes=60
```

Cloud AWS values:

```text
Provider=AWS
AccessKey=from secret
SecretKey=from secret
Region=eu-central-1 or selected AWS region
BucketName=inktavia-filestorage-dev/test/prod
ServiceUrl=null
ForcePathStyle=false
UseHttp=false
UploadUrlExpirationMinutes=15
ReadUrlExpirationMinutes=60
```

If `S3ObjectStorageOptions` already exists, update it. Do not duplicate options classes.

Add validation logic if the project has options validation conventions.

Generate:

```text
docs/filestorage/object-storage-environments.md
```
