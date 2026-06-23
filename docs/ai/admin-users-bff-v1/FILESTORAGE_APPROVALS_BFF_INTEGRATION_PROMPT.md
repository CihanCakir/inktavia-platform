# FILESTORAGE_APPROVALS_BFF_INTEGRATION_PROMPT.md
# Copilot Agent Prompt — FileStorage Integration for Approvals BFF

Paste this entire file into a Copilot Agent session opened at the **AdminPanel BFF project root**.
Identity module changes (VerificationDocument entity, etc.) must already be applied
(see `APPROVALS_IDENTITY_MODULE_EXTENSIONS_PROMPT.md`) before running this prompt.

---

## Architecture Principles (NON-NEGOTIABLE)

1. **Single binary owner**: FileStorage module is the ONLY owner of binary files on disk/S3/MinIO.
2. **Modules store FileId only**: Identity module stores `FileId` (long or Guid — match existing type). Never stores raw paths or URLs.
3. **BFF enriches with signed URLs**: All document URLs shown in the UI are short-lived signed URLs obtained by the BFF from FileStorage at request time. Never cache or store signed URLs in the database.
4. **Signed URL TTL**: Default 15 minutes for viewing; 5 minutes for upload pre-signed PUT URLs.
5. **Upload flow**: Browser → BFF (request pre-signed PUT URL from FileStorage) → BFF returns PUT URL to frontend → Browser uploads directly to MinIO/S3 → Browser notifies BFF of completion → BFF calls Identity to register `FileId` on the profile. BFF never handles raw file bytes.
6. **Provider abstraction**: `IFileStorageProvider` supports MinIO (local) and S3 (production). Switch via configuration — no compile-time changes.

---

## STEP 0 — Discovery (MANDATORY)

```bash
# 1. Find FileStorage module location and interfaces
find . -name "*FileStorage*" -o -name "*IFileStorage*" | sort
find . -name "*IFileStorageAdminBff*" -o -name "*FileStorageRemoteCall*" | sort

# 2. Find existing BFF remote call pattern
find . -name "*RemoteCall*" -o -name "*AizenRemoteCall*" | sort
grep -r "AizenRemoteCallPost\|AizenRemoteCallGet\|AizenApiResponse" \
  --include="*.cs" . | head -20

# 3. Find how BFF registers external services
grep -r "AddHttpClient\|IHttpClientFactory\|HttpClient" --include="*.cs" . | grep -v test | head -20

# 4. Find token forwarding / keycloak service token pattern
grep -r "IAdminPanelBffKeycloakServiceTokenProvider\|X-Aizen-User-Token\|ServiceToken" \
  --include="*.cs" . | head -20

# 5. Confirm FileStorage HTTP contract (URL, auth header)
grep -r "FileStorage\|filestorage\|file-storage" \
  --include="appsettings*.json" . | head -20

# 6. Find DI registration entry point
find . -name "Program.cs" -o -name "*ServiceCollectionExtensions*" -o -name "*Module*Registration*" | sort

# 7. Find existing BFF query handlers for profile detail
find . -name "*ProfileDetail*" -o -name "*OrganizerDetail*" -o -name "*VenueDetail*" | sort

# 8. Find ApprovalsController
find . -name "*ApprovalsController*" -o -name "*ProfileApprovalController*" | sort

# 9. Check FileStorage FileId type (long vs Guid)
grep -r "FileId\|fileId\|file_id" --include="*.cs" . | head -20
```

Do not assume anything. Read before writing.

---

## STEP 1 — Define FileStorage BFF remote call interface

Create `Infrastructure/FileStorage/IFileStorageAdminBffRemoteCall.cs`:

```csharp
namespace Aizen.AdminPanelBff.Infrastructure.FileStorage;

/// <summary>
/// BFF-side abstraction for calling the FileStorage module HTTP API.
/// Implementations must forward both the user token and the BFF service token.
/// </summary>
public interface IFileStorageAdminBffRemoteCall
{
    /// <summary>
    /// Request a pre-signed PUT URL so the browser can upload a file directly.
    /// </summary>
    Task<AizenApiResponse<PreSignedUploadUrlResponse>> RequestUploadUrlAsync(
        RequestUploadUrlDto request,
        string userToken,
        CancellationToken ct = default);

    /// <summary>
    /// Get a short-lived signed GET URL to view/download a file by its FileId.
    /// </summary>
    Task<AizenApiResponse<SignedDownloadUrlResponse>> GetSignedDownloadUrlAsync(
        long fileId,           // Use Guid if FileStorage uses Guid — verify in STEP 0
        string userToken,
        CancellationToken ct = default);

    /// <summary>
    /// Get signed GET URLs for multiple files in one call.
    /// </summary>
    Task<AizenApiResponse<BulkSignedDownloadUrlResponse>> GetBulkSignedDownloadUrlsAsync(
        IReadOnlyList<long> fileIds,
        string userToken,
        CancellationToken ct = default);

    /// <summary>
    /// Confirm that an upload completed (optional, if FileStorage requires explicit confirmation).
    /// </summary>
    Task<AizenApiResponse<UploadConfirmedResponse>> ConfirmUploadAsync(
        long fileId,
        ConfirmUploadDto request,
        string userToken,
        CancellationToken ct = default);
}
```

DTOs — create in `Infrastructure/FileStorage/Dtos/`:

```csharp
// Request upload URL
public record RequestUploadUrlDto(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? BucketTag     // e.g. "identity-verification-documents"
);

// Response
public record PreSignedUploadUrlResponse(
    long FileId,           // FileStorage-assigned ID — store this in Identity after upload
    string PutUrl,         // Pre-signed PUT URL (TTL 5 min)
    string BucketKey,      // Internal storage key
    DateTime ExpiresAt
);

// Signed download URL for one file
public record SignedDownloadUrlResponse(
    long FileId,
    string Url,            // Signed GET URL (TTL 15 min)
    DateTime ExpiresAt
);

// Bulk signed URLs
public record BulkSignedDownloadUrlResponse(
    IReadOnlyList<SignedFileUrlItem> Items
);
public record SignedFileUrlItem(long FileId, string? Url, bool Found);

// Confirm upload
public record ConfirmUploadDto(string BucketKey);
public record UploadConfirmedResponse(long FileId, bool IsConfirmed);
```

---

## STEP 2 — Implement IFileStorageAdminBffRemoteCall

Create `Infrastructure/FileStorage/FileStorageAdminBffRemoteCall.cs`:

```csharp
namespace Aizen.AdminPanelBff.Infrastructure.FileStorage;

public class FileStorageAdminBffRemoteCall : IFileStorageAdminBffRemoteCall
{
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileStorageAdminBffRemoteCall> _logger;

    // Base URL comes from configuration — see STEP 3
    public FileStorageAdminBffRemoteCall(
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        HttpClient httpClient,
        ILogger<FileStorageAdminBffRemoteCall> logger)
    {
        _serviceTokenProvider = serviceTokenProvider;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AizenApiResponse<PreSignedUploadUrlResponse>> RequestUploadUrlAsync(
        RequestUploadUrlDto request, string userToken, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetServiceTokenAsync(ct);
        return await AizenRemoteCallPost<RequestUploadUrlDto, PreSignedUploadUrlResponse>(
            _httpClient,
            "/api/v1/file-storage/upload/request-url",
            request,
            userToken,
            serviceToken,
            ct);
    }

    public async Task<AizenApiResponse<SignedDownloadUrlResponse>> GetSignedDownloadUrlAsync(
        long fileId, string userToken, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetServiceTokenAsync(ct);
        return await AizenRemoteCallGet<SignedDownloadUrlResponse>(
            _httpClient,
            $"/api/v1/file-storage/files/{fileId}/signed-url",
            userToken,
            serviceToken,
            ct);
    }

    public async Task<AizenApiResponse<BulkSignedDownloadUrlResponse>> GetBulkSignedDownloadUrlsAsync(
        IReadOnlyList<long> fileIds, string userToken, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetServiceTokenAsync(ct);
        return await AizenRemoteCallPost<object, BulkSignedDownloadUrlResponse>(
            _httpClient,
            "/api/v1/file-storage/files/signed-urls/bulk",
            new { fileIds },
            userToken,
            serviceToken,
            ct);
    }

    public async Task<AizenApiResponse<UploadConfirmedResponse>> ConfirmUploadAsync(
        long fileId, ConfirmUploadDto request, string userToken, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetServiceTokenAsync(ct);
        return await AizenRemoteCallPost<ConfirmUploadDto, UploadConfirmedResponse>(
            _httpClient,
            $"/api/v1/file-storage/files/{fileId}/confirm",
            request,
            userToken,
            serviceToken,
            ct);
    }
}
```

> Adapt the exact signatures of `AizenRemoteCallPost` / `AizenRemoteCallGet` to match the pattern
> already used in the BFF project (found in STEP 0). Do NOT invent a new HTTP calling convention.

---

## STEP 3 — Configuration

In `appsettings.json` and `appsettings.Development.json`:

```json
{
  "FileStorage": {
    "BaseUrl": "http://localhost:17003",  // adjust port
    "BucketTag": "identity-verification-documents",
    "SignedUrlTtlMinutes": 15,
    "UploadUrlTtlMinutes": 5
  }
}
```

Add a typed config class `FileStorageOptions`:

```csharp
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string BaseUrl { get; set; } = string.Empty;
    public string BucketTag { get; set; } = string.Empty;
    public int SignedUrlTtlMinutes { get; set; } = 15;
    public int UploadUrlTtlMinutes { get; set; } = 5;
}
```

---

## STEP 4 — Register in DI

In the BFF's DI registration (follow existing pattern):

```csharp
services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

services.AddHttpClient<FileStorageAdminBffRemoteCall>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<FileStorageOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
});

services.AddScoped<IFileStorageAdminBffRemoteCall, FileStorageAdminBffRemoteCall>();
```

---

## STEP 5 — Upload flow BFF endpoints

Add to the Approvals controller (existing controller — do not create a new one):

### 5a — Request pre-signed upload URL

```http
POST /api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}/documents/upload-url
POST /api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}/documents/upload-url
```

Handler for organizer (same pattern for venue):

```csharp
// Query/Command: RequestOrganizerDocumentUploadUrlCommand
public record RequestOrganizerDocumentUploadUrlCommand(
    long UserId,
    long ProfileId,
    string UserToken,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DocumentType,
    string? DocumentName
) : IRequest<RequestDocumentUploadUrlResult>;

public record RequestDocumentUploadUrlResult(
    long FileId,
    string PutUrl,
    DateTime ExpiresAt
);
```

Handler steps:
1. Verify profile exists via Identity remote call (or accept on trust if admin role verified at controller level).
2. Call `_fileStorageRemoteCall.RequestUploadUrlAsync(new RequestUploadUrlDto(FileName, ContentType, FileSizeBytes, BucketTag), userToken, ct)`.
3. Return `FileId`, `PutUrl`, `ExpiresAt` to frontend.

### 5b — Confirm upload and register FileId with Identity

```http
POST /api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}/documents
POST /api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}/documents
```

Request body:
```json
{
  "fileId": 12345,
  "documentType": "TradeRegistry",
  "name": "Ticaret Sicil Belgesi",
  "format": "PDF",
  "fileSizeDisplay": "2.1 MB",
  "issuer": "İstanbul Ticaret Sicil Müdürlüğü"
}
```

Handler steps:
1. Call FileStorage `ConfirmUploadAsync(fileId, ...)` — verifies file physically exists.
2. Call Identity endpoint `POST /identity/admin/organizers/{userId}/profiles/{profileId}/documents` with `{ fileId, documentType, name, format, fileSizeDisplay, issuer }`.
3. Return `{ documentId, fileId, message: "Document registered" }`.

---

## STEP 6 — Enrich detail responses with signed URLs

The organizer and venue detail query handlers currently return document list items with only `fileId` from Identity.
Update them to fetch signed URLs from FileStorage for all documents in one bulk call.

### In `GetOrganizerProfileApprovalDetailQueryHandler`:

```csharp
// After mapping documents from Identity response:
var fileIds = documents.Select(d => d.FileId).ToList();

AizenApiResponse<BulkSignedDownloadUrlResponse>? signedUrlResponse = null;
if (fileIds.Count > 0)
{
    signedUrlResponse = await _fileStorageRemoteCall
        .GetBulkSignedDownloadUrlsAsync(fileIds, userToken, ct);
}

var signedUrlMap = signedUrlResponse?.Data?.Items
    .Where(x => x.Found)
    .ToDictionary(x => x.FileId, x => x.Url)
    ?? new Dictionary<long, string?>();

// Map to BFF response DTO with URL included:
var enrichedDocuments = documents.Select(d => new ProfileApprovalDocumentBffDto(
    Id: d.Id,
    FileId: d.FileId,
    Type: d.DocumentType,
    Name: d.Name,
    Url: signedUrlMap.GetValueOrDefault(d.FileId),    // null if FileStorage unavailable
    Format: d.Format,
    FileSizeDisplay: d.FileSizeDisplay,
    Issuer: d.Issuer,
    MatchScore: d.MatchScore,
    UploadedAt: d.UploadedAt
)).ToList();
```

Same pattern in `GetVenueProfileApprovalDetailQueryHandler`.

### BFF response DTO for document:

```csharp
public record ProfileApprovalDocumentBffDto(
    long Id,
    long FileId,
    string Type,
    string Name,
    string? Url,             // signed GET URL — null if FileStorage was unavailable
    string? Format,
    string? FileSizeDisplay,
    string? Issuer,
    string? MatchScore,
    string UploadedAt        // ISO 8601 UTC
);
```

**Critical**: If FileStorage bulk call fails (HTTP error or timeout), log the error and continue with `Url = null` for all documents. Do NOT fail the entire detail endpoint because of a file URL fetch error. Document metadata must always be visible even if preview is unavailable.

---

## STEP 7 — Queue list: no signed URLs needed

The queue list endpoint (`GET /users/profile-approvals`) does NOT need to enrich documents.
Documents are only shown in the detail view. No changes needed to the queue list handler.

---

## STEP 8 — Controller: add new endpoints (with AdminPanelAccess policy)

Add to the existing approvals controller — do NOT create a new controller:

```csharp
[HttpPost("organizers/{userId}/profiles/{profileId}/documents/upload-url")]
[Authorize(Policy = "AdminPanelAccess")]
public async Task<IActionResult> RequestOrganizerDocumentUploadUrl(
    [FromRoute] long userId,
    [FromRoute] long profileId,
    [FromBody] RequestDocumentUploadUrlRequest request,
    CancellationToken ct)
{
    var userToken = Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
    var command = new RequestOrganizerDocumentUploadUrlCommand(
        userId, profileId, userToken,
        request.FileName, request.ContentType, request.FileSizeBytes,
        request.DocumentType, request.DocumentName);
    var result = await _mediator.Send(command, ct);
    return Ok(result);
}

[HttpPost("organizers/{userId}/profiles/{profileId}/documents")]
[Authorize(Policy = "AdminPanelAccess")]
public async Task<IActionResult> RegisterOrganizerDocument(
    [FromRoute] long userId,
    [FromRoute] long profileId,
    [FromBody] RegisterDocumentRequest request,
    CancellationToken ct)
{
    var userToken = Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
    var command = new RegisterOrganizerVerificationDocumentCommand(
        userId, profileId, userToken,
        request.FileId, request.DocumentType, request.Name,
        request.Format, request.FileSizeDisplay, request.Issuer);
    var result = await _mediator.Send(command, ct);
    return Ok(result);
}

// Venue equivalents: same pattern with VenueDocument commands
```

Request models:

```csharp
public record RequestDocumentUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DocumentType,
    string? DocumentName
);

public record RegisterDocumentRequest(
    long FileId,
    string DocumentType,
    string Name,
    string? Format,
    string? FileSizeDisplay,
    string? Issuer
);
```

---

## STEP 9 — FluentValidation

```csharp
public class RequestDocumentUploadUrlRequestValidator
    : AbstractValidator<RequestOrganizerDocumentUploadUrlCommand>
{
    private static readonly string[] AllowedTypes = ["application/pdf", "image/jpeg", "image/png"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public RequestDocumentUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).Must(t => AllowedTypes.Contains(t))
            .WithMessage("Only PDF, JPEG, and PNG files are accepted.");
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File size must be between 1 byte and 10 MB.");
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(100);
    }
}
```

---

## STEP 10 — Build and smoke tests

```bash
dotnet build Aizen.AdminPanelBff.sln
# Expected: 0 errors
```

### Manual smoke test sequence

```
# 1. Request upload URL
POST /api/v1/admin-panel/users/profile-approvals/organizers/1/profiles/2/documents/upload-url
Headers: X-Aizen-User-Token: <token>
Body: { "fileName": "test.pdf", "contentType": "application/pdf", "fileSizeBytes": 102400, "documentType": "TradeRegistry" }
Expected: 200 OK with { fileId, putUrl, expiresAt }

# 2. Upload file directly to putUrl (curl to MinIO)
curl -X PUT "<putUrl>" --data-binary @test.pdf -H "Content-Type: application/pdf"
Expected: 200 OK from MinIO

# 3. Confirm upload and register with Identity
POST /api/v1/admin-panel/users/profile-approvals/organizers/1/profiles/2/documents
Body: { "fileId": <returned fileId>, "documentType": "TradeRegistry", "name": "Trade Registry", "format": "PDF", "fileSizeDisplay": "100 KB" }
Expected: 200 OK with { documentId, fileId }

# 4. Fetch detail — should contain enriched document with signed URL
GET /api/v1/admin-panel/users/profile-approvals/organizers/1/profiles/2
Expected: 200 OK, documents[] contains { url: "https://minio.local/..." }

# 5. FileStorage unavailable: stop FileStorage, fetch detail
Expected: 200 OK, documents[] contains { url: null }, no 5xx
```

---

## STEP 11 — Generate report

Create `docs/reports/filestorage-approvals-bff-integration-report.md`:

```markdown
## FileStorage × Approvals BFF Integration Report

### Files Created
| File | Purpose |
|---|---|
| Infrastructure/FileStorage/IFileStorageAdminBffRemoteCall.cs | Interface |
| Infrastructure/FileStorage/FileStorageAdminBffRemoteCall.cs | Implementation |
| Infrastructure/FileStorage/Dtos/*.cs | Request/response DTOs |
| Infrastructure/FileStorage/FileStorageOptions.cs | Typed config |

### Files Modified
| File | Change |
|---|---|
| ApprovalsController.cs | Added 4 new endpoints (upload-url × 2, register doc × 2) |
| GetOrganizerProfileApprovalDetailQueryHandler.cs | Added bulk signed URL enrichment |
| GetVenueProfileApprovalDetailQueryHandler.cs | Added bulk signed URL enrichment |
| appsettings.json | Added FileStorage section |
| DI registration file | Registered HttpClient + IFileStorageAdminBffRemoteCall |

### New BFF Endpoints
- POST .../organizers/{userId}/profiles/{profileId}/documents/upload-url
- POST .../organizers/{userId}/profiles/{profileId}/documents
- POST .../venues/{userId}/profiles/{profileId}/documents/upload-url
- POST .../venues/{userId}/profiles/{profileId}/documents

### Resilience Notes
- FileStorage bulk URL fetch: failure returns url=null, not 5xx
- Upload confirmation step verifies file physically exists before registering in Identity
- All signed URLs expire (15 min view, 5 min upload)

### Build Result
0 errors

### Remaining
- MinIO bucket for "identity-verification-documents" must be created in infra
- S3 bucket equivalent for production
- MatchScore field: not populated yet (pending biometric check service)
```

---

## NON-NEGOTIABLE RULES

1. Never store signed URLs in the database.
2. Never handle raw file bytes in the BFF — only pre-signed URL exchange.
3. `FileId` type must match FileStorage module — verify in STEP 0 before writing any code.
4. Detail endpoints must succeed even if FileStorage is down (`url = null` on documents).
5. All controller endpoints must carry `[Authorize(Policy = "AdminPanelAccess")]`.
6. Use the existing `AizenRemoteCallPost/Get` pattern — do not invent a new HTTP call convention.
7. User token is read from `X-Aizen-User-Token` header, forwarded to both Identity and FileStorage.
8. BFF acquires Keycloak service token via `IAdminPanelBffKeycloakServiceTokenProvider` — not from the request.
9. Build must pass with 0 errors.
10. Read existing code before writing.
