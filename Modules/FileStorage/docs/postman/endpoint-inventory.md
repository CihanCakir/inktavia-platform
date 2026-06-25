# FileStorage Module — Endpoint Inventory

**Module:** FileStorage  
**Port:** 7106  
**Base URL:** `{{file_storage_api_base_url}}` = `http://localhost:7106/api/v1`  
**Auth:** `Authorization: Bearer {{active_access_token}}`  

---

## UploadSessionController

**Route prefix:** `/api/v1/upload-sessions`  
**Tag:** UploadSession  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/upload-sessions | CreateUploadSessionRequest | Returns uploadSessionCode + presignedUrl |
| 2 | POST | /api/v1/upload-sessions/{uploadSessionCode}/complete | CompleteUploadSessionRequest | Returns FileId |

### Upload Flow

The upload process requires 3 steps:
1. **Create upload session** → get `uploadSessionCode` and `presignedUrl`
2. **Upload file to S3** via PUT to `presignedUrl` (done directly from client to S3)
3. **Complete upload session** → confirm upload and get `fileId`

**Sample CreateUploadSessionRequest:**
```json
{
  "fileName": "vessel-photo.jpg",
  "contentType": "image/jpeg",
  "fileSizeBytes": 2457600,
  "bucketContext": "vessel-media",
  "ownerEntityType": "Vessel",
  "ownerEntityId": "{{vesselId}}"
}
```

**Sample Response (Create Upload Session):**
```json
{
  "uploadSessionCode": "upl-abc123def456",
  "presignedUrl": "https://s3.amazonaws.com/inktavia-media/vessel-media/vessel-photo.jpg?X-Amz-Signature=...",
  "expiresAt": "2025-01-15T12:30:00Z"
}
```

**Sample CompleteUploadSessionRequest:**
```json
{
  "checksum": "d41d8cd98f00b204e9800998ecf8427e",
  "actualSizeBytes": 2457600
}
```

**Sample Response (Complete Upload Session):**
```json
{
  "fileId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "vessel-photo.jpg",
  "contentType": "image/jpeg",
  "fileSizeBytes": 2457600,
  "status": "Active"
}
```

**Bucket Context values:** `vessel-media`, `vessel-documents`, `service-request-attachments`, `profile-photos`, `general`

---

## FileController

**Route prefix:** `/api/v1/files`  
**Tag:** File  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/files/{fileId} | — | FileDto |
| 2 | GET | /api/v1/files/{fileId}/metadata | — | FileMetadataDto |
| 3 | DELETE | /api/v1/files/{fileId} | DeleteFileRequest | 200 OK |
| 4 | PUT | /api/v1/files/{fileId}/visibility | FileVisibility (enum) | 200 OK |
| 5 | POST | /api/v1/files/{fileId}/owners | LinkFileToOwnerRequest | 200 OK |

**Sample FileDto Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "vessel-photo.jpg",
  "contentType": "image/jpeg",
  "fileSizeBytes": 2457600,
  "bucketContext": "vessel-media",
  "visibility": "Private",
  "status": "Active",
  "checksum": "d41d8cd98f00b204e9800998ecf8427e",
  "createdAt": "2025-01-15T10:00:00Z"
}
```

**Sample DeleteFileRequest:**
```json
{
  "reason": "Duplicate file uploaded"
}
```

**FileVisibility enum values:** `Public`, `Private`

**Sample LinkFileToOwnerRequest:**
```json
{
  "ownerEntityType": "Vessel",
  "ownerEntityId": "{{vesselId}}"
}
```

---

## FileAccessController

**Route prefix:** `/api/v1/files/{fileId}/access`  
**Tag:** FileAccess  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/files/{fileId}/access/read-url | CreateReadUrlRequest | Returns presigned read URL |
| 2 | POST | /api/v1/files/{fileId}/access/validate-ownership | ValidateFileOwnershipRequest | Returns ownership validation result |

**Sample CreateReadUrlRequest:**
```json
{
  "expiresInMinutes": 15
}
```

**Sample Response (Create Read URL):**
```json
{
  "readUrl": "https://s3.amazonaws.com/inktavia-media/vessel-media/vessel-photo.jpg?X-Amz-Signature=...",
  "expiresAt": "2025-01-15T12:15:00Z"
}
```

**Sample ValidateFileOwnershipRequest:**
```json
{
  "ownerEntityType": "Vessel",
  "ownerEntityId": "{{vesselId}}"
}
```

---

## FileProcessingController

**Route prefix:** `/api/v1/files/{fileId}/processing`  
**Tag:** FileProcessing  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/files/{fileId}/processing/start | StartFileProcessingRequest | Returns jobId |
| 2 | PUT | /api/v1/files/{fileId}/processing/result | UpdateFileProcessingResultRequest | 200 OK |
| 3 | GET | /api/v1/files/{fileId}/processing/jobs | — | List<FileProcessingJobDto> |

**Sample StartFileProcessingRequest:**
```json
{
  "processingType": "ThumbnailGeneration",
  "priority": 5
}
```

**ProcessingType values:** `ThumbnailGeneration`, `VirusScan`, `MetadataExtraction`, `ImageOptimization`

**Sample UpdateFileProcessingResultRequest:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Completed",
  "resultUrl": "https://s3.amazonaws.com/inktavia-media/thumbnails/vessel-photo-thumb.jpg",
  "errorMessage": null
}
```
