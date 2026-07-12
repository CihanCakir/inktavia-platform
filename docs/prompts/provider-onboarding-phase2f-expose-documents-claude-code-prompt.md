# Claude Code Prompt — Phase 2f: expose the document list (blocks the frontend)

Small and self-contained. The provider-web signed-URL upload flow is now implemented
(`documentsApi.ts`, `ComplianceVerificationPage`), but **the backend never returns the attached
documents**, so a provider uploads a file, sees it appear, reloads the page — and the list is empty.

`GET /api/v1/provider/onboarding` returns `OnboardingResponse` with `profileId, status, schemaVersion,
stepStatuses, draft, revisionSteps, revisionNote, lastSavedAtUtc, submittedAtUtc` — and **no documents**.
Identity's `ProviderOnboardingResponse` does not carry them either, and its onboarding query never touches
`VerificationDocuments`.

`AttachDocumentBffResponse` only returns `{ success, message }`, so the frontend cannot even reconstruct the
row it just created.

## 1. Two fields are missing on the entity (data is being destroyed today)

`VerificationDocumentEntity.CreateFromBff` does this:

```csharp
Format          = contentType,
FileSizeDisplay = sizeInBytes > 0 ? $"{sizeInBytes / 1024.0:F0} KB" : null,
FileId          = 0,
```

- **`SizeInBytes` is thrown away.** The exact byte count — which was just validated against FileStorage — is
  converted into a lossy display string. It cannot be recovered, and the UI cannot format sizes properly.
- `Format` is being used to hold a MIME type. That works, but it is misnamed and will confuse the next reader.
- `FileId` (the legacy internal `long`) is now written as `0` — a dead column since the Guid migration.

Fix:
- Add `ContentType` (string) and `SizeInBytes` (long) columns; populate them from the FileStorage snapshot.
- Migration `AddVerificationDocumentContentTypeAndSize`, back-filling `ContentType` from `Format` and
  `SizeInBytes` from FileStorage where the file still exists (rows that cannot be resolved: leave `0` and
  **report the count** — do not guess by parsing `FileSizeDisplay`).
- Either drop the dead `FileId` column or keep it only for legacy rows and stop writing `0` into it. State which
  you chose.

## 2. Return the documents

Add to **Identity's** `ProviderOnboardingResponse` and **the BFF's** `OnboardingResponse`:

```jsonc
"documents": [
  {
    "fileId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // FilePublicId (Guid) — never the internal long
    "fileName": "license.pdf",
    "contentType": "application/pdf",
    "sizeInBytes": 245678,
    "documentType": "MaritimeLicense",
    "issuer": "Turkish Maritime Authority",   // nullable
    "uploadedAt": "2026-07-12T10:31:00Z",
    "reviewStatus": null,                     // "Pending"|"Approved"|"NeedsReview"|"Rejected" once Phase 2e Part 5 lands
    "resolutionNote": null
  }
]
```

The Identity onboarding query must `Include` the profile's `VerificationDocuments` (excluding soft-deleted) and
map them. The BFF passes them straight through.

**Metadata only.** No `readUrl`, no `bucketName`, no `objectKey`, no `storageProvider`, and no internal numeric
id — a signed read URL is minted on demand by
`POST /onboarding/documents/{fileId}/access-url`, which already exists.

This is exactly the shape `ProviderDocument` in `src/features/onboarding/model/onboardingTypes.ts` already expects;
match it field-for-field so no frontend change is needed.

## 3. Make attach echo the created document (optional but cheap)

Have `AttachDocumentBffResponse` return the created `ProviderDocument` alongside `{ success, message }`. The
frontend currently refetches `GET /onboarding` after every upload; echoing the row lets it update optimistically
and removes a round-trip.

## Test

1. Provider uploads a document → `GET /onboarding` returns it in `documents` with the **real** `sizeInBytes`
   (not a rounded KB string) and the file's public Guid.
2. Reload the browser → the document is still listed (it comes from the server, not local storage).
3. The response contains **no** URL, bucket, object key or internal numeric id — grep the JSON to prove it.
4. Delete the document → it disappears from `documents`.
5. Two providers each upload a document → each sees only their own.
