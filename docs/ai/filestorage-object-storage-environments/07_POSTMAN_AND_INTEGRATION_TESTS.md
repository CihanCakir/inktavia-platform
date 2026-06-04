# 07 - Postman and Integration Tests

Create FileStorage object storage Postman docs and tests.

Output path:

```text
Modules/FileStorage/docs/postman/
```

Generate/update:

```text
FileStorage.ObjectStorage.postman_collection.json
FileStorage.ObjectStorage.Local.postman_environment.json
FileStorage.object-storage-testing-guide.md
FileStorage.object-storage-validation-report.md
```

Use the existing Inktavia Postman auth standard:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

Requests should test all available FileStorage controller endpoints related to:

- Create upload session.
- Generate signed upload URL.
- Complete upload if supported.
- Get file metadata.
- Generate signed read URL.
- Soft delete/delete file if supported.

Each request must include:

- Correct request DTO-based body.
- Route and query parameters as variables.
- Post-response tests.
- ID extraction scripts where applicable.

Local environment must include:

```text
file_storage_api_base_url=http://localhost:7106/api/v1
fileId=
uploadSessionId=
bucketName=inktavia-filestorage-local
```
