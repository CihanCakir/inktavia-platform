## FileStorage × Approvals BFF Integration Report

### Files Created

| File | Purpose |
|---|---|
| `AdminProfileApprovals/Dto/DocumentUploadBffDtos.cs` | `DocumentUploadUrlBffResponse`, `RegisterDocumentBffResponse` |
| `AdminProfileApprovals/Command/RequestOrganizerDocumentUploadUrlCommand.cs` | Command |
| `AdminProfileApprovals/Command/RequestOrganizerDocumentUploadUrlCommandHandler.cs` | Calls `FileStorage.CreateDocumentUploadSession`, returns pre-signed PUT URL |
| `AdminProfileApprovals/Command/RequestVenueDocumentUploadUrlCommand.cs` | Command |
| `AdminProfileApprovals/Command/RequestVenueDocumentUploadUrlCommandHandler.cs` | Same as organizer handler |
| `AdminProfileApprovals/Command/RegisterOrganizerVerificationDocumentCommand.cs` | Command |
| `AdminProfileApprovals/Command/RegisterOrganizerVerificationDocumentCommandHandler.cs` | CompleteUploadSession → RegisterOrganizerVerificationDocument |
| `AdminProfileApprovals/Command/RegisterVenueVerificationDocumentCommand.cs` | Command |
| `AdminProfileApprovals/Command/RegisterVenueVerificationDocumentCommandHandler.cs` | CompleteUploadSession → RegisterVenueVerificationDocument |

### Files Modified

| File | Change |
|---|---|
| `Common/RemoteClients/IFileStorageAdminBffRemoteCall.cs` | Added `CreateDocumentUploadSession`, `CompleteDocumentUploadSession` methods + 4 new types |
| `Common/RemoteClients/IIdentityAdminBffRemoteCall.cs` | Added `RegisterOrganizerVerificationDocument`, `RegisterVenueVerificationDocument` + 2 new types |
| `AdminProfileApprovals/Dto/SharedApprovalBffDtos.cs` | Added `Url?` field to `ProfileApprovalDocumentBffDto` |
| `AdminProfileApprovals/Query/GetOrganizerApprovalDetailBffQueryHandler.cs` | Injected `IFileStorageAdminBffRemoteCall`; maps `detail.Documents` → `ProfileApprovalDocumentBffDto`; enriches with parallel signed URL fetch; maps `RiskSignals` |
| `AdminProfileApprovals/Query/GetVenueApprovalDetailBffQueryHandler.cs` | Same as organizer handler |
| `Controllers/V1/AdminProfileApprovalsController.cs` | Added 4 new endpoints + `RequestDocumentUploadUrlRequest` / `RegisterDocumentRequest` models |

### New BFF Endpoints

| Method | Path | Handler |
|---|---|---|
| POST | `.../organizers/{userId}/profiles/{profileId}/documents/upload-url` | `RequestOrganizerDocumentUploadUrlCommand` |
| POST | `.../organizers/{userId}/profiles/{profileId}/documents` | `RegisterOrganizerVerificationDocumentCommand` |
| POST | `.../venues/{userId}/profiles/{profileId}/documents/upload-url` | `RequestVenueDocumentUploadUrlCommand` |
| POST | `.../venues/{userId}/profiles/{profileId}/documents` | `RegisterVenueVerificationDocumentCommand` |

All under `/api/v1/admin-panel/users/profile-approvals/` with `[Authorize(Policy = "AdminPanelAccess")]`.

### Upload Flow

```
1. POST .../documents/upload-url
   → BFF calls FileStorage POST /api/v1/file-storage/upload-sessions
   ← returns { fileId (Guid), uploadUrl (PUT), uploadSessionCode, expiresAt }

2. Browser PUT fileBytes to uploadUrl (direct to MinIO/S3, BFF never handles bytes)

3. POST .../documents  { fileId, uploadSessionCode, documentType, name, ... }
   → BFF calls FileStorage POST /api/v1/file-storage/upload-sessions/{code}/complete
   → BFF calls Identity POST /api/v1/identity/admin/.../documents
   ← returns { documentId, fileId }
```

### Detail Enrichment (STEP 6)

- `GetOrganizerApprovalDetailBffQueryHandler` and `GetVenueApprovalDetailBffQueryHandler` now map `detail.Documents` (from Identity's `VerificationDocumentDto`) to `ProfileApprovalDocumentBffDto`.
- Signed GET URLs are fetched in parallel via `IFileStorageAdminBffRemoteCall.CreateReadUrl` (one call per document, 15 min TTL).
- **Resilience**: if FileStorage is down or any URL fetch fails, `Url = null` on that document; the detail endpoint still returns 200 with full document metadata. A warning is added to the response.
- `RiskSignals` from Identity are also mapped (previously empty list).
- `DocumentsUploaded` checklist flag is now derived from `detail.Documents.Count > 0`.

### Resilience Notes

- FileStorage bulk URL fetch: failure returns `Url = null` per document, not 5xx
- `CompleteUploadSession` failure blocks document registration (prevents orphan Identity records)
- All remote call failures are logged at Warning level and surfaced in `Warnings[]`
- Auth token acquisition failure returns early with `Warnings[]`

### Build Result

**0 errors** (591 pre-existing warnings, unchanged)

### FileStorage Remote Call Pattern

Uses Refit (`AizenRemoteCallPost`/`AizenRemoteCallGet` attributes) — same pattern as all other BFF remote calls. No new HttpClient registration needed; `IFileStorageAdminBffRemoteCall` is already registered in `DependencyInjection.cs`.

### Remaining / Known Gaps

- `MatchScore` field on documents: not populated by Identity yet (pending biometric check service)
- `FileId` type: FileStorage uses `Guid`; Identity's `VerificationDocumentDto.FileId` is `long`. The BFF converts `long → string` in the detail response and accepts `Guid-as-string` in the register request. Full alignment requires Identity module to clarify its storage model.
- MinIO bucket for verification documents must be provisioned in infra before upload sessions will succeed
- S3 bucket equivalent for production
