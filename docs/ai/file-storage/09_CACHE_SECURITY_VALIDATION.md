# 09 — Cache and Security Validation

Implement and validate cache/security behavior.

## Cache

Use existing Aizen cache infrastructure.

Cache candidates:

```text
GetFileMetadataQuery
GetFileByIdQuery
GetFileAccessUrlQuery only for short TTL and user-safe context
GetFileProcessingJobsQuery with short TTL if safe
```

Do not cache:

```text
CreateUploadSessionCommand
CompleteUploadSessionCommand
LinkFileToOwnerCommand
DeleteFileCommand
ValidateFileOwnershipQuery unless cache key is owner-specific
Access-sensitive read URL without user context
```

## Redis usage

Use Redis/Aizen distributed cache for:

```text
Upload session temporary state
Signed URL short TTL cache
Rate limit
Processing status cache
```

## Security checks

Implement/validate:

```text
Allowed content types
Allowed extensions
Max file size
Owner module validation
Private/public visibility
Signed URL expiration
File status validation
Checksum validation where supplied
Soft delete protection
Access policy validation
```

## Recommended allowed content types

For MVP:

```text
image/jpeg
image/png
image/webp
application/pdf
text/plain
```

Do not allow executable files.

## Signed URL rules

```text
Upload URL TTL: 5-15 minutes
Read URL TTL: 5-30 minutes
Never store signed URL permanently
Always generate per request or from short-lived cache
```

Document every cacheable handler and every security service.
