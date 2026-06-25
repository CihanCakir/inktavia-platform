# FileStorage Module — Postman Testing Guide

## Overview
FileStorage module manages file upload sessions, file metadata, S3 presigned URLs, access control and processing jobs. Port: 7106.

## Prerequisites

### Environment Variables Required
| Variable | Value | Notes |
|---|---|---|
| file_storage_api_base_url | http://localhost:7106/api/v1 | |
| active_access_token | (set by auth) | Required for all endpoints |
| vesselId | (from Vessel module) | For owner linking |
| uploadSessionCode | (auto-set) | Set by Create Upload Session |
| fileId | (auto-set) | Set by Complete Upload Session |

### Services Required
- Keycloak (port 8080)
- FileStorage API (port 7106)
- AWS S3 or MinIO (for actual file storage)

## Upload Flow

FileStorage uses a 3-step upload flow:

### Step 1: Create Upload Session
Run `01 - Upload Session > Create Upload Session`
- Sets `uploadSessionCode` and `presignedUrl`

### Step 2: Upload File to S3
This step must be done outside Postman (or via pre-request script):
```javascript
// Pre-request script example (not fully automatable)
// PUT {{presignedUrl}} with binary file content
// Content-Type must match the contentType in CreateUploadSessionRequest
```
For testing: use curl or AWS CLI:
```bash
curl -X PUT "{{presignedUrl}}" \
  -H "Content-Type: image/jpeg" \
  --data-binary @./test-file.jpg
```

### Step 3: Complete Upload Session
Run `01 - Upload Session > Complete Upload Session`
- Sets `fileId` for subsequent requests
- Provide the MD5 checksum of the uploaded file

## Test Sequence

### Full Upload Flow
1. `00 - Auth Setup > Get Mobile Token`
2. `01 - Upload Session > Create Upload Session` → sets uploadSessionCode, presignedUrl
3. Upload file to presignedUrl externally
4. `01 - Upload Session > Complete Upload Session` → sets fileId

### File Access
5. `02 - File Management > Get File By ID`
6. `02 - File Management > Get File Metadata`
7. `03 - File Access > Create Read URL` → sets readUrl
8. Access file via readUrl in browser

### Link File to Vessel
9. `02 - File Management > Link File to Owner` (requires vesselId)

### Processing
10. `04 - File Processing > Start Processing`
11. `04 - File Processing > Get Processing Jobs`

## Expected Responses

### Create Upload Session
```json
HTTP 200 OK
{
  "uploadSessionCode": "upl-abc123def456",
  "presignedUrl": "https://s3.amazonaws.com/...",
  "expiresAt": "2025-01-15T12:30:00Z"
}
```

### Complete Upload Session
```json
HTTP 200 OK
{
  "fileId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "test-vessel-photo.jpg",
  "status": "Active"
}
```

### Get File By ID
```json
HTTP 200 OK
{
  "id": "3fa85f64-...",
  "fileName": "test-vessel-photo.jpg",
  "contentType": "image/jpeg",
  "fileSizeBytes": 2457600,
  "visibility": "Private",
  "status": "Active"
}
```

## Common Errors

| Error | Cause | Fix |
|---|---|---|
| 401 Unauthorized | Missing or expired token | Re-run Get Mobile Token |
| 400 Bad Request | Invalid content type | Check MIME type format |
| 404 Not Found | File/session doesn't exist | Check fileId/uploadSessionCode |
| 422 Checksum Mismatch | Checksum doesn't match uploaded file | Recalculate MD5 of uploaded file |
| 410 Session Expired | Upload session expired | Create a new upload session |

## Bucket Context Values
| Context | Use Case |
|---|---|
| vessel-media | Vessel photos and media |
| vessel-documents | Vessel certificates and documents |
| service-request-attachments | Service request supporting files |
| profile-photos | User/venue/organizer profile photos |
| general | General purpose uploads |
