# Phase 3b Report: Verify and Close Gaps

**Date:** 2026-07-13
**Branch:** `feature/messaging-registration`

---

## 0. Stack / file-storage-api build verification

### DeleteFileAsync (FileStorageService)

The updated `DeleteFileAsync` at `Modules/FileStorage/src/Aizen.Modules.FileStorage.Repository/Services/FileStorageService.cs:203`:

1. Releases all active `FileOwnerReference` rows (`Deactivate()`)
2. Deletes the object from the bucket via `_storageProvider.DeleteObjectAsync`
3. Only then soft-deletes the row

If the bucket delete fails, the file is left unclaimed but NOT soft-deleted so the orphan sweep can retry on its next pass. This closes the bytes-leak-forever bug.

`Deactivate()` method exists on `FileOwnerReferenceEntity:36`. `_db.FileOwnerReferences` DbSet is available in the service.

### OrphanFileCleanupJob

`Modules/FileStorage/src/Aizen.Modules.FileStorage/Jobs/OrphanFileCleanupJob.cs`:

- Extends `AizenRecurringJob`, `IsActive = true`, cron `"30 * * * *"` (hourly at :30)
- Publishes `OrphanFileCleanupRequestedMessage` to trigger the existing consumer
- `Program.cs` has `TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }` so the job is picked up by assembly scanning

Both changes are structurally sound. **Not yet verified in a running container** -- requires `docker compose up -d --build file-storage-api`.

---

## 1. Browser verification

**Not performed.** This repo contains only backend code. The frontend (SPA) is in a separate repository. The two "already fixed" items (`useHydrateFormFromServerDraft` with `reset()`/`keepDirtyValues`, and `result.data.success` checks on step pages) cannot be verified from here.

**Browser-based testing is required** to prove:

- Server draft hydrates into form fields after `localStorage.clear()` + hard reload
- A failing save does not advance the wizard
- An empty submit is rejected with specific missing items
- A complete submit flips status to `Submitted`
- The document upload/delete chain works end-to-end (including MinIO object deletion)
- The orphan cleanup sweep reaps abandoned files

`curl` cannot reproduce the SPA's own code path and should not be substituted for browser verification.

---

## 2. Submit rejects documents with Rejected review state

**Problem:** `ProviderOnboardingDomainService.SubmitAsync` checked that at least one document exists but never checked documents' review state. A provider whose document was rejected by an admin could resubmit unchanged.

**Fix applied:**

### New enum: `DocumentReviewStatus`
`Modules/Identity/src/Aizen.Modules.Identity.Domain/Enum/DocumentReviewStatus.cs`
```csharp
public enum DocumentReviewStatus { Pending, Approved, Rejected }
```

### Entity changes: `VerificationDocumentEntity`
`Modules/Identity/src/Aizen.Modules.Identity.Domain/Entities/VerificationDocument/VerificationDocumentEntity.cs`

Added:
- `DocumentReviewStatus ReviewStatus` (defaults to `Pending`)
- `string? ResolutionNote`
- `SetReviewStatus(status, note)` method

### EF configuration
`VerificationDocumentEntityConfiguration.cs`: `ReviewStatus` stored as string (max 20), `ResolutionNote` max 1000, nullable.

### Validation in SubmitAsync
`Modules/Identity/src/Aizen.Modules.Identity.Repository/Service/Onboarding/ProviderOnboardingDomainService.cs:75-84`

Now loads all documents for the profile, checks for `DocumentReviewStatus.Rejected`, and names each rejected document in the `missing` items list:

```
"Document 'trade_license.pdf' (type: trade_license) has been rejected and must be replaced."
```

### DTO mapping
`GetProviderOnboardingQueryHandler.cs`: Now maps `ReviewStatus` and `ResolutionNote` into `ProviderDocumentDto` so the SPA can display review state.

**TODO:** An admin endpoint to actually set the review status (approve/reject individual documents) does not yet exist. The `DocumentReviewStatus` field is in the database and the submit validation checks it, but there is no command handler for admins to invoke it. The existing `RequestProviderOnboardingRevision` operates on onboarding steps, not individual documents.

---

## 3. Admin queue SubmittedAt fallback fixed

**Problem:** Both `MapOrganizerToQueueItem` and `MapVenueToQueueItem` in `GetProfileApprovalQueueBffQueryHandler` used:

```csharp
SubmittedAt = item.SubmittedAtUtc?.ToString("O") ?? item.CreateDate?.ToString("O"),
```

For providers who haven't submitted, this reported the registration date as `SubmittedAt`. The queue is sorted by this field, so `incomplete` rows were ordered by a date that means something else.

**Fix:** Both projections now use:

```csharp
SubmittedAt = item.SubmittedAtUtc?.ToString("O"),
```

`SubmittedAt` is `null` when there is no submission. The existing sort (`OrderByDescending(x => x.SubmittedAt ?? string.Empty)`) places null-SubmittedAt rows last, which is the correct behavior.

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminProfileApprovals/Query/GetProfileApprovalQueueBffQueryHandler.cs` -- lines 188 and 213.

---

## 4. Keycloak outage no longer reported as "code sent"

**Problem:** All four auth handlers caught exceptions from Identity/Keycloak calls and returned success responses (`Accepted = true` / `Resent = true`), hiding infrastructure failures as "if an account exists, a verification code has been sent."

**Fix:** The catch block now returns `Accepted = false` / `Resent = false` with `"Service temporarily unavailable. Please try again later."` The null-body fallback (unknown account) still returns the generic anti-enumeration response.

**Four handlers fixed:**

| Handler | File | Change |
|---------|------|--------|
| `RequestOtpLoginCommandHandler` | `Auth/OtpLogin/RequestOtpLogin/` | `Accepted = false` on exception |
| `ResendOtpLoginCommandHandler` | `Auth/OtpLogin/ResendOtpLogin/` | `Resent = false` on exception |
| `ForgotProviderPasswordCommandHandler` | `Auth/Password/ForgotProviderPassword/` | `Accepted = false` on exception |
| `ResendProviderPasswordOtpCommandHandler` | `Auth/Password/ResendProviderPasswordOtp/` | `Resent = false` on exception |

All four files are under `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/`.

**Anti-enumeration behavior preserved:** When Identity returns a null body (unknown account), the response is still `Accepted = true` with the generic message. Only transport/infrastructure exceptions produce the error response.

---

## Summary of open items

| Item | Status |
|------|--------|
| file-storage-api build & container test | Not run (needs `docker compose up --build`) |
| Browser verification of Phase 3 headline | Not done (frontend is a separate repo) |
| Admin endpoint to approve/reject individual documents | Not implemented (new command handler needed) |
| EF migration for `ReviewStatus` / `ResolutionNote` columns | Migration not generated (needs `dotnet ef migrations add`) |
| FileStorage gaps (phantom claims, size lie, rate limiting, tests) | Out of scope per `filestorage-phase2i` |
