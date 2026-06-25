# Service Request — BFF Implementation Prompt

**Target project:** `Aizen.AdminPanel.BFF` (NOT the microservice)
**Run this AFTER** `PROMPT_1_SERVICE_REQUEST_MICROSERVICE.md` is complete and the microservice is deployed.

---

## ⚠️ STEP 0 — Read the existing BFF codebase first (MANDATORY)

Before writing a single line of code, do the following reads:

1. **BFF project structure:** List all folders under `Aizen.AdminPanel.BFF/`. Understand module/controller layout.

2. **Existing controllers:** Read 2–3 existing BFF controllers. Understand:
   - Base class (e.g. `AizenController`, `ApiControllerBase`, or plain `ControllerBase`)
   - Response wrapper method (e.g. `AizenOk()`, `Ok()`, `ApiResponse.Success()`)
   - Route attribute style (`[Route("api/...")]` vs `[HttpGet("/api/...")]`)
   - Auth attribute placement (`[Authorize]` on class or method)

3. **Existing Refit interfaces:** Read 1–2 existing `I*RemoteCall.cs` files. Understand:
   - Namespace convention
   - Whether interfaces are registered per-module or globally in `Program.cs`
   - Base URL injection pattern (`RefitClient`, `AddRefitClient`, or custom `HttpClient` factory)

4. **Existing module structure:** Look for an existing `ServiceRequests/` or similar folder under `Modules/`. If one exists, read all its files before adding anything — do not duplicate.

5. **Shared infrastructure:** Find and read:
   - How `FileStorage` / document URLs are resolved (look for file URL resolution, signed URL generation, `IFileStorageService`, `IDocumentService`)
   - How responses are paginated — the existing `PagedResponse<T>` or equivalent
   - Whether `ICurrentUserService` or similar is injected into controllers

6. **Existing DI registrations:** Read `Program.cs` (or module registrations) to understand how Refit clients and module services are registered. Follow this exact pattern for new registrations.

Only after completing these reads, proceed to Step 1.

---

## Step 1 — Refit Interface `IServiceRequestMicroserviceRemoteCall`

Create the Refit interface in the appropriate location (follow naming/folder convention from Step 0).

```csharp
using Refit;
using Aizen.AdminPanel.BFF.Modules.ServiceRequests.Dtos; // adjust namespace

public interface IServiceRequestMicroserviceRemoteCall
{
    // ── Service Request ──────────────────────────────────────────────────────
    [Get("/api/service-requests")]
    Task<ApiResponse<PagedResponse<ServiceRequestListItemDto>>> GetListAsync(
        [Query] GetServiceRequestListRequest request,
        CancellationToken ct = default);

    [Get("/api/service-requests/{id}")]
    Task<ApiResponse<ServiceRequestDetailDto>> GetDetailAsync(
        string id,
        CancellationToken ct = default);

    [Patch("/api/service-requests/{id}/status")]
    Task<ApiResponse<ServiceRequestDetailDto>> UpdateStatusAsync(
        string id,
        [Body] UpdateStatusRequest body,
        CancellationToken ct = default);

    [Post("/api/service-requests/{id}/assign")]
    Task<ApiResponse<ServiceRequestDetailDto>> AssignProviderAsync(
        string id,
        [Body] AssignProviderRequest body,
        CancellationToken ct = default);

    [Get("/api/service-requests/{id}/timeline")]
    Task<ApiResponse<ServiceRequestTimelineDto>> GetTimelineAsync(
        string id,
        CancellationToken ct = default);

    [Post("/api/service-requests/{id}/complete")]
    Task<ApiResponse<object>> CompleteAsync(
        string id,
        CancellationToken ct = default);

    [Post("/api/service-requests/{id}/dispute")]
    Task<ApiResponse<object>> DisputeAsync(
        string id,
        [Body] DisputeRequest body,
        CancellationToken ct = default);

    // ── Offers ───────────────────────────────────────────────────────────────
    [Get("/api/service-requests/{id}/offers")]
    Task<ApiResponse<ProviderOffersDto>> GetOffersAsync(
        string id,
        CancellationToken ct = default);

    [Post("/api/service-requests/{id}/offers/{offerId}/accept")]
    Task<ApiResponse<object>> AcceptOfferAsync(
        string id,
        string offerId,
        CancellationToken ct = default);

    [Post("/api/service-requests/{id}/offers/{offerId}/reject")]
    Task<ApiResponse<object>> RejectOfferAsync(
        string id,
        string offerId,
        CancellationToken ct = default);

    // ── Work Logs ─────────────────────────────────────────────────────────────
    [Get("/api/service-requests/{id}/work-logs")]
    Task<ApiResponse<WorkLogsDto>> GetWorkLogsAsync(
        string id,
        CancellationToken ct = default);

    [Post("/api/service-requests/{id}/work-logs")]
    Task<ApiResponse<WorkLogEntryDto>> AddWorkLogEntryAsync(
        string id,
        [Body] AddWorkLogEntryRequest body,
        CancellationToken ct = default);

    // ── Messages ─────────────────────────────────────────────────────────────
    [Get("/api/messages/conversations")]
    Task<ApiResponse<IReadOnlyList<ConversationSummaryDto>>> GetConversationsAsync(
        [Query] string? filter = null,
        CancellationToken ct = default);

    [Get("/api/messages/conversations/{id}")]
    Task<ApiResponse<ConversationDetailDto>> GetConversationDetailAsync(
        string id,
        CancellationToken ct = default);

    // ── Payment ───────────────────────────────────────────────────────────────
    [Post("/api/service-requests/{id}/payment/release")]
    Task<ApiResponse<object>> ReleasePaymentAsync(
        string id,
        CancellationToken ct = default);
}
```

Register with DI following the existing Refit registration pattern found in Step 0.

---

## Step 2 — DTOs

Mirror the microservice response shapes exactly. Create in `Modules/ServiceRequests/Dtos/` (or the folder convention found in Step 0).

All DTOs are **read-only records** (no setters). Property names are camelCase at serialization level.

```csharp
// ── List item ────────────────────────────────────────────────────────────────
public sealed record ServiceRequestListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Priority { get; init; }
    public string? Category { get; init; }
    public string? VesselId { get; init; }
    public string? VesselName { get; init; }
    public string? RequestedById { get; init; }
    public string? AssignedToId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

// ── Detail ───────────────────────────────────────────────────────────────────
public sealed record ServiceRequestDetailDto : ServiceRequestListItemDto
{
    public string? Description { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public ParticipantDto? RequestedBy { get; init; }
    public ParticipantDto? AssignedTo { get; init; }
    public IReadOnlyList<AttachmentDto> Attachments { get; init; } = [];
    public IReadOnlyList<TimelineEventDto> Timeline { get; init; } = [];
}

public sealed record ParticipantDto
{
    public string Id { get; init; } = string.Empty;
    public string? Email { get; init; }
}

public sealed record AttachmentDto
{
    public string Id { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? Name { get; init; }
    public string Type { get; init; } = "document";
}

// ── Timeline ─────────────────────────────────────────────────────────────────
public sealed record ServiceRequestTimelineDto
{
    public string RequestId { get; init; } = string.Empty;
    public IReadOnlyList<TimelineEventDto> Events { get; init; } = [];
}

public sealed record TimelineEventDto
{
    public string Id { get; init; } = string.Empty;
    public string Event { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? By { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset At { get; init; }
}

// ── Offers ───────────────────────────────────────────────────────────────────
public sealed record ProviderOffersDto
{
    public string ServiceRequestId { get; init; } = string.Empty;
    public string ServiceRequestTitle { get; init; } = string.Empty;
    public IReadOnlyList<ProviderOfferItemDto> Offers { get; init; } = [];
    public IReadOnlyList<AgreementEventDto> AgreementTimeline { get; init; } = [];
    public int TotalOffers { get; init; }
}

public sealed record ProviderOfferItemDto
{
    public string Id { get; init; } = string.Empty;
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public double Rating { get; init; }
    public int ReviewCount { get; init; }
    public decimal QuoteAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string EstimatedDuration { get; init; } = string.Empty;
    public string? ProposalFileId { get; init; }
    public string? ProposalUrl { get; init; }  // BFF resolves this from FileStorage
    public string Status { get; init; } = "Pending";
    public DateTimeOffset SubmittedAt { get; init; }
}

public sealed record AgreementEventDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? Actor { get; init; }
    public string Icon { get; init; } = "event";
}

// ── Work Logs ────────────────────────────────────────────────────────────────
public sealed record WorkLogsDto
{
    public string ServiceRequestId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public WorkPhaseDto CurrentPhase { get; init; } = default!;
    public IReadOnlyList<WorkPhaseDto> Phases { get; init; } = [];
    public IReadOnlyList<WorkLogEntryDto> LogEntries { get; init; } = [];
    public ProviderActivityDto ProviderActivity { get; init; } = default!;
    public JobHealthDto JobHealth { get; init; } = default!;
}

public sealed record WorkPhaseDto
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public string Status { get; init; } = "Upcoming";  // "Upcoming" | "In Progress" | "Completed"
}

public sealed record WorkLogEntryDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = "note";  // note | photo | status_change
    public string Content { get; init; } = string.Empty;
    public string? MediaFileId { get; init; }  // raw file ID from microservice
    public string? MediaUrl { get; init; }     // BFF resolves to signed URL
    public string Author { get; init; } = string.Empty;
}

public sealed record ProviderActivityDto
{
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<ProviderActivityEntryDto> Activities { get; init; } = [];
}

public sealed record ProviderActivityEntryDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "check_circle";
}

public sealed record JobHealthDto
{
    public int Score { get; init; }
    public string Label { get; init; } = string.Empty;
    public int DaysRemaining { get; init; }
    public string MilestoneReached { get; init; } = string.Empty;
}

// ── Messages ─────────────────────────────────────────────────────────────────
public sealed record ConversationSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public int UnreadCount { get; init; }
    public string Status { get; init; } = string.Empty;  // active | pending | flagged
    public string ServiceRequestId { get; init; } = string.Empty;
}

public sealed record ConversationDetailDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ServiceRequestId { get; init; } = string.Empty;
    public IReadOnlyList<ConversationParticipantDto> Participants { get; init; } = [];
    public IReadOnlyList<ChatMessageDto> Messages { get; init; } = [];
}

public sealed record ConversationParticipantDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;  // Owner | Provider | Admin
}

public sealed record ChatMessageDto
{
    public string Id { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public string SenderRole { get; init; } = string.Empty;  // Owner | Provider | Admin
    public string Content { get; init; } = string.Empty;
    public bool IsInternalNote { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyList<MessageAttachmentBffDto> Attachments { get; init; } = [];
}

public sealed record MessageAttachmentBffDto
{
    public string Url { get; init; } = string.Empty;  // BFF resolves from FileStorage
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "document";   // image | document
}
```

---

## Step 3 — BFF Controllers

Create 4 controllers. Follow the **exact** base class, attribute, and response wrapper style found in Step 0.

### 3a. `AdminServiceRequestsController`

Route prefix: `/api/admin/service-requests`

```csharp
[Authorize]
[ApiController]
[Route("api/admin/service-requests")]
public class AdminServiceRequestsController : /* base class from Step 0 */
{
    private readonly IServiceRequestMicroserviceRemoteCall _srRemote;

    // GET /api/admin/service-requests
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetServiceRequestListRequest request, CancellationToken ct)
    {
        var result = await _srRemote.GetListAsync(request, ct);
        return /* wrap using existing pattern */;
    }

    // GET /api/admin/service-requests/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(string id, CancellationToken ct)
    {
        var result = await _srRemote.GetDetailAsync(id, ct);
        return /* wrap */;
    }

    // PATCH /api/admin/service-requests/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var result = await _srRemote.UpdateStatusAsync(id, request, ct);
        return /* wrap */;
    }

    // POST /api/admin/service-requests/{id}/assign
    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignProvider(string id, [FromBody] AssignProviderRequest request, CancellationToken ct)
    {
        var result = await _srRemote.AssignProviderAsync(id, request, ct);
        return /* wrap */;
    }

    // GET /api/admin/service-requests/{id}/timeline
    [HttpGet("{id}/timeline")]
    public async Task<IActionResult> GetTimeline(string id, CancellationToken ct)
    {
        var result = await _srRemote.GetTimelineAsync(id, ct);
        return /* wrap */;
    }

    // POST /api/admin/service-requests/{id}/complete
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        var result = await _srRemote.CompleteAsync(id, ct);
        return /* wrap */;
    }

    // POST /api/admin/service-requests/{id}/dispute
    [HttpPost("{id}/dispute")]
    public async Task<IActionResult> Dispute(string id, [FromBody] DisputeRequest request, CancellationToken ct)
    {
        var result = await _srRemote.DisputeAsync(id, request, ct);
        return /* wrap */;
    }
}
```

### 3b. `AdminOffersController`

Route prefix: `/api/admin/service-requests/{serviceRequestId}/offers`

```csharp
[HttpGet("")]
// → _srRemote.GetOffersAsync(serviceRequestId)
// ENRICHMENT: for each offer where ProposalFileId != null,
//   call FileStorage to resolve signed URL → populate ProposalUrl
//   if FileStorage fails, set ProposalUrl = null (degrade gracefully, do NOT throw)

[HttpPost("{offerId}/accept")]
// → _srRemote.AcceptOfferAsync(serviceRequestId, offerId)

[HttpPost("{offerId}/reject")]
// → _srRemote.RejectOfferAsync(serviceRequestId, offerId)
```

### 3c. `AdminWorkLogsController`

Route prefix: `/api/admin/service-requests/{serviceRequestId}/work-logs`

```csharp
[HttpGet("")]
// → _srRemote.GetWorkLogsAsync(serviceRequestId)
// ENRICHMENT: for each WorkLogEntry where MediaFileId != null,
//   call FileStorage → resolve MediaUrl
//   if FileStorage fails, set MediaUrl = null (degrade gracefully)

[HttpPost("")]
// → _srRemote.AddWorkLogEntryAsync(serviceRequestId, request)
// After success, return the new WorkLogEntryDto with MediaUrl enriched if applicable
```

### 3d. `AdminMessagesController`

Route prefix: `/api/admin/messages`

```csharp
[HttpGet("conversations")]
// → _srRemote.GetConversationsAsync(filter)

[HttpGet("conversations/{id}")]
// → _srRemote.GetConversationDetailAsync(id)
// ENRICHMENT: for each attachment where FileId != null,
//   call FileStorage → resolve Url
//   if FileStorage fails, set Url = "" or null (degrade gracefully)

[HttpPost("conversations/{id}/payment/release")]
// → _srRemote.ReleasePaymentAsync(serviceRequestId extracted from conversation)
```

---

## Step 4 — Request Models

```csharp
public sealed record GetServiceRequestListRequest
{
    [FromQuery(Name = "search")]  public string? Search { get; init; }
    [FromQuery(Name = "status")]  public string? Status { get; init; }
    [FromQuery(Name = "priority")] public string? Priority { get; init; }
    [FromQuery(Name = "category")] public string? Category { get; init; }
    [FromQuery(Name = "vesselId")] public string? VesselId { get; init; }
    [FromQuery(Name = "page")]    public int Page { get; init; } = 0;
    [FromQuery(Name = "size")]    public int Size { get; init; } = 20;
}

public sealed record UpdateStatusRequest
{
    public string Status { get; init; } = string.Empty;
    // Allowed: pending | assigned | in_progress | completed | cancelled | disputed
}

public sealed record AssignProviderRequest
{
    public string ProviderId { get; init; } = string.Empty;
    public string? ProviderName { get; init; }
}

public sealed record DisputeRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed record AddWorkLogEntryRequest
{
    public string Type { get; init; } = "note";       // note | photo | status_change
    public string Content { get; init; } = string.Empty;
    public string? MediaFileId { get; init; }
    public string Author { get; init; } = string.Empty;
}
```

---

## Step 5 — FluentValidation (BFF layer)

Add lightweight validators for BFF request models. These are in addition to the deep validation in the microservice.

```csharp
// UpdateStatusRequestValidator
RuleFor(x => x.Status)
    .NotEmpty()
    .Must(s => new[] { "pending", "assigned", "in_progress", "completed", "cancelled", "disputed" }.Contains(s))
    .WithMessage("Status must be one of: pending, assigned, in_progress, completed, cancelled, disputed");

// DisputeRequestValidator
RuleFor(x => x.Reason)
    .NotEmpty()
    .MaximumLength(500);

// AddWorkLogEntryRequestValidator
RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
RuleFor(x => x.Type)
    .Must(t => new[] { "note", "photo", "status_change" }.Contains(t))
    .WithMessage("Type must be: note | photo | status_change");
RuleFor(x => x.Author).NotEmpty();
```

---

## Step 6 — JSON Serialization

Confirm the BFF uses the same serialization settings as all other modules. Typical setting:

```csharp
services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

If this is already set globally, do NOT add a duplicate. Only add if this module is serialized separately.

---

## Step 7 — DI Registration

Follow the exact existing module registration pattern found in Step 0. A typical extension method looks like:

```csharp
public static IServiceCollection AddServiceRequestModule(this IServiceCollection services, IConfiguration config)
{
    services
        .AddRefitClient<IServiceRequestMicroserviceRemoteCall>()
        .ConfigureHttpClient(c =>
        {
            c.BaseAddress = new Uri(config["ServiceRequest:BaseUrl"]
                ?? throw new InvalidOperationException("ServiceRequest:BaseUrl is required"));
        });

    return services;
}
```

Then in `Program.cs`:
```csharp
builder.Services.AddServiceRequestModule(builder.Configuration);
```

Add to `appsettings.json`:
```json
"ServiceRequest": {
  "BaseUrl": "http://localhost:5XXX"
}
```

---

## Step 8 — Frontend BFF URL Alignment

The frontend expects these exact base paths. Verify the BFF routes match:

| Frontend API call path | BFF controller route |
|------------------------|----------------------|
| `/api/admin/service-requests` | `GET /api/admin/service-requests` |
| `/api/admin/service-requests/{id}` | `GET /api/admin/service-requests/{id}` |
| `/api/admin/service-requests/{id}/offers` | `GET /api/admin/service-requests/{id}/offers` |
| `/api/admin/service-requests/{id}/offers/{offerId}/accept` | `POST` |
| `/api/admin/service-requests/{id}/offers/{offerId}/reject` | `POST` |
| `/api/admin/service-requests/{id}/work-logs` | `GET /POST` |
| `/api/admin/service-requests/{id}/timeline` | `GET` |
| `/api/admin/service-requests/{id}/complete` | `POST` |
| `/api/admin/service-requests/{id}/dispute` | `POST` |
| `/api/admin/service-requests/{id}/payment/release` | `POST` |
| `/api/admin/messages/conversations` | `GET` |
| `/api/admin/messages/conversations/{id}` | `GET` |

If the BFF already has a global `/api/admin` prefix, adjust controller routes accordingly.

---

## Step 9 — Smoke Tests

```
# Service Requests
GET /api/admin/service-requests
→ 200, body.items is array, body.total is integer

GET /api/admin/service-requests/{id}
→ 200, body.title is string, body.status is snake_case

# Offers
GET /api/admin/service-requests/{id}/offers
→ 200, body.offers is array, offer.status is "Pending"|"Accepted"|"Rejected"
→ offer.proposalUrl is null (not undefined) when no FileStorage URL

# Work Logs
GET /api/admin/service-requests/{id}/work-logs
→ 200, body.phases.length >= 0, body.logEntries is array
→ entry.mediaUrl is null when no FileStorage URL (not 500)

# Messages
GET /api/admin/messages/conversations
→ 200, result is array, item.unreadCount is integer

GET /api/admin/messages/conversations/{id}
→ 200, body.messages is array, message.senderRole is PascalCase
```

---

## Step 10 — Final Checklist

- [ ] Step 0 reads completed before any file was modified
- [ ] `IServiceRequestMicroserviceRemoteCall` registered in DI
- [ ] Base URL read from `appsettings.json` (not hardcoded)
- [ ] `MediaUrl` / `ProposalUrl` / attachment `Url` degrade to `null` when FileStorage fails — no 500
- [ ] BFF does NOT call microservice directly for SignalR — SignalR is microservice-side only
- [ ] All 4 controllers use the correct BFF base class and response wrapper
- [ ] Validators registered and validated before microservice call
- [ ] `camelCase` JSON confirmed
- [ ] Smoke tests pass against the running microservice
