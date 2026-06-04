# 08 - Build Validation and Final Report

Run validation:

```bash
dotnet restore
dotnet build
```

If Docker Compose was changed, validate syntax:

```bash
docker compose config
```

If a local compose file was added:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml config
```

Produce final report:

```text
docs/filestorage/filestorage-object-storage-final-report.md
```

Report must include:

- Files created.
- Files modified.
- Local MinIO status.
- Dev/Test/Prod AWS S3 configuration status.
- Secret handling status.
- Docker Compose validation status.
- Postman collection status.
- Build status.
- Known issues.
- Next recommended steps.

Do not claim success if commands were not executed. Clearly state skipped commands and why.
