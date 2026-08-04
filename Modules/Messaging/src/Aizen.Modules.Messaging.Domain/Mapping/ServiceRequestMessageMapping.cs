using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;

namespace Aizen.Modules.Messaging.Domain.Mapping;

/// <summary>
/// The SINGLE ServiceRequest-chat → Messaging-message mapping, shared by the one-time backfill
/// (<c>ServiceRequestChatBackfiller</c>) and the live-sync consumer so both produce byte-identical rows and the
/// same idempotency key. Takes primitive SR fields (no ServiceRequest reference) so it can live in Domain and be
/// referenced by both the Repository (backfiller) and the host (consumer).
/// </summary>
public static class ServiceRequestMessageMapping
{
    /// <summary>
    /// Idempotency key. SentAt is floored to whole seconds because the backfill reads the persisted SR
    /// <c>CreateDate</c> (audit set at SaveChanges) while the live event carries the publish-time timestamp — the
    /// two are milliseconds apart within the same send, so a second-granularity key makes backfill + live-sync
    /// converge (redelivery / re-run never doubles).
    /// </summary>
    public static string MessageKey(long senderUserId, DateTimeOffset sentAt, string content)
        => $"{senderUserId}|{sentAt.ToUnixTimeSeconds()}|{(content ?? string.Empty).Trim()}";

    // SR ServiceRequestMessageType (int) → Messaging MessageType. Offer(4) has no Messaging equivalent → StatusChange.
    public static MessageType MapMessageType(int srMessageType) => srMessageType switch
    {
        1 => MessageType.Text,
        2 => MessageType.SystemNotification,
        3 => MessageType.StatusChange,
        4 => MessageType.StatusChange, // Offer
        5 => MessageType.MediaAttachment, // Image
        6 => MessageType.Location,
        _ => MessageType.Text,
    };

    // Placeholder used only when no real display name is available (resolved from UserProfiles elsewhere).
    public static string RoleName(MessagingParticipantRole role) => role switch
    {
        MessagingParticipantRole.Owner    => "Owner",
        MessagingParticipantRole.Provider => "Provider",
        MessagingParticipantRole.Admin    => "Admin",
        MessagingParticipantRole.System   => "System",
        _ => role.ToString(),
    };

    /// <summary>
    /// Build a Messaging message from SR fields. <paramref name="senderName"/> is the resolved display name (from
    /// the enriched event or a UserProfiles lookup); falls back to the role placeholder when null/empty.
    /// </summary>
    public static ConversationMessageEntity MapMessage(
        long conversationId,
        long senderUserId,
        int srSenderType,   // SR ServiceRequestMessageSenderType (1 Owner / 2 Provider / 3 Admin / 4 System) == Messaging role ints
        int srMessageType,
        string content,
        Guid? attachmentFileId,
        DateTimeOffset sentAt,
        string? senderName)
    {
        var role = (MessagingParticipantRole)srSenderType;
        var type = MapMessageType(srMessageType);
        var name = string.IsNullOrWhiteSpace(senderName) ? RoleName(role) : senderName!.Trim();

        var msg = ConversationMessageEntity.Create(
            conversationId, senderUserId, name, role, content, type, isInternalNote: false, sentAt: sentAt);

        if (attachmentFileId is { } fileId)
        {
            var fileRef = fileId.ToString();
            msg.AddAttachment(MessageAttachmentEntity.Create(
                0, fileRef, type == MessageType.MediaAttachment ? "image" : "document", fileRef));
        }

        return msg;
    }
}
