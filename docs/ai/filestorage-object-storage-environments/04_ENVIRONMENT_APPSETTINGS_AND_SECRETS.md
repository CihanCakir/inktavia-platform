# 04 - Environment AppSettings and Secrets

Create or update appsettings/environment examples for FileStorage object storage.

Expected files, depending on existing conventions:

```text
appsettings.Local.json
appsettings.Development.json
appsettings.Test.json
appsettings.Production.json
.env.example
```

If the repository keeps appsettings under module-specific API projects, update the FileStorage API project appsettings.

Do not commit real AWS credentials.

Document environment variable overrides:

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

If Kubernetes manifests are already present, add an example Secret and ConfigMap for dev/test/prod. If Kubernetes manifests are not present, create documentation only.

Generate:

```text
docs/filestorage/aws-s3-dev-test-prod-setup.md
```
