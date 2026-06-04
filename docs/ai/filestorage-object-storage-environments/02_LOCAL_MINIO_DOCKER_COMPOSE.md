# 02 - Local MinIO Docker Compose

Add local MinIO support for FileStorage local development and tests.

Preferred output depends on repo conventions:

```text
docker-compose.local.yml
```

or update existing:

```text
docker-compose.override.yml
docker-compose.yml
```

Required services:

- `minio`
- `minio-init`

MinIO requirements:

- Console port: `9001`
- API port: `9000`
- Healthcheck on `/minio/health/live`
- Persistent `minio_data` volume
- Bucket auto-create through `minio/mc`
- Default local bucket: `inktavia-filestorage-local`

Use environment variables:

```text
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=minioadmin
MINIO_BUCKET_NAME=inktavia-filestorage-local
```

Update `.env.example` only. Do not commit real secrets.

Generate:

```text
docs/filestorage/minio-local-setup.md
```
