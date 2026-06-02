# 08 — API Controllers, Starter and DI

Create API layer.

## Program.cs

Create/update `Aizen.Modules.FileStorage.Api/Program.cs`.

Follow Identity/ReferenceData/Vessel starter and operation pattern.

Register:

```text
Application
Repository
Controllers
Consumers
Swagger/OpenAPI
Authentication/Authorization
Validation
MessageBus consumers
AWS S3 options
Cache
Mongo if used
```

## Controllers

Create under `Aizen.Modules.FileStorage.Api/Controllers/V1`:

```text
UploadSessionController
FileController
FileAccessController
FileProcessingController
```

## Endpoints

### UploadSessionController

```text
POST /api/v1/file-storage/upload-sessions
POST /api/v1/file-storage/upload-sessions/{uploadSessionId}/complete
GET  /api/v1/file-storage/upload-sessions/{uploadSessionId}
```

### FileController

```text
GET    /api/v1/file-storage/files/{fileId}
POST   /api/v1/file-storage/files/filter
GET    /api/v1/file-storage/files/by-owner
PATCH  /api/v1/file-storage/files/{fileId}/visibility
DELETE /api/v1/file-storage/files/{fileId}
```

### FileAccessController

```text
POST /api/v1/file-storage/files/{fileId}/read-url
POST /api/v1/file-storage/files/{fileId}/validate-ownership
POST /api/v1/file-storage/files/{fileId}/link-owner
```

### FileProcessingController

```text
POST /api/v1/file-storage/files/{fileId}/processing/start
GET  /api/v1/file-storage/files/{fileId}/processing-jobs
POST /api/v1/file-storage/files/{fileId}/processing/result
```

## Controller rules

- Controllers must not contain business logic.
- Controllers dispatch commands/queries.
- Controllers use request models from Abstraction/Request.
- Controllers return DTOs via existing API response wrapper.
- Controllers must include DocumentationInfo.

## Consumer registration

Register consumers according to existing Aizen MessageBus configuration.
If consumer registration is convention-based, ensure namespaces/folders match the convention.
If explicit registration is required, add all FileStorage consumers.
