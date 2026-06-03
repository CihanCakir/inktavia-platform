# FileStorage Module — Postman Validation Report

## Endpoint Coverage

| Controller | Endpoint | Method | Route | Covered | Sample | Tests | Notes |
|---|---|---|---|---|---|---|---|
| UploadSessionController | Create Upload Session | POST | /upload-sessions | ✅ Yes | ✅ Yes | ✅ Yes | Extracts uploadSessionCode + presignedUrl |
| UploadSessionController | Complete Upload Session | POST | /upload-sessions/{code}/complete | ✅ Yes | ✅ Yes | ✅ Yes | Extracts fileId |
| FileController | Get File By ID | GET | /files/{fileId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| FileController | Get File Metadata | GET | /files/{fileId}/metadata | ✅ Yes | ✅ Yes | ✅ Yes | |
| FileController | Delete File | DELETE | /files/{fileId} | ✅ Yes | ✅ Yes | ✅ Yes | Requires reason |
| FileController | Update Visibility | PUT | /files/{fileId}/visibility | ✅ Yes | ✅ Yes | ✅ Yes | |
| FileController | Link File to Owner | POST | /files/{fileId}/owners | ✅ Yes | ✅ Yes | ✅ Yes | |
| FileAccessController | Create Read URL | POST | /files/{fileId}/access/read-url | ✅ Yes | ✅ Yes | ✅ Yes | Extracts readUrl |
| FileAccessController | Validate Ownership | POST | /files/{fileId}/access/validate-ownership | ✅ Yes | ✅ Yes | ✅ Yes | |
| FileProcessingController | Start Processing | POST | /files/{fileId}/processing/start | ✅ Yes | ✅ Yes | ✅ Yes | Extracts processingJobId |
| FileProcessingController | Update Processing Result | PUT | /files/{fileId}/processing/result | ✅ Yes | ✅ Yes | ✅ Yes | |
| FileProcessingController | Get Processing Jobs | GET | /files/{fileId}/processing/jobs | ✅ Yes | ✅ Yes | ✅ Yes | |

## Coverage Statistics

| Category | Count | Covered | Coverage % |
|---|---|---|---|
| Total Endpoints | 12 | 12 | 100% |
| With Sample Request | 12 | 12 | 100% |
| With Test Scripts | 12 | 12 | 100% |
| ID Extraction Scripts | 4 | 4 | 100% |

## Known Limitations

1. **S3 Upload Step**: Step 2 of the upload flow (actual PUT to S3 presigned URL) cannot be automated in Postman without a pre-request script. The `Complete Upload Session` request will only succeed if the file was actually uploaded to S3.

2. **Checksum**: The `CompleteUploadSessionRequest.checksum` must be the actual MD5 hash of the uploaded file. The sample value in the collection is a placeholder.

3. **Processing Jobs**: File processing (thumbnail generation, virus scan) depends on background workers being running. Jobs may not be immediately visible.

4. **Dev Environment**: In development with MinIO, the presigned URL format differs from production S3 URLs.
