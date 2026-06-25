# Docker Compose MinIO Reference

Expected local MinIO service example:

```yaml
minio:
  image: minio/minio:latest
  container_name: inktavia-minio
  command: server /data --console-address ":9001"
  ports:
    - "9000:9000"
    - "9001:9001"
  environment:
    MINIO_ROOT_USER: ${MINIO_ROOT_USER:-minioadmin}
    MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD:-minioadmin}
  volumes:
    - minio_data:/data
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
    interval: 10s
    timeout: 5s
    retries: 5

minio-init:
  image: minio/mc:latest
  container_name: inktavia-minio-init
  depends_on:
    minio:
      condition: service_healthy
  entrypoint: >
    /bin/sh -c "
    mc alias set local http://minio:9000 ${MINIO_ROOT_USER:-minioadmin} ${MINIO_ROOT_PASSWORD:-minioadmin};
    mc mb -p local/${MINIO_BUCKET_NAME:-inktavia-filestorage-local};
    mc anonymous set none local/${MINIO_BUCKET_NAME:-inktavia-filestorage-local};
    exit 0;
    "
```

Expected volume:

```yaml
volumes:
  minio_data:
```
