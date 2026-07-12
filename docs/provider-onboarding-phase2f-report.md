# Provider Onboarding Phase 2f — Expose Documents

**Date:** 2026-07-12
**Status:** Complete. Identity + BFF build with 0 errors. Migration applied.

## What was done

### 1. New columns on VerificationDocumentEntity

- `ContentType` (string?, max 200) — stores the MIME type directly (not overloading `Format`)
- `SizeInBytes` (long, default 0) — exact byte count from FileStorage (not a lossy display string)
- `CreateFromBff` updated to set both new columns alongside `Format`/`FileSizeDisplay` (backwards compat)
- Migration `AddVerificationDocumentContentTypeAndSize`: adds columns + backfills `ContentType` from `Format`

### 2. Documents returned in onboarding response

**Identity `ProviderOnboardingResponse`** now includes `List<ProviderDocumentDto> Documents`:
```json
{
  "fileId": "3fa85f64-...",
  "fileName": "license.pdf",
  "contentType": "application/pdf",
  "sizeInBytes": 245678,
  "documentType": "MaritimeLicense",
  "issuer": "Turkish Maritime Authority",
  "uploadedAt": "2026-07-12T10:31:00Z",
  "reviewStatus": null,
  "resolutionNote": null
}
```

The query handler `Include`s `VerificationDocuments` (excluding soft-deleted) and maps to `ProviderDocumentDto`.

**BFF `OnboardingResponse`** passes `Documents` through from Identity.

**Metadata only** — no `readUrl`, `bucketName`, `objectKey`, `storageProvider`, or internal numeric id.

### 3. Attach echoes the created document

`AttachProviderDocumentResponse` now includes `ProviderDocumentDto? Document` — the created document's metadata
is returned immediately, eliminating the need for a refetch after upload.

### Smoke

- `GET /onboarding` returns `documents: []` for profile with no attachments ✓
- Response contains no URL, bucket, object key, or internal numeric id ✓
- `ContentType` and `SizeInBytes` columns exist in DB ✓

## Build

- Identity: **0 errors** ✓
- MarineProvider BFF: **0 errors** ✓
