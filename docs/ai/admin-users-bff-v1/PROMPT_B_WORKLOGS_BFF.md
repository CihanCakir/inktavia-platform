# WorkLogs — BFF Implementation Prompt

**Target project:** `Aizen.AdminPanel.BFF`
**Prerequisite:** `PROMPT_A_WORKLOGS_MICROSERVICE.md` must be complete and the microservice deployed/running.
**Scope:** Extend the BFF to expose work-logs endpoints, aggregate microservice data, and resolve `MediaFileId` → signed URL via FileStorage.

---

## ⚠️ STEP 0 — MANDATORY reads before writing anything

### 0a. BFF project structure

```
ls -R Aizen.AdminPanel.BFF/
```

Identify:
- Module folder for ServiceRequests (e.g., `Modules/ServiceRequests/`)
- Existing controller(s) for service requests — read them all
- Existing Refit interface for the ServiceRequest microservice (`IServiceRequestMicroserviceRemoteCall` or similar)
- Existing DTO folder and naming convention

### 0b. Read the existing Refit interface

Find `IServiceRequestMicroserviceRemoteCall` (or equivalent). Note:
- Every method already defined (DO NOT DUPLICATE)
- Exact namespace and file path
- How `[Get]`, `[Post]`, `[Patch]` attributes are used
- CancellationToken usage pattern

### 0c. Read existing BFF controller(s) for ServiceRequests

Note:
- Base class and `[Authorize]` placement
- Route prefix (e.g., `[Route("api/admin/service-requests")]`)
- Response wrapper (`AizenOk`, `Ok`, `ApiResponse.Success`, or whatever is in use)
- How FileStorage URL resolution is called in existing endpoints (search for `IFileStorageService`, `IDocumentUrlResolver`, or similar)

### 0d. Read FileStorage resolution pattern

Search for how other BFF controllers resolve file IDs to URLs:
- Look for `IFileStorageService`, `IDocumentService`, `IFileUrlResolver`
- Find the method signature (e.g., `GetReadUrlAsync(string fileId)` or `ResolveUrlAsync(string fileId)`)
- Note: URL resolution MUST degrade gracefully — if FileStorage returns an error, set `MediaUrl = null`, do NOT propagate a 500

### 0e. Read existing DTO records

Read 2–3 existing DTOs. Confirm:
- `sealed record` or `class`?
- `{ get; init; }` vs `{ get; set; }`?
- Namespace convention

Only after Step 0 is complete, proceed.

---

## Step 1 — Extend Refit Interface

Add only the methods that are MISSING from the interface found in Step 0b. Do not re-add existing methods.

```csharp
// ── Work Logs ─────────────────────────────────────────────────────────────────

/// <summary>GET /api/service-requests/{id}/work-logs</summary>
[Get("/api/service-requests/{id}/work-logs")]
Task<ApiResponse<WorkLogsRemoteDto>> GetWorkLogsAsync(
    long id,
    CancellationToken ct = default);

/// <summary>POST /api/service-requests/{id}/work-logs</summary>
[Post("/api/service-requests/{id}/work-logs")]
Task<ApiResponse<WorkLogEntryRemoteDto>> AddWorkLogEntryAsync(
    long id,
    [Body] AddWorkLogEntryRemoteRequest body,
    CancellationToken ct = default);

/// <summary>PATCH /api/service-requests/{id}/work-logs/phases/{phaseNumber}</summary>
[Patch("/api/service-requests/{id}/work-logs/phases/{phaseNumber}")]
Task<ApiResponse<WorkPhaseRemoteDto>> UpdateWorkPhaseAsync(
    long id,
    int phaseNumber,
    [Body] UpdateWorkPhaseRemoteRequest body,
    CancellationToken ct = default);
```

---

## Step 2 — Remote DTOs (microservice response shapes)

These mirror the microservice's `GetWorkLogsResult` exactly. Place in the DTOs folder for the ServiceRequests module.

```csharp
// ── Remote DTOs (from microservice) ──────────────────────────────────────────

/// <summary>Mirrors GetWorkLogsResult from the microservice.</summary>
public sealed record WorkLogsRemoteDto
{
    public long ServiceRequestId { get; init; }
    public string Title { get; init; } = string.Empty;
    public WorkPhaseRemoteDto? CurrentPhase { get; init; }
    public IReadOnlyList<WorkPhaseRemoteDto> Phases { get; init; } = [];
    public IReadOnlyList<WorkLogEntryRemoteDto> LogEntries { get; init; } = [];
    public ProviderActivityRemoteDto? ProviderActivity { get; init; }
    public JobHealthRemoteDto? JobHealth { get; init; }
}

public sealed record WorkPhaseRemoteDto
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public string Status { get; init; } = "Upcoming";
}

public sealed record WorkLogEntryRemoteDto
{
    public long Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    /// <summary>Raw file ID — BFF will resolve to a signed URL.</summary>
    public string? MediaFileId { get; init; }
    public string Author { get; init; } = string.Empty;
}

public sealed record ProviderActivityRemoteDto
{
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<ProviderActivityEntryRemoteDto> Activities { get; init; } = [];
}

public sealed record ProviderActivityEntryRemoteDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "check_circle";
}

public sealed record JobHealthRemoteDto
{
    public int Score { get; init; }
    public string Label { get; init; } = string.Empty;
    public int DaysRemaining { get; init; }
    public string MilestoneReached { get; init; } = "0";
}

public sealed record AddWorkLogEntryRemoteRequest
{
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public long? AuthorId { get; init; }
    public string? MediaFileId { get; init; }
}

public sealed record UpdateWorkPhaseRemoteRequest
{
    public int ProgressPercent { get; init; }
    public string Status { get; init; } = string.Empty;
}
```

---

## Step 3 — BFF Response DTOs (sent to frontend)

These are the shapes the React frontend's `useWorkLogsQuery` TypeScript hook expects.
The frontend `WorkLogsDto` interface is:

```ts
{
  serviceRequestId: string    // ← BFF serialises long as string
  title: string
  currentPhase: WorkPhase
  phases: WorkPhase[]         // WorkPhase: { number, title, progressPercent, status }
  logEntries: WorkLogEntry[]  // { id, timestamp, type, content, mediaUrl?, author }
  providerActivity: ProviderActivity
  jobHealth: JobHealth        // { score, label, daysRemaining, milestoneReached }
}
```

Create the BFF response DTOs:

```csharp
// ── BFF Response DTOs (sent to frontend) ─────────────────────────────────────

public sealed record WorkLogsBffResponse
{
    public string ServiceRequestId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public WorkPhaseBffDto? CurrentPhase { get; init; }
    public IReadOnlyList<WorkPhaseBffDto> Phases { get; init; } = [];
    public IReadOnlyList<WorkLogEntryBffDto> LogEntries { get; init; } = [];
    public ProviderActivityBffDto ProviderActivity { get; init; } = new();
    public JobHealthBffDto JobHealth { get; init; } = new();
}

public sealed record WorkPhaseBffDto
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public string Status { get; init; } = "Upcoming";
}

public sealed record WorkLogEntryBffDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    /// <summary>Signed read URL resolved by BFF. Null when file is unavailable.</summary>
    public string? MediaUrl { get; init; }
    public string Author { get; init; } = string.Empty;
}

public sealed record ProviderActivityBffDto
{
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<ProviderActivityEntryBffDto> Activities { get; init; } = [];
}

public sealed record ProviderActivityEntryBffDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "check_circle";
}

public sealed record JobHealthBffDto
{
    public int Score { get; init; }
    public string Label { get; init; } = string.Empty;
    public int DaysRemaining { get; init; }
    public string MilestoneReached { get; init; } = "0";
}

public sealed record WorkLogEntryAddedBffResponse
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? MediaUrl { get; init; }
    public string Author { get; init; } = string.Empty;
}
```

---

## Step 4 — BFF Request Models + Validators

```csharp
// ── BFF request models ────────────────────────────────────────────────────────

public sealed record AddWorkLogEntryBffRequest
{
    /// <summary>note | photo | status_change</summary>
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public long? AuthorId { get; init; }
    /// <summary>FileStorage file ID for photo entries.</summary>
    public string? MediaFileId { get; init; }
}

public sealed record UpdateWorkPhaseBffRequest
{
    public int ProgressPercent { get; init; }
    /// <summary>Upcoming | In Progress | Completed</summary>
    public string Status { get; init; } = string.Empty;
}
```

```csharp
// ── Validators ────────────────────────────────────────────────────────────────
using FluentValidation;

public sealed class AddWorkLogEntryBffRequestValidator
    : AbstractValidator<AddWorkLogEntryBffRequest>
{
    private static readonly string[] AllowedTypes = ["note", "photo", "status_change"];

    public AddWorkLogEntryBffRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.Author)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.MediaFileId)
            .MaximumLength(256)
            .When(x => x.MediaFileId is not null);
    }
}

public sealed class UpdateWorkPhaseBffRequestValidator
    : AbstractValidator<UpdateWorkPhaseBffRequest>
{
    private static readonly string[] AllowedStatuses = ["Upcoming", "In Progress", "Completed"];

    public UpdateWorkPhaseBffRequestValidator()
    {
        RuleFor(x => x.ProgressPercent)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s, StringComparer.Ordinal))
            .WithMessage($"Status must be: {string.Join(", ", AllowedStatuses)}.");
    }
}
```

---

## Step 5 — BFF Controller Endpoints

Add these methods to the existing ServiceRequests BFF controller (found in Step 0c). Follow the base class, route style, auth attribute, and response wrapper already present.

```csharp
// ── Work Logs ─────────────────────────────────────────────────────────────────

/// <summary>
/// Returns work-log entries, phases, provider activity and job health.
/// MediaFileId values are resolved to signed URLs via FileStorage.
/// </summary>
[HttpGet("{id}/work-logs")]
[ProducesResponseType(typeof(WorkLogsBffResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetWorkLogs(
    [FromRoute] string id,
    CancellationToken ct)
{
    // 1. Call microservice
    var remoteResponse = await _srRemote.GetWorkLogsAsync(long.Parse(id), ct);

    if (!remoteResponse.IsSuccessStatusCode || remoteResponse.Content is null)
        return NotFound();

    var remote = remoteResponse.Content;

    // 2. Collect all MediaFileIds that need URL resolution
    var fileIds = remote.LogEntries
        .Where(e => e.MediaFileId is not null)
        .Select(e => e.MediaFileId!)
        .Distinct()
        .ToList();

    // 3. Resolve signed URLs — degrade gracefully on failure
    var urlMap = new Dictionary<string, string>(StringComparer.Ordinal);
    if (fileIds.Count > 0)
    {
        try
        {
            // Use the FileStorage service found in Step 0d.
            // Example: var urls = await _fileStorage.GetBulkReadUrlsAsync(fileIds, ct);
            // Populate urlMap from the result.
            // Replace the line below with the actual call:
            var urls = await _fileStorage.GetBulkReadUrlsAsync(fileIds, ct);
            foreach (var kv in urls)
                urlMap[kv.Key] = kv.Value;
        }
        catch
        {
            // FileStorage failure must NOT propagate to 500 — log and continue with null URLs
            _logger.LogWarning(
                "FileStorage URL resolution failed for WorkLogs on SR {Id}. MediaUrls will be null.",
                id);
        }
    }

    // 4. Map to BFF response
    var response = new WorkLogsBffResponse
    {
        ServiceRequestId = remote.ServiceRequestId.ToString(),
        Title = remote.Title,
        CurrentPhase = remote.CurrentPhase is null ? null : new WorkPhaseBffDto
        {
            Number          = remote.CurrentPhase.Number,
            Title           = remote.CurrentPhase.Title,
            ProgressPercent = remote.CurrentPhase.ProgressPercent,
            Status          = remote.CurrentPhase.Status,
        },
        Phases = remote.Phases
            .Select(p => new WorkPhaseBffDto
            {
                Number          = p.Number,
                Title           = p.Title,
                ProgressPercent = p.ProgressPercent,
                Status          = p.Status,
            })
            .ToList(),
        LogEntries = remote.LogEntries
            .Select(e => new WorkLogEntryBffDto
            {
                Id        = e.Id.ToString(),
                Timestamp = e.Timestamp,
                Type      = e.Type,
                Content   = e.Content,
                MediaUrl  = e.MediaFileId is not null
                              ? urlMap.GetValueOrDefault(e.MediaFileId)
                              : null,
                Author    = e.Author,
            })
            .ToList(),
        ProviderActivity = remote.ProviderActivity is null
            ? new ProviderActivityBffDto()
            : new ProviderActivityBffDto
            {
                ProviderId  = remote.ProviderActivity.ProviderId,
                ProviderName = remote.ProviderActivity.ProviderName,
                AvatarUrl   = remote.ProviderActivity.AvatarUrl,
                Activities  = remote.ProviderActivity.Activities
                    .Select(a => new ProviderActivityEntryBffDto
                    {
                        Id          = a.Id,
                        Timestamp   = a.Timestamp,
                        Description = a.Description,
                        Icon        = a.Icon,
                    })
                    .ToList(),
            },
        JobHealth = remote.JobHealth is null
            ? new JobHealthBffDto()
            : new JobHealthBffDto
            {
                Score            = remote.JobHealth.Score,
                Label            = remote.JobHealth.Label,
                DaysRemaining    = remote.JobHealth.DaysRemaining,
                MilestoneReached = remote.JobHealth.MilestoneReached,
            },
    };

    // Use the existing response wrapper found in Step 0c, e.g.:
    return AizenOk(response);
}

/// <summary>
/// Adds a work-log entry. If a MediaFileId is provided, the BFF resolves
/// it to a signed URL and returns it in the response.
/// </summary>
[HttpPost("{id}/work-logs")]
[ProducesResponseType(typeof(WorkLogEntryAddedBffResponse), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> AddWorkLogEntry(
    [FromRoute] string id,
    [FromBody] AddWorkLogEntryBffRequest body,
    CancellationToken ct)
{
    // Validate (or rely on pipeline behaviour from Step 0g pattern)
    var remoteBody = new AddWorkLogEntryRemoteRequest
    {
        Type        = body.Type,
        Content     = body.Content,
        Author      = body.Author,
        AuthorId    = body.AuthorId,
        MediaFileId = body.MediaFileId,
    };

    var remoteResponse = await _srRemote.AddWorkLogEntryAsync(long.Parse(id), remoteBody, ct);

    if (!remoteResponse.IsSuccessStatusCode || remoteResponse.Content is null)
    {
        if (remoteResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();
        return BadRequest();
    }

    var entry = remoteResponse.Content;

    // Resolve media URL if entry has a file
    string? mediaUrl = null;
    if (entry.MediaFileId is not null)
    {
        try
        {
            mediaUrl = await _fileStorage.GetReadUrlAsync(entry.MediaFileId, ct);
        }
        catch
        {
            _logger.LogWarning(
                "FileStorage URL resolution failed for entry {EntryId} on SR {SrId}.",
                entry.Id, id);
        }
    }

    var result = new WorkLogEntryAddedBffResponse
    {
        Id        = entry.Id.ToString(),
        Timestamp = entry.Timestamp,
        Type      = entry.Type,
        Content   = entry.Content,
        MediaUrl  = mediaUrl,
        Author    = entry.Author,
    };

    return CreatedAtAction(nameof(GetWorkLogs), new { id }, result);
}

/// <summary>
/// Updates progress and status of a single work phase.
/// </summary>
[HttpPatch("{id}/work-logs/phases/{phaseNumber:int}")]
[ProducesResponseType(typeof(WorkPhaseBffDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateWorkPhase(
    [FromRoute] string id,
    [FromRoute] int phaseNumber,
    [FromBody] UpdateWorkPhaseBffRequest body,
    CancellationToken ct)
{
    var remoteBody = new UpdateWorkPhaseRemoteRequest
    {
        ProgressPercent = body.ProgressPercent,
        Status = body.Status,
    };

    var remoteResponse = await _srRemote.UpdateWorkPhaseAsync(
        long.Parse(id), phaseNumber, remoteBody, ct);

    if (!remoteResponse.IsSuccessStatusCode || remoteResponse.Content is null)
    {
        if (remoteResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();
        return BadRequest();
    }

    var phase = remoteResponse.Content;
    return AizenOk(new WorkPhaseBffDto
    {
        Number          = phase.Number,
        ProgressPercent = phase.ProgressPercent,
        Status          = phase.Status,
    });
}
```

> ⚠️ Replace `_fileStorage`, `_srRemote`, `_logger`, and `AizenOk` with the actual field names and wrappers found in Step 0c/0d. Do not leave any of those as-is without verifying against the existing controller.

---

## Step 6 — Aizen BFF Envelope Alignment

The frontend `httpClient` normalizer (`responseNormalizer.ts`) unwraps `body` from the Aizen envelope:

```
{ header: { isSuccess: true }, body: <WorkLogsBffResponse> }
```

Verify that the existing BFF response wrapper (e.g., `AizenOk()`) emits this envelope. If it does, no change needed. If this controller uses a different wrapper that does NOT emit the envelope, wrap manually:

```csharp
// Only if needed — check first:
return Ok(new { header = new { isSuccess = true }, body = response });
```

---

## Step 7 — Route Alignment Check

Confirm these BFF routes match what the frontend calls:

| Frontend call | BFF endpoint |
|---|---|
| `GET /service-requests/{id}/work-logs` | `GET /api/admin/service-requests/{id}/work-logs` |
| `POST /service-requests/{id}/work-logs` | `POST /api/admin/service-requests/{id}/work-logs` |
| `PATCH /service-requests/{id}/work-logs/phases/{n}` | `PATCH /api/admin/service-requests/{id}/work-logs/phases/{n}` |

The frontend `httpClient` base URL strips `/api/admin`. If the BFF has a global prefix of `/api/admin`, the controller route should be `service-requests/{id}/work-logs`.

---

## Step 8 — DI Registration

No new DI registration is needed if the Refit interface was already registered globally in Step 0. Verify:

```csharp
// In Program.cs or module registration:
// The following should already exist — do NOT add a duplicate:
services.AddRefitClient<IServiceRequestMicroserviceRemoteCall>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(config["ServiceRequest:BaseUrl"]));
```

If the FileStorage service is not yet injected in the ServiceRequests controller, add it to the constructor:

```csharp
private readonly IFileStorageService _fileStorage;   // use the exact interface found in Step 0d

public ServiceRequestsController(
    IServiceRequestMicroserviceRemoteCall srRemote,
    IFileStorageService fileStorage,             // add if missing
    ILogger<ServiceRequestsController> logger)
{
    _srRemote     = srRemote;
    _fileStorage  = fileStorage;
    _logger       = logger;
}
```

---

## Step 9 — Smoke Tests

```bash
# GET work-logs — in-progress request with seed data
GET /api/admin/service-requests/9001/work-logs
→ 200 OK
→ body.serviceRequestId == "9001"
→ body.phases is array with length >= 1
→ body.phases[*].status ∈ ["Upcoming", "In Progress", "Completed"]
→ body.logEntries is array (may be empty — that is valid)
→ body.logEntries[*].mediaUrl is string or null (never undefined)
→ body.jobHealth.score is number 0-100
→ body.providerActivity.providerName is string

# GET work-logs — non-existent ID
GET /api/admin/service-requests/99999/work-logs
→ 404 Not Found

# POST work-log entry — note
POST /api/admin/service-requests/9001/work-logs
Content-Type: application/json
{ "type": "note", "content": "Fuel filter replaced and torque verified.", "author": "M. Thorne" }
→ 201 Created
→ response.id is string
→ response.type == "note"
→ response.mediaUrl is null

# POST work-log entry — invalid type
POST /api/admin/service-requests/9001/work-logs
{ "type": "invalid_type", "content": "test", "author": "test" }
→ 400 Bad Request

# PATCH work phase
PATCH /api/admin/service-requests/9001/work-logs/phases/3
{ "progressPercent": 75, "status": "In Progress" }
→ 200 OK
→ body.progressPercent == 75
→ body.status == "In Progress"
```

---

## Step 10 — Final Checklist

- [ ] Step 0 reads completed before any file written
- [ ] Refit interface methods added WITHOUT duplicating existing methods
- [ ] All Remote DTOs match the microservice's `GetWorkLogsResult` field names exactly
- [ ] All BFF Response DTOs match the frontend TypeScript `WorkLogsDto` interface exactly
- [ ] `MediaFileId` → `MediaUrl` resolution is wrapped in `try/catch` — FileStorage failure → `null`, not 500
- [ ] `long.Parse(id)` used (microservice uses `long` IDs; BFF route uses `string`)
- [ ] `AizenOk` / response wrapper replaced with project's actual wrapper
- [ ] FileStorage service injected in controller constructor
- [ ] Logger injected for FileStorage warning
- [ ] Validators registered (assembly scan or explicit)
- [ ] BFF envelope `{ header, body }` confirmed — frontend unwraps `body`
- [ ] All 3 smoke tests pass
