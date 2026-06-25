# Service Request Microservice — Implementation Prompt

**Target project:** `Aizen.ServiceRequest` (the .NET microservice, NOT the BFF)
**Run this in:** The `Aizen.ServiceRequest` solution/folder in Claude Code.

---

## ⚠️ STEP 0 — Read the existing codebase first (MANDATORY)

Before writing a single line of code, do the following reads:

1. **Project structure:** List all folders and files under `Aizen.ServiceRequest/`. Understand the module/layer breakdown (Domain, Application, Infrastructure, Api or similar).

2. **Existing entities:** Read every `*Entity.cs` file under the Domain layer. Note which fields already exist on `ServiceRequestEntity` (or equivalent). Do NOT duplicate fields.

3. **Existing CQRS pattern:** Read 2–3 existing `*Query.cs`, `*QueryHandler.cs`, `*Command.cs`, `*CommandHandler.cs` files to understand:
   - Naming convention (e.g. `GetServiceRequestByIdQuery` vs `ServiceRequestDetailQuery`)
   - Return type wrappers (e.g. `Result<T>`, `AizenResponse<T>`, plain `T`)
   - How `IRequest<T>` is declared
   - Whether `CancellationToken` is passed through

4. **Existing controllers:** Read the existing `ServiceRequestsController` (or equivalent). Understand:
   - Base class and attribute style
   - How responses are wrapped (e.g. `Ok(result)`, `AizenOk(result)`, custom `ApiResponse`)
   - Route prefix (e.g. `/api/service-requests` vs `/api/v1/service-requests`)

5. **Existing DbContext + EF configuration:** Read the `DbContext` class and 1–2 existing `EntityConfiguration` files. Understand:
   - Table naming convention (snake_case? PascalCase?)
   - How `Id` columns are configured (IDENTITY, SEQUENCE, Guid?)
   - Whether `BaseEntity` exists and what fields it brings

6. **⚠️ SignalR — Critical inspection:** Search for:
   - Classes inheriting from `Hub` or `Hub<T>`
   - Files named `*Hub.cs`, `*HubClient.cs`
   - `AddSignalR()` and `MapHub<>` registrations in `Program.cs`
   - Existing hub method names and client method names (strings sent via `SendAsync`)
   - Group naming convention (e.g. `$"sr-{id}"` vs `$"service-request:{id}"`)
   - How connections authenticate (JWT bearer? Keycloak token in query string?)
   - Document everything found. You will EXTEND the hub, never replace or rename existing members.

7. **Existing FluentValidation setup:** Read 1–2 validators to understand the registration pattern.

8. **Migration history:** Check the latest migration file name so your new migration follows the naming sequence.

Only after completing these reads, proceed to Step 1.

---

## Step 1 — Domain Entities

Based on what you read in Step 0, add or extend the following entities. **If a field already exists, skip it.**

### 1a. Extend `ServiceRequestEntity` — add missing fields

```csharp
// Add only fields that are MISSING from the existing entity:
public string? Category { get; private set; }
public string? VesselName { get; private set; }        // denormalized for list queries
public string? RequestedByEmail { get; private set; }  // denormalized
public string? AssignedProviderName { get; private set; } // denormalized
public DateTimeOffset? DisputedAt { get; private set; }
public string? DisputeReason { get; private set; }

// Navigation collections — add only missing ones:
public ICollection<ProviderOfferEntity> Offers { get; private set; } = [];
public ICollection<WorkLogEntryEntity> WorkLogEntries { get; private set; } = [];
public ICollection<WorkPhaseEntity> WorkPhases { get; private set; } = [];
public ICollection<ServiceRequestConversationEntity> Conversations { get; private set; } = [];
```

Add domain methods for the new status transitions if they don't exist:
```csharp
public void MarkCompleted(DateTimeOffset completedAt)
public void MarkDisputed(string reason, DateTimeOffset disputedAt)
public void Assign(long providerId, string providerName)
public void ReleasePayment()
```

### 1b. `ProviderOfferEntity` (create if missing)

```csharp
public class ProviderOfferEntity : BaseEntity  // use the base class found in Step 0
{
    public long Id { get; private set; }
    public long ServiceRequestId { get; private set; }
    public long ProviderId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public double Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public decimal QuoteAmount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string EstimatedDuration { get; private set; } = string.Empty;
    public string? ProposalFileId { get; private set; }
    public string? ProposalUrl { get; private set; }   // signed read URL — set by BFF, not stored
    public string Status { get; private set; } = "Pending";
    // Pending | Accepted | Rejected | Expired
    public DateTimeOffset SubmittedAt { get; private set; }

    // Domain methods
    public void Accept() { Status = "Accepted"; }
    public void Reject() { Status = "Rejected"; }
}
```

### 1c. `WorkLogEntryEntity` (create if missing)

```csharp
public class WorkLogEntryEntity : BaseEntity
{
    public long Id { get; private set; }
    public long ServiceRequestId { get; private set; }
    public string Type { get; private set; } = "note";
    // note | photo | status_change
    public string Content { get; private set; } = string.Empty;
    public string? MediaFileId { get; private set; }   // FileStorage file ID
    public string Author { get; private set; } = string.Empty;
    public long? AuthorId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
}
```

### 1d. `WorkPhaseEntity` (create if missing)

```csharp
public class WorkPhaseEntity : BaseEntity
{
    public long Id { get; private set; }
    public long ServiceRequestId { get; private set; }
    public int PhaseNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int ProgressPercent { get; private set; }   // 0-100
    public string Status { get; private set; } = "Upcoming";
    // Upcoming | In Progress | Completed
    public int DisplayOrder { get; private set; }

    public void UpdateProgress(int percent, string status)
    {
        ProgressPercent = Math.Clamp(percent, 0, 100);
        Status = status;
    }
}
```

### 1e. `ServiceRequestConversationEntity` (create if missing)

```csharp
public class ServiceRequestConversationEntity : BaseEntity
{
    public long Id { get; private set; }
    public long ServiceRequestId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Status { get; private set; } = "active";
    // active | pending | flagged
    public int UnreadCount { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }
    public ICollection<ConversationMessageEntity> Messages { get; private set; } = [];
}
```

### 1f. `ConversationMessageEntity` (create if missing)

```csharp
public class ConversationMessageEntity : BaseEntity
{
    public long Id { get; private set; }
    public long ConversationId { get; private set; }
    public string SenderId { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public string SenderRole { get; private set; } = "Owner";
    // Admin | Provider | Owner
    public string Content { get; private set; } = string.Empty;
    public bool IsInternalNote { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public ICollection<MessageAttachmentEntity> Attachments { get; private set; } = [];
}
```

### 1g. `MessageAttachmentEntity` (create if missing)

```csharp
public class MessageAttachmentEntity : BaseEntity
{
    public long Id { get; private set; }
    public long MessageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = "document";  // image | document
    public string? FileId { get; private set; }
    public string? Url { get; private set; }  // signed read URL — set by BFF
}
```

---

## Step 2 — EF Core Configurations

For each new entity, add a configuration class. Follow the exact style of existing configurations found in Step 0.

Table naming: follow the existing convention (likely snake_case). Suggested names:
- `provider_offers`
- `work_log_entries`
- `work_phases`
- `sr_conversations`
- `conversation_messages`
- `message_attachments`

Add `HasIndex(e => e.ServiceRequestId)` on all child tables for list query performance.

After adding configs, create and apply the migration:
```
dotnet ef migrations add ServiceRequest_WorkLogs_Offers_Conversations
dotnet ef database update
```

---

## Step 3 — Application Layer: Queries

Follow the naming and return-type pattern found in Step 0 exactly.

### 3a. Extend `GetServiceRequestListQuery` / handler

Add filter parameters if missing:
```csharp
public string? Search { get; init; }
public string? Status { get; init; }
public string? Priority { get; init; }
public string? Category { get; init; }
public long? VesselId { get; init; }
public int Page { get; init; } = 0;
public int Size { get; init; } = 20;
```

Response DTO `ServiceRequestListItemDto`:
```csharp
public sealed record ServiceRequestListItemDto
{
    public string Id { get; init; } = string.Empty;        // long serialized as string
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;    // lowercase: in_progress
    public string? Priority { get; init; }                 // lowercase: urgent
    public string? Category { get; init; }
    public string? VesselId { get; init; }
    public string? VesselName { get; init; }
    public string? RequestedById { get; init; }
    public string? AssignedToId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
```

Wrap result in a paged response matching the existing paged wrapper in the project.

### 3b. Extend `GetServiceRequestDetailQuery` / handler

Extend the existing detail DTO with fields required by the frontend:

```csharp
// Additional fields on top of list item:
public string? Description { get; init; }
public string? Location { get; init; }
public DateTimeOffset? ScheduledAt { get; init; }
public DateTimeOffset? CompletedAt { get; init; }
public ParticipantDto? RequestedBy { get; init; }   // { id, email }
public ParticipantDto? AssignedTo { get; init; }    // { id, email }
public IReadOnlyList<AttachmentDto> Attachments { get; init; } = [];
public IReadOnlyList<TimelineEventDto> Timeline { get; init; } = [];
```

### 3c. `GetServiceRequestTimelineQuery` (create if missing)

```csharp
// Returns:
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
```

### 3d. `GetProviderOffersQuery` (create)

```csharp
// Returns:
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
    public string? ProposalUrl { get; init; }  // null here — BFF enriches this
    public string Status { get; init; } = "Pending";   // PascalCase: Accepted | Rejected | Pending | Expired
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
```

`AgreementTimeline` is derived from `ServiceRequestTimelineEventEntity` rows that have financial relevance (e.g. event type = `OfferAccepted`, `ContractSigned`, `PaymentReleased`).

### 3e. `GetWorkLogsQuery` (create)

```csharp
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
    public string Status { get; init; } = "Upcoming";
    // Title Case with space: "In Progress" | "Completed" | "Upcoming"
}

public sealed record WorkLogEntryDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    public string? MediaUrl { get; init; }   // null here — BFF enriches via FileStorage
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
```

Handler logic for `JobHealth`:
- `Score` = budget utilization % = `(currentSpend / budgetLimit) * 100`
- `DaysRemaining` = days until scheduled completion
- `MilestoneReached` = total man-hours from all `WorkLogEntry` records summed (as string, e.g. `"142.5"`)

### 3f. `GetConversationListQuery` (create)

```csharp
public sealed record GetConversationListQuery : IRequest<IReadOnlyList<ConversationSummaryDto>>
{
    public string? Filter { get; init; }  // ALL | PENDING | FLAGGED
}

public sealed record ConversationSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public int UnreadCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ServiceRequestId { get; init; } = string.Empty;
}
```

### 3g. `GetConversationDetailQuery` (create)

```csharp
public sealed record ConversationDetailDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ServiceRequestId { get; init; } = string.Empty;
    public IReadOnlyList<ParticipantDto> Participants { get; init; } = [];
    public IReadOnlyList<ChatMessageDto> Messages { get; init; } = [];
}

public sealed record ChatMessageDto
{
    public string Id { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public string SenderRole { get; init; } = string.Empty;  // PascalCase: Owner | Provider | Admin
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyList<MessageAttachmentDto> Attachments { get; init; } = [];
}

public sealed record MessageAttachmentDto
{
    public string Url { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "document";  // image | document
}
```

---

## Step 4 — Application Layer: Commands

### 4a. `AddWorkLogEntryCommand` (create)

```csharp
public sealed record AddWorkLogEntryCommand : IRequest<WorkLogEntryDto>
{
    [Required] public string ServiceRequestId { get; init; } = string.Empty;
    [Required] public string Type { get; init; } = "note";
    [Required] public string Content { get; init; } = string.Empty;
    public string? MediaFileId { get; init; }
    [Required] public string Author { get; init; } = string.Empty;
    public long? AuthorId { get; init; }
}
```

Handler must:
1. Validate (Content not empty, Type in allowed values)
2. Persist `WorkLogEntryEntity`
3. **Push SignalR event** `WorkLogEntryAdded` to the service request group (see Step 5)
4. Return the new `WorkLogEntryDto`

### 4b. `ApproveProviderOfferCommand` / `RejectProviderOfferCommand` (create or verify)

Handler for Approve must:
1. Set offer status to `Accepted`
2. Set all other offers to `Rejected`
3. Assign the provider to the service request
4. Append timeline event `OfferAccepted`
5. Push SignalR `OfferAccepted` event
6. Push SignalR `StatusChanged` event if status changes to `assigned`

### 4c. `CompleteServiceRequestCommand` (verify/extend)

Handler must:
1. Set status to `completed`, set `CompletedAt`
2. Append timeline event `ServiceCompleted`
3. Push SignalR `StatusChanged` to group

### 4d. `DisputeServiceRequestCommand` (verify/extend)

Handler must:
1. Set status to `disputed`, set `DisputedAt`, set `DisputeReason`
2. Append timeline event `DisputeFiled`
3. Push SignalR `DisputeFiled` to group

### 4e. `ReleasePaymentCommand` (create)

Handler must:
1. Append timeline event `PaymentReleased`
2. Push SignalR `PaymentReleased` to group
3. Optionally: publish a domain event to trigger payout in Payment module

---

## Step 5 — SignalR Hub Extension

**USE WHAT YOU FOUND IN STEP 0.** Do not create a new hub if one already exists.

Add only these **new** items to the existing hub:

```csharp
// New client event name constants (use a static class or string constants):
public static class SrHubEvents
{
    public const string WorkLogEntryAdded = "WorkLogEntryAdded";
    public const string StatusChanged     = "StatusChanged";
    public const string OfferAccepted     = "OfferAccepted";
    public const string DisputeFiled      = "DisputeFiled";
    public const string PaymentReleased   = "PaymentReleased";
    public const string MessageReceived   = "MessageReceived";
}
```

Add group join/leave methods if missing (use the group naming convention found in Step 0):
```csharp
public async Task JoinServiceRequestGroup(string serviceRequestId)
    => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(serviceRequestId));

public async Task LeaveServiceRequestGroup(string serviceRequestId)
    => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(serviceRequestId));

public async Task JoinConversationGroup(string conversationId)
    => await Groups.AddToGroupAsync(Context.ConnectionId, ConvGroupName(conversationId));

// Use whatever group naming was already there, e.g.:
private static string GroupName(string srId) => $"sr:{srId}";
private static string ConvGroupName(string convId) => $"conv:{convId}";
```

In command handlers, inject `IHubContext<YourExistingHubType>` and push:
```csharp
await _hubContext.Clients
    .Group($"sr:{command.ServiceRequestId}")
    .SendAsync(SrHubEvents.WorkLogEntryAdded, newEntryDto, cancellationToken);
```

---

## Step 6 — FluentValidation

Add validators following the existing pattern. Key rules:

| Command | Rules |
|---------|-------|
| `UpdateStatusCommand` | Status ∈ {pending, assigned, in_progress, completed, cancelled, disputed} |
| `AssignProviderCommand` | ProviderId not empty |
| `DisputeCommand` | Reason not empty, max 500 chars |
| `AddWorkLogEntryCommand` | Content not empty, max 2000 chars; Type ∈ {note, photo, status_change} |

---

## Step 7 — Controller / Route Verification

Verify or add these routes. Follow the **exact** controller style found in Step 0 (base class, response wrapper, route prefix):

| Method | Path | Handler |
|--------|------|---------|
| GET | `/api/service-requests` | `GetServiceRequestListQuery` |
| GET | `/api/service-requests/{id}` | `GetServiceRequestDetailQuery` |
| PATCH | `/api/service-requests/{id}/status` | `UpdateStatusCommand` |
| POST | `/api/service-requests/{id}/assign` | `AssignProviderCommand` |
| GET | `/api/service-requests/{id}/timeline` | `GetTimelineQuery` |
| POST | `/api/service-requests/{id}/complete` | `CompleteCommand` |
| POST | `/api/service-requests/{id}/dispute` | `DisputeCommand` |
| GET | `/api/service-requests/{id}/offers` | `GetProviderOffersQuery` |
| POST | `/api/service-requests/{id}/offers/{offerId}/accept` | `ApproveOfferCommand` |
| POST | `/api/service-requests/{id}/offers/{offerId}/reject` | `RejectOfferCommand` |
| GET | `/api/service-requests/{id}/work-logs` | `GetWorkLogsQuery` |
| POST | `/api/service-requests/{id}/work-logs` | `AddWorkLogEntryCommand` |
| GET | `/api/messages/conversations` | `GetConversationListQuery` |
| GET | `/api/messages/conversations/{id}` | `GetConversationDetailQuery` |
| POST | `/api/service-requests/{id}/payment/release` | `ReleasePaymentCommand` |

---

## Step 8 — JSON Serialization

Confirm camelCase serialization. Follow existing project setting — do not override if already set.

**Value casing contract** (must match exactly — frontend reads these strings):
- `status` field: lowercase with underscore → `pending`, `in_progress`, `completed`, `disputed`
- `priority` field: lowercase → `low`, `medium`, `high`, `urgent`
- `offer.status` field: PascalCase → `Pending`, `Accepted`, `Rejected`, `Expired`
- `senderRole` field: PascalCase → `Owner`, `Provider`, `Admin`
- `workPhase.status` field: Title Case with space → `Upcoming`, `In Progress`, `Completed`
- All ID fields: serialize as **string** (not integer) — frontend TypeScript uses `string` for all IDs

---

## Step 9 — Smoke Tests

Write integration tests for:
- List returns `items` array and `total` integer
- Detail returns `title`, `status`, `requestedBy.email`, `attachments` array
- Offers returns `offers` array with `providerName`, `quoteAmount`, `status` = PascalCase
- Work logs returns `phases` array (5 items), `logEntries` array, `jobHealth.score` integer
- Conversations list returns array at `body` root
- Conversation detail returns `messages` with `senderRole` values
- Adding a work log entry publishes `WorkLogEntryAdded` to SignalR group (verify with mock hub context)

---

## Step 10 — Final Checklist

- [ ] Step 0 reads completed before any file was modified
- [ ] All new entities have EF configurations and migration applied
- [ ] Existing SignalR hub not modified — only extended
- [ ] `WorkLogEntryAdded` fires on POST /work-logs
- [ ] `StatusChanged` fires on complete, dispute, assign
- [ ] All IDs serialize as strings
- [ ] `offer.status` is PascalCase, `sr.status` is snake_case
- [ ] No FluentValidation errors on valid inputs
- [ ] Smoke tests pass with `dotnet test`
