# 06 — Application Commands and Queries

Create Application layer.

## General rules

- Follow Aizen CQRS pattern from Identity/ReferenceData/Vessel.
- Use typed DTO/response models only.
- Do not return object.
- Use `IAizenInfoAccessor` to set UserInfo, Client and Device values when required.
- Use `IAizenMessagePublisher` to publish async messages.
- Every command/query/handler/validator/mapping must include DocumentationInfo.

## Commands

```text
CreateUploadSessionCommand -> FileUploadSessionDto
CompleteUploadSessionCommand -> FileDto
LinkFileToOwnerCommand -> FileOwnerReferenceDto
DeleteFileCommand -> bool
CreateReadUrlCommand -> FileAccessUrlDto
StartFileProcessingCommand -> FileProcessingJobDto
UpdateFileProcessingResultCommand -> bool
UpdateFileVisibilityCommand -> FileDto
RejectFileCommand -> bool
```

## Queries

```text
GetFileMetadataQuery -> FileMetadataDto
GetFileByIdQuery -> FileDto
GetFileAccessUrlQuery -> FileAccessUrlDto
ValidateFileOwnershipQuery -> FileValidationResultDto
GetFileProcessingJobsQuery -> paginated FileProcessingJobDto
FilterFilesQuery -> paginated FileDto
GetFilesByOwnerQuery -> paginated FileDto
```

All collection queries must use pagination.

## Command rules

### CreateUploadSessionCommand
1. Use IAizenInfoAccessor to get user/client/device.
2. Validate file name, content type, extension and size.
3. Generate file id/file code/object key.
4. Create FileEntity.
5. Create FileUploadSessionEntity.
6. Generate S3 pre-signed upload URL.
7. Return FileUploadSessionDto.
8. Do not upload file through API.

### CompleteUploadSessionCommand
1. Find session.
2. Validate not expired.
3. Validate S3 object exists.
4. Validate size/content type/checksum.
5. Mark file Uploaded/Ready.
6. Publish `FileUploadedMessage`.
7. Optionally publish `FileProcessingRequestedMessage`.
8. Return FileDto.

### LinkFileToOwnerCommand
1. Validate file exists.
2. Validate file status is Uploaded/Ready.
3. Validate owner module/entity fields.
4. Create FileOwnerReferenceEntity.
5. Publish `FileLinkedToOwnerMessage`.
6. Return FileOwnerReferenceDto.

### CreateReadUrlCommand
1. Validate file exists.
2. Validate access policy.
3. Generate pre-signed read URL.
4. Cache signed URL if allowed.
5. Return FileAccessUrlDto.

### DeleteFileCommand
1. Validate file exists.
2. Soft delete metadata.
3. Optionally delete S3 object based on delete behavior.
4. Publish `FileDeletedMessage`.
5. Return bool.

## Query rules

- Query handlers must return DTOs.
- Do not return entities or Mongo documents.
- Cache safe read queries using IAizenQueryHandlerCacheable where appropriate.
- Access-sensitive queries must include user context in cache key or must not be cached.
