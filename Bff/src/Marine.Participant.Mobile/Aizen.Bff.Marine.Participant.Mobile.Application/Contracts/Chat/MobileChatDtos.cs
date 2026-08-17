namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;

// BE_MO10a — owner chat (read + text write). Reads from the unified Messaging store (participant-scoped); text writes
// go through the SR module with SenderType=Owner (the anti-harassment gate is provider-only — the owner always opens
// the channel). COST-FREE: message text + sender + timestamps only — no economics. System messages are carried
// through so the FE renders lifecycle pills. Enums cross as string NAMES (the AdminPanel numeric-enum gotcha).

/// <summary>One inbox row — a conversation for one of the owner's service requests.</summary>
public sealed class MobileConversationDto
{
    public long ServiceRequestId { get; set; }
    public string Title { get; set; } = default!;
    public string? LastMessagePreview { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    /// <summary>Conversation status name; ChannelOpen = not "Closed".</summary>
    public string? Status { get; set; }
    public bool ChannelOpen { get; set; }
    public string? Topic { get; set; }
}

/// <summary>The owner's conversation inbox.</summary>
public sealed class MobileConversationListDto
{
    public List<MobileConversationDto> Items { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>One chat message (cost-free). System/lifecycle messages ride through (SenderType = "System").</summary>
public sealed class MobileChatMessageDto
{
    public long Id { get; set; }
    /// <summary>ServiceRequestMessageSenderType name (Owner/Provider/Admin/System).</summary>
    public string SenderType { get; set; } = default!;
    /// <summary>True when the owner sent it (drives left/right bubble alignment).</summary>
    public bool IsOwn { get; set; }
    public string? SenderName { get; set; }
    /// <summary>ServiceRequestMessageType name (Text/SystemNotification/StatusChange/Image/Location).</summary>
    public string MessageType { get; set; } = default!;
    public string Content { get; set; } = string.Empty;
    /// <summary>Attachment file id (image rides in MO10b — the field is carried through now).</summary>
    public Guid? AttachmentFileId { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public string? LocationLabel { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>A conversation thread for one SR (the owner's own — 403 module-side otherwise).</summary>
public sealed class MobileChatThreadDto
{
    public long ServiceRequestId { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    /// <summary>The counterparty (provider) display name from the conversation participants.</summary>
    public string? CounterpartyName { get; set; }
    public bool ChannelOpen { get; set; }
    public List<MobileChatMessageDto> Messages { get; set; } = new();
    public int TotalCount => Messages.Count;
}

/// <summary>Send-a-message payload — exactly one of { text, image, location } (BE_MO10b). No sender id in the body:
/// <list type="bullet">
/// <item>text → <see cref="Content"/> only;</item>
/// <item>image → <see cref="AttachmentFileId"/> (a FileId from the reused mobile upload session);</item>
/// <item>location → <see cref="LocationLat"/> + <see cref="LocationLng"/> (+ optional <see cref="LocationLabel"/>).</item>
/// </list></summary>
public sealed class MobileSendChatMessageRequest
{
    public string? Content { get; set; }
    /// <summary>A committed FileId (uploaded via <c>api/v1/mobile/uploads</c>) → an image message.</summary>
    public Guid? AttachmentFileId { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public string? LocationLabel { get; set; }
}

/// <summary>A short-lived signed read-url for a chat/attachment file the owner is authorized to view.</summary>
public sealed class MobileAttachmentReadUrlDto
{
    /// <summary>The presigned GET url; empty when the object could not be resolved (FE shows a placeholder).</summary>
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
