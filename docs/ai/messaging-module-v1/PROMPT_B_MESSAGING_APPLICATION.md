# PROMPT B — Messaging Module: Application Layer
# `Aizen.Modules.Messaging.Application`

## Context & Architecture Rules

You are implementing the **Application layer** of `Aizen.Modules.Messaging`. This layer contains CQRS commands, queries, handlers, validators, the realtime publisher, and the content moderation service implementation.

**Confirmed patterns from ServiceRequest module — follow exactly:**
- Command base: `AizenCommand<TResponse>` with readonly constructor-set properties
- Handler base: `AizenCommandHandler<TCommand, TResponse>` / `AizenQueryHandler<TQuery, TResponse>`
- Query base: `AizenQuery<TResponse>`
- Current user: `IAizenInfoAccessor` → `_info.UserInfoAccessor.UserInfo.UserId` (returns `long`)
- Realtime: `IRealtimePublisher.PublishToGroupAsync(groupName, dto, ct)` / `PublishToUserAsync(userId, dto, ct)`
- `[DocumentationInfo("...", "...")]` on every class
- Each Command or Query has its own subfolder: `Command/SendMessage/SendMessageCommand.cs` + `SendMessageCommandHandler.cs`
- Namespace: `Aizen.Modules.Messaging.Application.*`

---

## Step 1 — Content Moderation Service (Application/Services/)

This is the implementation of `IMessageContentPolicy` from the Domain layer.

### `MessageContentPolicyService.cs`
Path: `Application/Services/MessageContentPolicyService.cs`

**Scenario rules to implement:**

| Scenario | Detection | Action |
|----------|-----------|--------|
| Off-platform payment solicitation | Keywords: "iban", "swift", "whatsapp", "telegram", "direct payment", "outside platform", "banka hesabı", "hesap no" | `Block` with code `OFF_PLATFORM_SOLICITATION` |
| Personal contact info leakage | Regex: phone numbers `(\+?\d[\d\s\-]{8,})`, email patterns `\w+@\w+\.\w+` | `Review` — flag for human review |
| Prohibited commercial content | Keywords: "competitor", platform competitor names | `Block` with code `PROHIBITED_COMMERCIAL` |
| Spam / excessive repetition | Same content sent >3x in 5 minutes by same user (checked via cache) | `Block` with code `SPAM_DETECTED` |
| Excessive link sharing | More than 2 URLs in a single message (`http://` or `https://`) | `Review` |
| Clean message | No violations | `Allow` |

```csharp
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Messaging.Domain.Interface;
using System.Text.RegularExpressions;

namespace Aizen.Modules.Messaging.Application.Services;

[DocumentationInfo("Message content policy service",
    "Enforces platform content rules: blocks off-platform solicitation, personal data leakage, spam, and prohibited content.")]
public sealed class MessageContentPolicyService : IMessageContentPolicy
{
    private readonly IAizenCache _cache;

    // Off-platform solicitation keywords (case-insensitive)
    private static readonly string[] OffPlatformKeywords =
    [
        "iban", "swift", "bic", "banka hesabı", "hesap no", "hesap numarası",
        "whatsapp", "telegram", "signal", "direct payment", "outside platform",
        "platform dışı", "direkt öde", "nakit öde", "cash payment"
    ];

    // Prohibited external commercial
    private static readonly string[] ProhibitedCommercialKeywords =
    [
        // Add platform-specific competitor names here
    ];

    private static readonly Regex PhoneRegex =
        new(@"(\+?\d[\d\s\-\(\)]{8,}\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmailRegex =
        new(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);

    private static readonly Regex UrlRegex =
        new(@"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public MessageContentPolicyService(IAizenCache cache) => _cache = cache;

    public async Task<ContentPolicyResult> EvaluateAsync(
        string content, long senderUserId, CancellationToken ct = default)
    {
        var normalized = content.ToLowerInvariant();

        // 1. Off-platform solicitation — hard block
        foreach (var keyword in OffPlatformKeywords)
        {
            if (normalized.Contains(keyword))
                return ContentPolicyResult.Block(
                    $"Message contains off-platform solicitation keyword: '{keyword}'",
                    "OFF_PLATFORM_SOLICITATION");
        }

        // 2. Prohibited commercial
        foreach (var keyword in ProhibitedCommercialKeywords)
        {
            if (normalized.Contains(keyword))
                return ContentPolicyResult.Block(
                    $"Message contains prohibited commercial content.",
                    "PROHIBITED_COMMERCIAL");
        }

        // 3. Phone number leakage — soft flag, human review
        if (PhoneRegex.IsMatch(content))
            return ContentPolicyResult.Review("Message may contain a phone number.");

        // 4. Email address leakage — soft flag
        if (EmailRegex.IsMatch(content))
            return ContentPolicyResult.Review("Message may contain an email address.");

        // 5. Excessive URL sharing
        var urlCount = UrlRegex.Matches(content).Count;
        if (urlCount > 2)
            return ContentPolicyResult.Review($"Message contains {urlCount} URLs.");

        // 6. Spam detection via cache — same content within 5 min window
        var spamKey  = $"msg:spam:{senderUserId}:{content.GetHashCode()}";
        var existing = await _cache.GetAsync<int?>(spamKey, ct);
        var count    = (existing ?? 0) + 1;
        await _cache.SetAsync(spamKey, count, TimeSpan.FromMinutes(5), ct);
        if (count > 3)
            return ContentPolicyResult.Block("Repeated identical message detected.", "SPAM_DETECTED");

        return ContentPolicyResult.Allow();
    }
}
```

---

## Step 2 — Realtime Publisher (Application/Realtime/)

### `MessagingRealtimePublisher.cs`
Path: `Application/Realtime/MessagingRealtimePublisher.cs`

```csharp
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Modules.Messaging.Abstraction.Dto;
using Aizen.Modules.Messaging.Abstraction.Enum;
using System.Text.Json;

namespace Aizen.Modules.Messaging.Application.Realtime;

[DocumentationInfo("Messaging realtime publisher",
    "Publishes typed messaging events to conversation groups and user channels via SignalR.")]
public sealed class MessagingRealtimePublisher
{
    private readonly IRealtimePublisher _publisher;
    public MessagingRealtimePublisher(IRealtimePublisher publisher) => _publisher = publisher;

    /// <summary>Broadcast a new message to all conversation participants.</summary>
    public async Task PublishMessageSentAsync(
        long conversationId,
        long contextId,
        MessagingContextType contextType,
        object messagePayload,
        IEnumerable<long> participantUserIds,
        CancellationToken ct = default)
    {
        var dto = new MessagingRealtimeEventDto
        {
            ConversationId = conversationId,
            ContextId      = contextId,
            ContextType    = contextType,
            EventType      = MessagingRealtimeEventType.MessageSent,
            PayloadJson    = JsonSerializer.Serialize(messagePayload),
            OccurredAt     = DateTimeOffset.UtcNow,
        };

        // Broadcast to conversation group (all connected subscribers)
        await _publisher.PublishToGroupAsync($"messaging:conv:{conversationId}", dto, ct: ct);

        // Also push to each participant's personal user channel (for notification badges)
        foreach (var userId in participantUserIds)
            await _publisher.PublishToUserAsync(userId.ToString(), dto, ct: ct);
    }

    /// <summary>Notify admin operations channel of a moderation event.</summary>
    public async Task PublishModerationEventAsync(
        long conversationId,
        long messageId,
        string policyCode,
        string reason,
        CancellationToken ct = default)
    {
        var dto = new MessagingRealtimeEventDto
        {
            ConversationId = conversationId,
            EventType      = MessagingRealtimeEventType.MessageModerated,
            PayloadJson    = JsonSerializer.Serialize(new { messageId, policyCode, reason }),
            OccurredAt     = DateTimeOffset.UtcNow,
        };

        await _publisher.PublishToGroupAsync("admin:messaging-moderation", dto, ct: ct);
    }

    /// <summary>Notify participants of conversation status change (flagged, archived, etc.).</summary>
    public async Task PublishConversationStatusChangedAsync(
        long conversationId,
        string newStatus,
        CancellationToken ct = default)
    {
        var dto = new MessagingRealtimeEventDto
        {
            ConversationId = conversationId,
            EventType      = MessagingRealtimeEventType.ConversationStatusChanged,
            PayloadJson    = JsonSerializer.Serialize(new { newStatus }),
            OccurredAt     = DateTimeOffset.UtcNow,
        };

        await _publisher.PublishToGroupAsync($"messaging:conv:{conversationId}", dto, ct: ct);
    }
}
```

---

## Step 3 — Abstraction DTOs (`Aizen.Modules.Messaging.Abstraction/Dto/`)

### `MessagingRealtimeEventDto.cs`
```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Dto;

[DocumentationInfo("Messaging realtime event DTO",
    "Payload published via SignalR for all messaging events.")]
public sealed class MessagingRealtimeEventDto
{
    public long ConversationId              { get; set; }
    public long ContextId                   { get; set; }
    public MessagingContextType ContextType { get; set; }
    public MessagingRealtimeEventType EventType { get; set; }
    public string? PayloadJson              { get; set; }
    public DateTimeOffset OccurredAt        { get; set; }
}
```

---

## Step 4 — CQRS Commands

### 4.1 `CreateConversationCommand`
Path: `Command/CreateConversation/CreateConversationCommand.cs` + `Handler.cs`

```csharp
// Command
public sealed class CreateConversationCommand : AizenCommand<CreateConversationResponse>
{
    public MessagingContextType ContextType     { get; }
    public long ContextId                        { get; }
    public string Title                          { get; }
    public List<ConversationParticipantInput> Participants { get; }

    public CreateConversationCommand(
        MessagingContextType contextType, long contextId,
        string title, List<ConversationParticipantInput> participants)
    {
        ContextType  = contextType;
        ContextId    = contextId;
        Title        = title;
        Participants = participants;
    }
}

public sealed record ConversationParticipantInput(
    long UserId, string DisplayName, MessagingParticipantRole Role);
```

**Handler logic:**
1. Check `GetByContextAsync(ContextType, ContextId)` — if conversation already exists, return its ID (idempotent)
2. Create `ConversationEntity.Create(ContextType, ContextId, Title)`
3. Add all participants via `ConversationParticipantEntity.Create(...)`
4. `await _conversationRepository.AddAsync(entity, ct)`
5. Publish `ConversationCreated` realtime event
6. Return `new CreateConversationResponse(entity.Id.ToString())`

---

### 4.2 `SendMessageCommand`
Path: `Command/SendMessage/SendMessageCommand.cs` + `Handler.cs` + `Validator.cs`

```csharp
// Command
public sealed class SendMessageCommand : AizenCommand<SendMessageResponse>
{
    public long ConversationId              { get; }
    public string Content                   { get; }
    public MessageType Type                 { get; }
    public bool IsInternalNote              { get; }
    public string? AttachmentFileStorageId  { get; }
    public string? AttachmentFileName       { get; }
    public string? AttachmentFileType       { get; }

    public SendMessageCommand(
        long conversationId, string content, MessageType type,
        bool isInternalNote, string? attachmentFileStorageId,
        string? attachmentFileName, string? attachmentFileType)
    { /* assign all */ }
}
```

**Handler logic (critical — content moderation integrated):**
```csharp
public override async Task<SendMessageResponse?> Handle(SendMessageCommand request, CancellationToken ct)
{
    var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct)
        ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

    if (conversation.Status == ConversationStatus.Closed)
        throw new InvalidOperationException("Cannot send message to a closed conversation.");

    var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
    var participant   = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId)
        ?? throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");

    // ── Content moderation check ──────────────────────────────────
    var policyResult = await _contentPolicy.EvaluateAsync(request.Content, currentUserId, ct);

    if (!policyResult.IsAllowed)
    {
        // Hard block: log, notify admin, return error
        await _realtimePublisher.PublishModerationEventAsync(
            conversation.Id, 0, policyResult.PolicyCode!, policyResult.ViolationReason!, ct);
        throw new InvalidOperationException(
            $"Message blocked by content policy: {policyResult.ViolationReason}");
    }

    var message = ConversationMessageEntity.Create(
        conversation.Id, currentUserId, participant.DisplayName,
        participant.Role, request.Content, request.Type, request.IsInternalNote);

    // Set moderation status before persisting
    if (policyResult.RequiresReview)
        message.Flag(policyResult.ViolationReason!);

    if (!string.IsNullOrEmpty(request.AttachmentFileStorageId))
    {
        var attachment = MessageAttachmentEntity.Create(
            0, // MessageId set after save via EF
            request.AttachmentFileName ?? "attachment",
            request.AttachmentFileType ?? "document",
            request.AttachmentFileStorageId);
        message.AddAttachment(attachment);
    }

    await _messageRepository.AddAsync(message, ct);
    conversation.AddMessage(message);
    _conversationRepository.Update(conversation);

    // Notify moderation queue if flagged
    if (policyResult.RequiresReview)
        await _realtimePublisher.PublishModerationEventAsync(
            conversation.Id, message.Id, "REVIEW_REQUIRED", policyResult.ViolationReason!, ct);

    // Broadcast to conversation group
    var participantIds = conversation.Participants.Select(p => p.UserId);
    await _realtimePublisher.PublishMessageSentAsync(
        conversation.Id, conversation.ContextId, conversation.ContextType,
        message.ToDto(), participantIds, ct);

    return new SendMessageResponse(message.ToDto());
}
```

**Validator (`SendMessageCommandValidator`):**
```csharp
public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid message type.");
    }
}
```

---

### 4.3 `ModerateMessageCommand`
Path: `Command/ModerateMessage/`

```csharp
public sealed class ModerateMessageCommand : AizenCommand<bool>
{
    public long MessageId                { get; }
    public MessageModerationStatus Status { get; }
    public string? Reason                { get; }

    public ModerateMessageCommand(long messageId, MessageModerationStatus status, string? reason)
    { MessageId = messageId; Status = status; Reason = reason; }
}
```

**Handler logic:**
1. Load message via `_messageRepository.GetByIdAsync(request.MessageId, ct)`
2. Call `message.SetModerationStatus(request.Status, request.Reason)`
3. `_messageRepository.Update(message)`
4. If `Status == Blocked`: publish `MessageModerated` event to conversation group
5. Return `true`

---

### 4.4 `FlagConversationCommand`
Path: `Command/FlagConversation/`

```csharp
public sealed class FlagConversationCommand : AizenCommand<bool>
{
    public long ConversationId { get; }
    public string Reason       { get; }
    public FlagConversationCommand(long conversationId, string reason) { ... }
}
```

**Handler logic:**
1. Load conversation, call `conversation.Flag()`
2. Update repository
3. Publish `ConversationStatusChanged` event
4. Return `true`

---

### 4.5 `MarkConversationReadCommand`
Path: `Command/MarkConversationRead/`

```csharp
public sealed class MarkConversationReadCommand : AizenCommand<bool>
{
    public long ConversationId { get; }
    public MarkConversationReadCommand(long conversationId) { ConversationId = conversationId; }
}
```

**Handler logic:**
1. Load conversation
2. `conversation.MarkReadByAdmin()`
3. Update
4. Publish `UnreadCountUpdated` with `unreadCount = 0`

---

## Step 5 — CQRS Queries

### 5.1 `GetConversationListQuery`
Path: `Query/GetConversationList/`

**Query:**
```csharp
public sealed class GetConversationListQuery : AizenQuery<GetConversationListResponse>
{
    public ConversationStatus? Status          { get; }
    public MessagingContextType? ContextType   { get; }
    public int Skip                            { get; }
    public int Take                            { get; }

    public GetConversationListQuery(ConversationStatus? status, MessagingContextType? contextType, int skip, int take)
    { Status = status; ContextType = contextType; Skip = skip; Take = take; }
}
```

**Handler logic:**
1. `GetListAsync(Status, ContextType, Skip, Take, ct)`
2. Map to `ConversationSummaryDto` list
3. `CountAsync(...)` for total
4. Return `new GetConversationListResponse(items, total)`

---

### 5.2 `GetConversationDetailQuery`
Path: `Query/GetConversationDetail/`

**Handler logic:**
1. `GetByIdWithMessagesAsync(ConversationId, ct)` — throw if null
2. Filter out `Blocked` moderation messages (not visible to non-admin callers unless caller IsAdmin)
3. Filter out `IsInternalNote` messages if caller is not Admin
4. Resolve attachment URLs via `IFileStorageService.GetReadUrlAsync(FileStorageId)` — graceful null on failure
5. Map and return

---

### 5.3 `GetConversationByContextQuery`
Path: `Query/GetConversationByContext/`

```csharp
public sealed class GetConversationByContextQuery : AizenQuery<GetConversationDetailResponse>
{
    public MessagingContextType ContextType { get; }
    public long ContextId                  { get; }
    public GetConversationByContextQuery(MessagingContextType contextType, long contextId) { ... }
}
```

Used by other modules (e.g., ServiceRequest) to open or retrieve a conversation by their own domain ID. Handler calls `GetByContextAsync(...)`.

---

### 5.4 `GetModerationQueueQuery`
Path: `Query/GetModerationQueue/`

Returns flagged and pending-review messages for admin moderation panel.

```csharp
public sealed class GetModerationQueueQuery : AizenQuery<GetModerationQueueResponse>
{
    public int Skip { get; }
    public int Take { get; }
    public GetModerationQueueQuery(int skip, int take) { Skip = skip; Take = take; }
}
```

Handler calls `_messageRepository.GetFlaggedAsync(Skip, Take, ct)`.

---

## Step 6 — Abstraction Response/Request Types

### Responses (`Abstraction/Response/`)
```csharp
// GetConversationListResponse
public sealed class GetConversationListResponse(List<ConversationSummaryDto> items, int total)
{
    public List<ConversationSummaryDto> Items { get; } = items;
    public int Total { get; } = total;
}

public sealed record ConversationSummaryDto
{
    public string Id                { get; init; } = string.Empty;
    public string Title             { get; init; } = string.Empty;
    public string ContextType       { get; init; } = string.Empty;
    public string ContextId         { get; init; } = string.Empty;
    public string Preview           { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public int UnreadCount          { get; init; }
    public string Status            { get; init; } = string.Empty;
}

// GetConversationDetailResponse
public sealed class GetConversationDetailResponse(ConversationDetailDto conversation)
{
    public ConversationDetailDto Conversation { get; } = conversation;
}

public sealed record ConversationDetailDto
{
    public string Id                { get; init; } = string.Empty;
    public string Title             { get; init; } = string.Empty;
    public string ContextType       { get; init; } = string.Empty;
    public string ContextId         { get; init; } = string.Empty;
    public string Status            { get; init; } = string.Empty;
    public List<ParticipantDto> Participants { get; init; } = [];
    public List<ChatMessageDto> Messages    { get; init; } = [];
}

public sealed record ParticipantDto(string UserId, string DisplayName, string Role);

public sealed record ChatMessageDto
{
    public string Id                        { get; init; } = string.Empty;
    public string SenderUserId              { get; init; } = string.Empty;
    public string SenderName                { get; init; } = string.Empty;
    public string SenderRole                { get; init; } = string.Empty;
    public string Content                   { get; init; } = string.Empty;
    public string Type                      { get; init; } = string.Empty;
    public bool IsInternalNote              { get; init; }
    public string ModerationStatus          { get; init; } = string.Empty;
    public DateTimeOffset Timestamp         { get; init; }
    public List<AttachmentDto> Attachments  { get; init; } = [];
}

public sealed record AttachmentDto(string Url, string FileName, string FileType);

// CreateConversationResponse
public sealed class CreateConversationResponse(string conversationId)
{
    public string ConversationId { get; } = conversationId;
}

// SendMessageResponse
public sealed class SendMessageResponse(ChatMessageDto message)
{
    public ChatMessageDto Message { get; } = message;
}
```

### Requests (`Abstraction/Request/`)
```csharp
// SendMessageRequest (inbound from controllers/BFF)
public sealed record SendMessageRequest(
    string Content,
    MessageType Type = MessageType.Text,
    bool IsInternalNote = false,
    string? AttachmentFileStorageId = null,
    string? AttachmentFileName = null,
    string? AttachmentFileType = null
);

// CreateConversationRequest
public sealed record CreateConversationRequest(
    MessagingContextType ContextType,
    long ContextId,
    string Title,
    List<ConversationParticipantInput> Participants
);
```

---

## Quality Gates

- [ ] `MessageContentPolicyService` registered in DI as `IMessageContentPolicy`
- [ ] Off-platform keyword list is configurable via `IConfiguration` (not hardcoded — read from config/db)
- [ ] Blocked messages: exception thrown, message NOT persisted
- [ ] Flagged messages: message IS persisted with `ModerationStatus = Flagged`, admin notified via realtime
- [ ] `GetConversationDetailQuery` filters out `Blocked` messages for non-admin
- [ ] `GetConversationDetailQuery` filters out `IsInternalNote` for non-admin
- [ ] `CreateConversationCommand` is idempotent (returns existing ID if context already has conversation)
- [ ] All validators registered; empty content, content > 4000 chars fail
- [ ] Attachment URL resolution: graceful null on FileStorage failure — do NOT throw
