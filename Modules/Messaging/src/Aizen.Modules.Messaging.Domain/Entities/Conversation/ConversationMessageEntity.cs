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

    // ── BE_WC0 (Phase-4 parity) — additive, nullable. Location = discrete geo payload (currently the synced value
    // still rides in Content as JSON; these columns are unused until WC1/WC2). SourceKey = stable natural key for
    // durable cross-replica idempotency (see ServiceRequestMessageMapping.SourceKey / SystemSourceKey); null for
    // native Messaging sends. A partial unique index on (ConversationId, SourceKey) enforces it where non-null.
    public decimal? LocationLat                      { get; private set; }
    public decimal? LocationLng                      { get; private set; }
    public string? LocationLabel                     { get; private set; }
    public string? SourceKey                         { get; private set; }

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
        bool isInternalNote = false,
        DateTimeOffset? sentAt = null,
        string? sourceKey = null,
        decimal? locationLat = null,
        decimal? locationLng = null,
        string? locationLabel = null)
    {
        return new ConversationMessageEntity
        {
            ConversationId   = conversationId,
            SenderUserId     = senderUserId,
            SenderName       = senderName.Trim(),
            SenderRole       = senderRole,
            Content          = content.Trim(),
            Type             = type,
            IsInternalNote   = isInternalNote,
            ModerationStatus = MessageModerationStatus.Allowed,
            // Defaults to now for live sends; the backfill passes the original source timestamp (UTC) to preserve history.
            SentAt           = sentAt ?? DateTimeOffset.UtcNow,
            IsActive         = true,
            // BE_WC0 additive parity fields (null unless the caller supplies them).
            SourceKey        = string.IsNullOrWhiteSpace(sourceKey) ? null : sourceKey.Trim(),
            LocationLat      = locationLat,
            LocationLng      = locationLng,
            LocationLabel    = string.IsNullOrWhiteSpace(locationLabel) ? null : locationLabel!.Trim(),
        };
    }

    public void AddAttachment(MessageAttachmentEntity attachment)
        => _attachments.Add(attachment);

    /// <summary>BE_WC0 — set the durable idempotency key on an already-built row (used by the SourceKey backfill).</summary>
    public void SetSourceKey(string? sourceKey)
        => SourceKey = string.IsNullOrWhiteSpace(sourceKey) ? null : sourceKey.Trim();

    /// <summary>BE_WC0 — set the discrete geo payload (kept for WC1/WC2; unused by the current sync path).</summary>
    public void SetLocation(decimal? lat, decimal? lng, string? label)
    {
        LocationLat   = lat;
        LocationLng   = lng;
        LocationLabel = string.IsNullOrWhiteSpace(label) ? null : label!.Trim();
    }

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
