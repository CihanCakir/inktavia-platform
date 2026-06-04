# FileStorage Object Storage Environments Guide

## Goal

Configure FileStorage so that:

- Local tests use MinIO through Docker Compose.
- Dev/Test/Prod use AWS S3.
- The application uses one consistent `S3ObjectStorage` configuration contract.
- The object storage provider can switch between MinIO and AWS S3 using configuration only.
- Signed upload and signed read URLs work in both local and cloud environments.

## Required output

The agent must generate or update:

```text
docker-compose.yml
docker-compose.override.yml or docker-compose.local.yml
.env.example
docs/filestorage/object-storage-environments.md
docs/filestorage/minio-local-setup.md
docs/filestorage/aws-s3-dev-test-prod-setup.md
Modules/FileStorage/docs/postman/FileStorage.ObjectStorage.postman_collection.json
Modules/FileStorage/docs/postman/FileStorage.ObjectStorage.Local.postman_environment.json
Modules/FileStorage/docs/postman/FileStorage.object-storage-validation-report.md
```

The exact project paths may vary. Inspect the repository before editing.
