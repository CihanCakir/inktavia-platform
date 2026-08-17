# PROMPT A — Messaging Module: Domain Layer
# `Aizen.Modules.Messaging.Domain`

## Context & Architecture Rules

You are implementing the **Domain layer** of `Aizen.Modules.Messaging` — a standalone, context-agnostic messaging module for Inktavia Marine OS.

**Confirmed patterns from ServiceRequest module — follow exactly:**
- All entity IDs are `long` (via `AizenEntityWithAudit` base class — NOT `Guid`)
- Base class: `AizenEntityWithAudit` provides `Id (long)`, `IsActive (bool)`, `IsDeleted (bool)`, `CreatedAt`, `UpdatedAt`
- Domain methods use `private set` / factory `Create(...)` static methods
- Private backing collections: `private readonly List<T> _items = new(); public IReadOnlyCollection<T> Items => _items.AsReadOnly();`
- `[DocumentationInfo("...", "...")]` attribute on every class
- Namespace: `Aizen.Modules.Messaging.Domain.*`
- Do NOT use `Guid` for IDs anywhere in Domain entities

---

## Overview

The Messaging module is the **platform-wide communication backbone** for Inktavia. A `ConversationEntity` is the root aggregate. It is context-aware via `ContextType` + `ContextId`, allowing any module (ServiceRequest, Commerce, CargoDry, Direct) to own a conversation without coupling to Messaging internals.

---

## Step 1 — Enums (`Aizen.Modules.Messaging.Abstraction/Enum/`)

> These go in the **Abstraction** project, not Domain. Domain references Abstraction.

### `MessagingContextType.cs`
```csharp
namespace Aizen.Modules.Messaging.Abstraction.Enum;

/// <summary>Identifies which domain context owns this conversation.</summary>
public enum MessagingContextType
{
    ServiceRequest  = 1,   // Owner ↔ Provider ↔ Admin, scoped to an SR
    CommerceOrder   = 2,   // Buyer ↔ Seller, scoped to an order
    CargoDrySupport = 3,   // Kit owner ↔ CargoDry support team
    VenueInquiry    = 4,   // Guest ↔ Marina/Venue operator
    DirectMessage   = 5,   // Platform-level DM between two users
}
```

### `MessagingParticipantRole.cs`
```csharp
namespace Aizen.Modules.Messaging.Abstraction.Enum;

public enum MessagingParticipantRole
{
    Owner    = 1,
    Provider = 2,
    Admin    = 3,
    System   = 4,
    Support  = 5,
    Buyer    = 6,
    Seller   = 7,
}
```

### `ConversationStatus.cs`
```csharp
namespace Aizen.Modules.Messaging.Abstraction.Enum;

public enum ConversationStatus
{
    Active   = 1,
    Archived = 2,
    Flagged  = 3,  // Under moderation review
    Closed   = 4,
}
```

### `MessageModerationStatus.cs`
```csharp
namespace Aizen.Modules.Messaging.Abstraction.Enum;

/// <summary>Content moderation verdict for a message.</summary>
public enum MessageModerationStatus
{
    Allowed        = 1,   // Default — message passes all checks
    PendingReview  = 2,   // Queued for manual review
    Flagged        = 3,   // Soft-blocked, visible but marked
    Blocked        = 4,   // Hard-blocked, hidden from recipients
    AutoApproved   = 5,   // Passed automated content filters
}
```

### `MessageType.cs`
```csharp
namespace Aizen.Modules.Messaging.Abstraction.Enum;

public enum MessageType
{
    Text               = 1,
    SystemNotification = 2,
    StatusChange       = 3,
    InternalNote       = 4,   // Admin-only, not visible to participants
    MediaAttachment    = 5,
}
```

### `MessagingRealtimeEventType.cs`
```csharp
namespace Aizen.Modules.Messaging.Abstraction.Enum;

public enum MessagingRealtimeEventType
{
    MessageSent               = 1,
    MessageModerated          = 2,
    ConversationCreated       = 3,
    ConversationStatusChanged = 4,
    ParticipantJoined         = 5,
    ParticipantLeft           = 6,
    UnreadCountUpdated        = 7,
}
```

---

## Step 2 — Domain Entities (`Aizen.Modules.Messaging.Domain/Entities/`)

### 2.1 `ConversationEntity.cs`
Path: `Entities/Conversation/ConversationEntity.cs`

```csharp
using Aizen.Core.Domain;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Conversation entity",
    "Root aggregate for a messaging thread. Context-agnostic: owned by any domain via ContextType+ContextId.")]
public sealed class ConversationEntity : AizenEntityWithAudit
{
    // ── Context binding ─────────────────────────────────────────────
    public MessagingContextType ContextType  { get; private set; }
    public long ContextId                    { get; private set; }   // SR ID, Order ID, Kit ID, etc.

    // ── Metadata ────────────────────────────────────────────────────
    public string Title                      { get; private set; } = string.Empty;
    public ConversationStatus Status         { get; private set; } = ConversationStatus.Active;
    public int UnreadCountByAdmin            { get; private set; }
    public DateTimeOffset LastMessageAt      { get; private set; }
    public string LastMessagePreview         { get; private set; } = string.Empty;

    // ── Navigation ──────────────────────────────────────────────────
    private readonly List<ConversationParticipantEntity> _participants = new();
    public IReadOnlyCollection<ConversationParticipantEntity> Participants => _participants.AsReadOnly();

    private readonly List<ConversationMessageEntity> _messages = new();
    public IReadOnlyCollection<ConversationMessageEntity> Messages => _messages.AsReadOnly();

    public ConversationEntity() { }

    public static ConversationEntity Create(
        MessagingContextType contextType,
        long contextId,
        string title)
    {
        return new ConversationEntity
        {
            ContextType        = contextType,
            ContextId          = contextId,
            Title              = title.Trim(),
            Status             = ConversationStatus.Active,
            UnreadCountByAdmin = 0,
            LastMessageAt      = DateTimeOffset.UtcNow,
            LastMessagePreview = string.Empty,
            IsActive           = true,
        };
    }

    public void AddParticipant(ConversationParticipantEntity participant)
        => _participants.Add(participant);

    public void AddMessage(ConversationMessageEntity message)
    {
        _messages.Add(message);
        LastMessageAt      = message.SentAt;
        LastMessagePreview = message.Content.Length > 120
            ? message.Content[..120] + "…"
            : message.Content;
        if (!message.IsInternalNote)
            UnreadCountByAdmin++;
    }

    public void MarkReadByAdmin()
        => UnreadCountByAdmin = 0;

    public void SetStatus(ConversationStatus status)
        => Status = status;

    public void Flag()
        => Status = ConversationStatus.Flagged;

    public void Archive()
        => Status = ConversationStatus.Archived;
}
```

---

### 2.2 `ConversationParticipantEntity.cs`
Path: `Entities/Conversation/ConversationParticipantEntity.cs`

```csharp
using Aizen.Core.Domain;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Conversation participant entity",
    "A user who is a member of a conversation thread. DisplayName is captured at join time.")]
public sealed class ConversationParticipantEntity : AizenEntityWithAudit
{
    public long ConversationId          { get; private set; }
    public long UserId                  { get; private set; }
    public string DisplayName           { get; private set; } = string.Empty;
    public MessagingParticipantRole Role { get; private set; }
    public DateTimeOffset JoinedAt      { get; private set; }
    public bool IsActive                { get; private set; } = true;

    public ConversationParticipantEntity() { }

    public static ConversationParticipantEntity Create(
        long conversationId,
        long userId,
        string displayName,
        MessagingParticipantRole role)
    {
        return new ConversationParticipantEntity
        {
            ConversationId = conversationId,
            UserId         = userId,
            DisplayName    = displayName.Trim(),
            Role           = role,
            JoinedAt       = DateTimeOffset.UtcNow,
            IsActive       = true,
        };
    }

    public void Leave() => IsActive = false;
}
```

---

### 2.3 `ConversationMessageEntity.cs`
Path: `Entities/Conversation/ConversationMessageEntity.cs`

```csharp
using Aizen.Core.Domain;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Conversation message entity",
    "A single message in a conversation. Carries moderation status and optional attachment.")]
public sealed class ConversationMessageEntity : AizenEntityWithAudit
{
    public long ConversationId                       { get; private set; }
    public long SenderUserId                         { get; private set; }
    public string SenderName                         { get; private set; } = string.Empty;
    public MessagingParticipantRole SenderRole       { get; private set; }
    public string Content                            { get; private set; } = string.Empty;
    public MessageType Type                          { get; private set; } = MessageType.Text;
    public bool IsInternalNote                       { get; private set; }
    public MessageModerationStatus ModerationStatus  { get; private set; } = MessageModerationStatus.Allowed;
    public string? ModerationReason                  { get; private set; }
    public DateTimeOffset SentAt                     { get; private set; }

    private readonly List<MessageAttachmentEntity> _attachments = new();
    public IReadOnlyCollection<MessageAttachmentEntity> Attachments => _attachments.AsReadOnly();

    public ConversationMessageEntity() { }

    public static ConversationMessageEntity Create(
        long conversationId,
        long senderUserId,
        string senderName,
        MessagingParticipantRole senderRole,
        string content,
        MessageType type = MessageType.Text,
        bool isInternalNote = false)
    {
        return new ConversationMessageEntity
        {
            ConversationId  = conversationId,
            SenderUserId    = senderUserId,
            SenderName      = senderName.Trim(),
            SenderRole      = senderRole,
            Content         = content.Trim(),
            Type            = type,
            IsInternalNote  = isInternalNote,
            ModerationStatus = MessageModerationStatus.Allowed,
            SentAt          = DateTimeOffset.UtcNow,
            IsActive        = true,
        };
    }

    public void AddAttachment(MessageAttachmentEntity attachment)
        => _attachments.Add(attachment);

    public void SetModerationStatus(MessageModerationStatus status, string? reason = null)
    {
        ModerationStatus = status;
        ModerationReason = reason;
    }

    public void Flag(string reason)
        => SetModerationStatus(MessageModerationStatus.Flagged, reason);

    public void Block(string reason)
        => SetModerationStatus(MessageModerationStatus.Blocked, reason);
}
```

---

### 2.4 `MessageAttachmentEntity.cs`
Path: `Entities/Conversation/MessageAttachmentEntity.cs`

```csharp
using Aizen.Core.Domain;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Message attachment entity",
    "A file attached to a conversation message. FileId resolves to a URL via FileStorage module.")]
public sealed class MessageAttachmentEntity : AizenEntityWithAudit
{
    public long MessageId        { get; private set; }
    public string FileName       { get; private set; } = string.Empty;
    public string FileType       { get; private set; } = "document";   // "image" | "document" | "video"
    public string? FileStorageId { get; private set; }                  // Resolved via FileStorage module

    public MessageAttachmentEntity() { }

    public static MessageAttachmentEntity Create(
        long messageId,
        string fileName,
        string fileType,
        string? fileStorageId = null)
    {
        return new MessageAttachmentEntity
        {
            MessageId     = messageId,
            FileName      = fileName.Trim(),
            FileType      = fileType.ToLower(),
            FileStorageId = fileStorageId,
            IsActive      = true,
        };
    }
}
```

---

## Step 3 — Repository Interfaces (`Aizen.Modules.Messaging.Domain/Interface/Repository/`)

### `IConversationRepository.cs`

```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;

namespace Aizen.Modules.Messaging.Domain.Interface.Repository;

[DocumentationInfo("Conversation repository interface", "Data access contract for the Conversation aggregate.")]
public interface IConversationRepository
{
    // ── Queries ─────────────────────────────────────────────────────
    Task<IReadOnlyList<ConversationEntity>> GetListAsync(
        ConversationStatus? status,
        MessagingContextType? contextType,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<ConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<ConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default);

    Task<ConversationEntity?> GetByContextAsync(
        MessagingContextType contextType,
        long contextId,
        CancellationToken ct = default);

    Task<int> CountAsync(ConversationStatus? status, MessagingContextType? contextType, CancellationToken ct = default);

    // ── Commands ────────────────────────────────────────────────────
    Task AddAsync(ConversationEntity entity, CancellationToken ct = default);
    void Update(ConversationEntity entity);
}
```

### `IConversationMessageRepository.cs`

```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;

namespace Aizen.Modules.Messaging.Domain.Interface.Repository;

[DocumentationInfo("Conversation message repository interface", "Data access for messages within conversations.")]
public interface IConversationMessageRepository
{
    Task<IReadOnlyList<ConversationMessageEntity>> GetByConversationIdAsync(
        long conversationId,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<ConversationMessageEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Returns flagged messages for moderation queue.</summary>
    Task<IReadOnlyList<ConversationMessageEntity>> GetFlaggedAsync(
        int skip,
        int take,
        CancellationToken ct = default);

    Task AddAsync(ConversationMessageEntity entity, CancellationToken ct = default);
    void Update(ConversationMessageEntity entity);
}
```

---

## Step 4 — Domain Interface: Content Policy

Create a domain service interface that enforces content moderation rules. This lives in Domain (not Application) because the rule about what is "allowed" is a domain concern.

### `IMessageContentPolicy.cs`
Path: `Aizen.Modules.Messaging.Domain/Interface/IMessageContentPolicy.cs`

```csharp
namespace Aizen.Modules.Messaging.Domain.Interface;

[DocumentationInfo("Message content policy interface",
    "Domain contract for content moderation. Detects prohibited content, off-platform solicitation, channel abuse.")]
public interface IMessageContentPolicy
{
    /// <summary>
    /// Evaluates content against all active policy rules.
    /// Returns (isAllowed, reason). If isAllowed=false, reason explains the violation.
    /// </summary>
    Task<ContentPolicyResult> EvaluateAsync(string content, long senderUserId, CancellationToken ct = default);
}

public sealed record ContentPolicyResult(
    bool IsAllowed,
    bool RequiresReview,
    string? ViolationReason,
    string? PolicyCode   // e.g. "OFF_PLATFORM_PAYMENT", "PROHIBITED_CONTENT", "SPAM_DETECTED"
)
{
    public static ContentPolicyResult Allow() => new(true, false, null, null);
    public static ContentPolicyResult Review(string reason) => new(true, true, reason, "REVIEW_REQUIRED");
    public static ContentPolicyResult Block(string reason, string code) => new(false, false, reason, code);
}
```

---

## Quality Gates

- [ ] All IDs are `long` — no `Guid` in entity properties
- [ ] All entities have `[DocumentationInfo]` attribute
- [ ] All entities have parameterless constructor `public EntityName() { }` for EF
- [ ] All properties use `private set`
- [ ] Factory methods are `public static EntityName Create(...)`
- [ ] Private backing list fields for navigation collections
- [ ] No business logic dependencies — Domain is pure
- [ ] `IMessageContentPolicy` interface defined, NOT implemented (implementation is in Application)
