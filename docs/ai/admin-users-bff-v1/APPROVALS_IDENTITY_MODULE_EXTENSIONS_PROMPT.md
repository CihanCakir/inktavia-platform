# APPROVALS_IDENTITY_MODULE_EXTENSIONS_PROMPT.md
# Copilot Agent Prompt — Identity Module Extensions for Profile Approval Workflow

Paste this entire file into a Copilot Agent session opened at the **Aizen solution root**
(not just the BFF project — this touches the Identity module).

---

## Context

The AdminPanel BFF gap report identified the following missing pieces in the Identity module
that block the profile approval workflow from working in production:

| Gap | Priority |
|---|---|
| `OrganizerProfileListItemDto` / `VenueProfileListItemDto` missing `companyOrVenueName`, `email`, `phone` | MVP — blocks queue table |
| No `VerificationDocument` entity on organizer/venue profiles | MVP — blocks document tab |
| No `RiskSignal` entity or risk level calculation | MVP — blocks risk badge |
| `ApproveOrganizer`/`ApproveVenue`/`RejectOrganizer`/`RejectVenue` response missing `reviewedBy`, `reviewedAt` | MVP — blocks decision banner |
| `RejectOrganizer`/`RejectVenue` command missing `reasonCategory`, `internalNote`, `notifyUser` fields | MVP — blocks rejection modal full data |

All of these must be implemented before the approval screens go live.

---

## STEP 0 — Discovery (MANDATORY)

```bash
# 1. Find OrganizerProfile and VenueProfile entities
find Modules/Identity -name "OrganizerProfile*" -o -name "VenueProfile*" | sort

# 2. Find existing list item DTOs
grep -r "OrganizerProfileListItemDto\|VenueProfileListItemDto\|OrganizerProfileDto\|VenueProfileDto" \
  Modules/Identity/src --include="*.cs" | head -30

# 3. Find approve/reject commands and their handlers
find Modules/Identity -name "*Approve*" -o -name "*Reject*" | sort
grep -r "ApproveOrganizerProfile\|RejectOrganizerProfile\|ApproveVenueProfile\|RejectVenueProfile" \
  Modules/Identity/src --include="*.cs" | head -30

# 4. Find existing document-related entities (might already exist as stubs)
grep -r "VerificationDocument\|ProfileDocument\|IdentityDocument\|FileId" \
  Modules/Identity/src --include="*.cs" | head -40

# 5. Find existing DbContext and table mappings
grep -r "DbContext\|HasOne\|HasMany\|Fluent\|IEntityTypeConfiguration" \
  Modules/Identity/src --include="*.cs" | grep -v test | head -30

# 6. Check if RiskSignal entity already exists
grep -r "RiskSignal\|RiskLevel\|RiskScore" Modules/Identity/src --include="*.cs" | head -20

# 7. Confirm profile status enum values
grep -r "enum.*Status\|ApprovalStatus\|ProfileStatus" Modules/Identity/src --include="*.cs" | head -20

# 8. Confirm how migrations are run
find . -name "*.csproj" | xargs grep -l "EntityFrameworkCore" | head -10
```

Confirm before writing:
- Exact entity class names and namespaces (`OrganizerProfile`, `VenueProfile`)
- Exact DbContext class name for Identity module
- Whether `FileId` is already used anywhere in Identity (what type — `long`, `Guid`, `string`?)
- Existing migration strategy (EF Core code-first or snapshot-based)
- Whether approve/reject commands are CQRS command handlers or controller-level logic

---

## STEP 1 — Extend List Item DTOs

### 1a — Locate and extend OrganizerProfileListItemDto

Add the missing fields to `OrganizerProfileListItemDto`:

```csharp
// Fields to ADD (preserve all existing fields exactly):
public string? OrganizationName { get; init; }    // company/org name for queue display
public string? Email { get; init; }
public string? Phone { get; init; }
public string? City { get; init; }
public string? Country { get; init; }
public string? ReviewedAt { get; init; }           // ISO 8601 UTC, set on approve/reject
```

Map these from the `OrganizerProfile` entity in the query handler that builds this DTO.

### 1b — Locate and extend VenueProfileListItemDto

Add the missing fields to `VenueProfileListItemDto`:

```csharp
// Fields to ADD:
public string? VenueName { get; init; }
public string? OwnerName { get; init; }
public string? Email { get; init; }
public string? Phone { get; init; }
public string? City { get; init; }
public string? Country { get; init; }
public string? ReviewedAt { get; init; }
```

---

## STEP 2 — Add VerificationDocument entity

### 2a — Create the entity

Create `Modules/Identity/src/.../Entities/VerificationDocumentEntity.cs`:

```csharp
namespace Aizen.Modules.Identity.[Organizer|Common].Entities;

/// <summary>
/// Stores a reference to a file uploaded to FileStorage as a verification document.
/// The binary file is owned by the FileStorage module; only FileId is stored here.
/// </summary>
public class VerificationDocumentEntity
{
    public long Id { get; private set; }

    /// <summary>FK to OrganizerProfile or VenueProfile — confirm actual FK field name.</summary>
    public long ProfileId { get; private set; }

    /// <summary>Reference to binary file in FileStorage module. Type: match existing FileId convention.</summary>
    public long FileId { get; private set; }   // Use Guid if FileStorage.FileId is Guid — verify in STEP 0

    /// <summary>Human-readable document name. e.g. "Ticaret Sicil Belgesi"</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Document category. e.g. "TaxCertificate", "TradeRegistry", "InsurancePolicy"</summary>
    public string DocumentType { get; private set; } = string.Empty;

    /// <summary>File format snapshot. e.g. "PDF", "PNG"</summary>
    public string? Format { get; private set; }

    /// <summary>Human-readable size snapshot. e.g. "4.2 MB" — snapshot from FileStorage at upload time.</summary>
    public string? FileSizeDisplay { get; private set; }

    /// <summary>Issuing authority. e.g. "İstanbul Ticaret Sicil Müdürlüğü"</summary>
    public string? Issuer { get; private set; }

    /// <summary>Biometric match or verification score, if applicable. e.g. "98.4%"</summary>
    public string? MatchScore { get; private set; }

    public long UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    // EF constructor
    private VerificationDocumentEntity() { }

    public static VerificationDocumentEntity Create(
        long profileId,
        long fileId,
        string name,
        string documentType,
        string? format,
        string? fileSizeDisplay,
        string? issuer,
        long uploadedByUserId)
    {
        return new VerificationDocumentEntity
        {
            ProfileId = profileId,
            FileId = fileId,
            Name = name,
            DocumentType = documentType,
            Format = format,
            FileSizeDisplay = fileSizeDisplay,
            Issuer = issuer,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
        }
    }
}
```

> If `FileStorage.FileId` is `Guid`, change `FileId` to `Guid`. Run STEP 0 to verify before writing.

### 2b — Add to DbContext and EF configuration

Add `DbSet<VerificationDocumentEntity>` to the Identity DbContext.

Create EF configuration:

```csharp
public class VerificationDocumentEntityConfiguration : IEntityTypeConfiguration<VerificationDocumentEntity>
{
    public void Configure(EntityTypeBuilder<VerificationDocumentEntity> builder)
    {
        builder.ToTable("identity_verification_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(20);
        builder.Property(x => x.FileSizeDisplay).HasMaxLength(30);
        builder.Property(x => x.Issuer).HasMaxLength(300);
        builder.Property(x => x.MatchScore).HasMaxLength(20);
        // Add FK to OrganizerProfile or VenueProfile (use correct navigation pattern)
        builder.HasIndex(x => x.ProfileId);
        builder.HasIndex(x => x.FileId);
    }
}
```

Add EF Core migration:

```bash
# Adjust project/context name to match actual Identity module structure
dotnet ef migrations add AddVerificationDocuments \
  --project Modules/Identity/src/Aizen.Modules.Identity.Infrastructure \
  --startup-project ... \
  --context IdentityDbContext
```

### 2c — Add to OrganizerProfile and VenueProfile entities

```csharp
// In OrganizerProfile entity:
public ICollection<VerificationDocumentEntity> VerificationDocuments { get; private set; }
    = new List<VerificationDocumentEntity>();

// Domain method:
public void AddVerificationDocument(VerificationDocumentEntity document)
{
    VerificationDocuments.Add(document);
}
```

Same for `VenueProfile`.

### 2d — Add command to attach a verification document

Create `AddVerificationDocumentCommand`:

```csharp
// Command
public record AddOrganizerVerificationDocumentCommand(
    long UserId,
    long ProfileId,
    long FileId,
    string Name,
    string DocumentType,
    string? Format,
    string? FileSizeDisplay,
    string? Issuer
) : IRequest<AddVerificationDocumentResult>;

public record AddVerificationDocumentResult(long DocumentId);
```

Handler must:
1. Load `OrganizerProfile` by `(UserId, ProfileId)` — return `NotFound` if missing
2. Create `VerificationDocumentEntity.Create(...)` 
3. Call `profile.AddVerificationDocument(document)`
4. Save via DbContext
5. Return new document ID

Same for `AddVenueVerificationDocumentCommand`.

### 2e — Expose in Identity controller

Add endpoint:

```http
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/documents
POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/documents
```

Request body:
```json
{
  "fileId": 12345,
  "name": "Ticaret Sicil Belgesi",
  "documentType": "TradeRegistry",
  "format": "PDF",
  "fileSizeDisplay": "2.1 MB",
  "issuer": "İstanbul Ticaret Sicil Müdürlüğü"
}
```

### 2f — Include documents in detail query

Update the organizer/venue detail query to include `VerificationDocuments`:

```csharp
// EF Include in detail query
.Include(x => x.VerificationDocuments)
```

Map to DTO:

```csharp
Documents = profile.VerificationDocuments
    .OrderByDescending(d => d.UploadedAt)
    .Select(d => new VerificationDocumentIdentityDto(
        Id: d.Id,
        FileId: d.FileId,
        Name: d.Name,
        DocumentType: d.DocumentType,
        Format: d.Format,
        FileSizeDisplay: d.FileSizeDisplay,
        Issuer: d.Issuer,
        MatchScore: d.MatchScore,
        UploadedAt: d.UploadedAt.ToString("O")
    ))
    .ToList()
```

---

## STEP 3 — Add RiskSignal entity and risk level calculation

### 3a — Risk level decision

Risk level is **computed** from a list of `RiskSignal` records attached to a profile.
It is NOT a single stored field — it is derived at query time.

Calculation rules:

```
riskLevel =
  if any signal with Severity == "high"  → "H"
  else if any signal with Severity == "medium" → "M"
  else → "L"
```

### 3b — Create RiskSignalEntity

```csharp
public class RiskSignalEntity
{
    public long Id { get; private set; }
    public long ProfileId { get; private set; }

    /// <summary>"high" | "medium" | "low"</summary>
    public string Severity { get; private set; } = string.Empty;

    /// <summary>Short label. e.g. "Duplicate email detected"</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Detailed description for the reviewer.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Signal source code. e.g. "DUPLICATE_EMAIL" | "MISSING_TAX_ID" | "INCOMPLETE_COMPANY_DATA"</summary>
    public string SignalCode { get; private set; } = string.Empty;

    public DateTime DetectedAt { get; private set; }

    private RiskSignalEntity() { }

    public static RiskSignalEntity Create(
        long profileId, string severity, string title, string description, string signalCode)
    {
        return new RiskSignalEntity
        {
            ProfileId = profileId,
            Severity = severity,
            Title = title,
            Description = description,
            SignalCode = signalCode,
            DetectedAt = DateTime.UtcNow,
        };
    }
}
```

EF config: table `identity_risk_signals`, index on `ProfileId`.

### 3c — Automatic risk signal generation on profile submission

When an organizer/venue profile is submitted (status transitions to `Pending`), run a
`RiskAssessmentService` that evaluates the profile and creates `RiskSignalEntity` records:

```csharp
public class RiskAssessmentService
{
    public IReadOnlyList<RiskSignalEntity> Evaluate(OrganizerProfile profile)
    {
        var signals = new List<RiskSignalEntity>();

        // HIGH signals
        if (IsDuplicateEmail(profile.Email))
            signals.Add(RiskSignalEntity.Create(profile.Id, "high",
                "Duplicate email detected",
                "Another active profile uses this email address.",
                "DUPLICATE_EMAIL"));

        if (IsDuplicateTaxNumber(profile.TaxNumber))
            signals.Add(RiskSignalEntity.Create(profile.Id, "high",
                "Duplicate tax ID detected",
                "Another profile is registered with the same tax number.",
                "DUPLICATE_TAX_ID"));

        // MEDIUM signals
        if (string.IsNullOrWhiteSpace(profile.TaxNumber))
            signals.Add(RiskSignalEntity.Create(profile.Id, "medium",
                "Tax number not provided",
                "Tax number is missing from the application.",
                "MISSING_TAX_ID"));

        if (profile.VerificationDocuments?.Count == 0)
            signals.Add(RiskSignalEntity.Create(profile.Id, "medium",
                "No verification documents uploaded",
                "The applicant has not uploaded any verification documents.",
                "NO_DOCUMENTS"));

        if (string.IsNullOrWhiteSpace(profile.OrganizationName))
            signals.Add(RiskSignalEntity.Create(profile.Id, "medium",
                "Company name missing",
                "Organization name was not provided.",
                "MISSING_COMPANY_NAME"));

        // LOW signals
        if (string.IsNullOrWhiteSpace(profile.Phone))
            signals.Add(RiskSignalEntity.Create(profile.Id, "low",
                "Phone number not provided",
                "No contact phone number on record.",
                "MISSING_PHONE"));

        return signals;
    }
}
```

Call `RiskAssessmentService.Evaluate()` inside the command handler that transitions profile to `Pending`. Clear and regenerate signals on every resubmission.

### 3d — Compute risk level in query

Add to detail DTO:

```csharp
var riskLevel = signals.Any(s => s.Severity == "high") ? "H"
              : signals.Any(s => s.Severity == "medium") ? "M"
              : "L";
```

Also include in list item DTO by fetching aggregate per profile (use a `GROUP BY` subquery or a separate `MAX(Severity)` projection to avoid N+1).

### 3e — Expose RiskSignals in detail endpoint

Include in detail DTO:

```csharp
RiskSignals = profile.RiskSignals
    .Select(s => new RiskSignalIdentityDto(s.Severity, s.Title, s.Description, s.SignalCode, s.DetectedAt.ToString("O")))
    .ToList(),
RiskLevel = ComputeRiskLevel(profile.RiskSignals)
```

---

## STEP 4 — Extend approve/reject commands

### 4a — Approve command: return ReviewedBy + ReviewedAt

After setting the profile status to `Approved`:

```csharp
profile.Approve(reviewedByUserId: currentAdminUserId);
// profile.ReviewedBy = currentAdminUserId.ToString(); (or name — decide convention)
// profile.ReviewedAt = DateTime.UtcNow;
```

Update approval response DTO:

```csharp
public record ApproveOrganizerProfileResult(
    long UserId,
    long ProfileId,
    string Status,         // "approved"
    string? ReviewedBy,
    string? ReviewedAt     // ISO 8601 UTC
);
```

Extract `currentAdminUserId` from the incoming identity token claims inside the command handler.
Use the existing pattern for extracting the current user from the identity context.

### 4b — Reject command: add reasonCategory, internalNote, notifyUser

Extend `RejectOrganizerProfileCommand`:

```csharp
public record RejectOrganizerProfileCommand(
    long UserId,
    long ProfileId,
    string Reason,
    string? ReasonCategory,    // "missingDocuments" | "invalidTaxId" | etc.
    string? InternalNote,      // stored for internal audit only, not shown to user
    bool NotifyUser = true
) : IRequest<RejectProfileResult>;
```

Add fields to `OrganizerProfile`/`VenueProfile` entity:

```csharp
public string? RejectionReason { get; private set; }
public string? RejectionCategory { get; private set; }
public string? InternalNote { get; private set; }
public string? ReviewedBy { get; private set; }
public DateTime? ReviewedAt { get; private set; }
```

Domain method:

```csharp
public void Reject(string reason, string? reasonCategory, string? internalNote, string reviewedBy)
{
    Status = ProfileStatus.Rejected;
    RejectionReason = reason;
    RejectionCategory = reasonCategory;
    InternalNote = internalNote;
    ReviewedBy = reviewedBy;
    ReviewedAt = DateTime.UtcNow;
}
```

If `NotifyUser = true` and a Notification module exists, dispatch a notification from the handler.
If not, log the intent and document the gap.

Reject response DTO:

```csharp
public record RejectProfileResult(
    long UserId,
    long ProfileId,
    string Status,
    string? ReviewedBy,
    string? ReviewedAt,
    string? RejectionCategory
);
```

---

## STEP 5 — Migration and build

```bash
# Generate migration for all entity changes
dotnet ef migrations add ApprovalWorkflowExtensions \
  --project Modules/Identity/src/Aizen.Modules.Identity.Infrastructure \
  --context IdentityDbContext

# Build
dotnet build Aizen.sln --no-incremental
```

Expected: **0 errors**.

---

## STEP 6 — Generate report

Create `docs/reports/identity-approval-extensions-report.md`:

```markdown
## Identity Module — Approval Extensions Report

### Files Created / Modified
| File | Change |
|---|---|
| ...entity file... | Created |
| ...DTO file... | Extended |
| ...command file... | Extended |

### New Entities
- VerificationDocumentEntity: table identity_verification_documents
- RiskSignalEntity: table identity_risk_signals

### DTO Changes
- OrganizerProfileListItemDto: added OrganizationName, Email, Phone, ReviewedAt
- VenueProfileListItemDto: added VenueName, OwnerName, Email, Phone, ReviewedAt
- ApproveOrganizerProfileResult: added ReviewedBy, ReviewedAt
- RejectOrganizerProfileCommand: added ReasonCategory, InternalNote, NotifyUser
- OrganizerProfileDetailDto: added Documents[], RiskSignals[], RiskLevel
- VenueProfileDetailDto: added Documents[], RiskSignals[], RiskLevel

### New Endpoints
- POST /identity/admin/organizers/{userId}/profiles/{profileId}/documents
- POST /identity/admin/venues/{userId}/profiles/{profileId}/documents

### Risk Level Logic
- H: any signal with severity == "high"
- M: any signal with severity == "medium" (no high)
- L: no signals or all low

### Remaining Gaps
- NotifyUser integration pending (Notification module not yet wired)
- InternalNote not shown to applicant (correct)
- MatchScore field on documents populated by future biometric check service

### Build Result
0 errors
```

---

## NON-NEGOTIABLE RULES

1. Business IDs are `long`. Do NOT use `Guid` for ProfileId/UserId/DocumentId.
2. `FileId` type: use whatever type FileStorage module uses — verify in STEP 0.
3. Risk level is **computed** from RiskSignal records — not a stored field on the profile.
4. Signals are generated automatically when profile transitions to `Pending`.
5. `InternalNote` is stored but never returned in public-facing profile responses.
6. `ReviewedBy` and `ReviewedAt` must be set on every approve/reject action.
7. Build must pass with 0 errors.
8. Read existing code before writing.
