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

    /// <summary>
    /// BE_WC0 — the durable, stable <c>SourceKey</c> for a message mirrored from a ServiceRequest chat row. Uses the
    /// SR message's own primary key (carried on the enriched event / read from the SR table by the backfill), so the
    /// live-sync consumer and the backfill converge on an identical value and a redelivered event hits the partial
    /// unique index on <c>(ConversationId, SourceKey)</c> instead of inserting a duplicate row. Scheme:
    /// <c>sr:{serviceRequestId}:{srMessageId}</c>.
    /// </summary>
    public static string SourceKey(long serviceRequestId, long srMessageId)
        => $"sr:{serviceRequestId}:{srMessageId}";

    /// <summary>
    /// BE_WC0 — the <c>SourceKey</c> for a System/lifecycle message generated inside Messaging from an SR domain event
    /// (WC1). Scheme: <c>sys:{serviceRequestId}:{code}</c> (e.g. <c>sys:42:OFFER_ACCEPTED</c>). Distinct namespace from
    /// the synced <c>sr:</c> keys so the two producers never collide; native Messaging sends leave <c>SourceKey</c> null.
    /// </summary>
    public static string SystemSourceKey(long serviceRequestId, string code)
        => $"sys:{serviceRequestId}:{(code ?? string.Empty).Trim()}";

    /// <summary>
    /// BE_WC1 — the lifecycle SourceKey CODE for a synced SR message, or <c>null</c> when it is a regular chat
    /// message (Text/Image/Location) that should key on the SR message id instead. This is what lets the SR→Messaging
    /// sync consumer and the WC1 Messaging lifecycle consumers converge on the SAME <c>sys:{srId}:{code}</c> key so the
    /// partial unique index keeps exactly one row across the parallel run. Codes:
    /// <list type="bullet">
    ///   <item>offer card (SR Offer type, content <c>offer:{offerId}|…</c>) → <c>OFFER:{offerId}</c>;</item>
    ///   <item>System lifecycle (SR System sender / StatusChange / SystemNotification) → the content code verbatim
    ///   (<c>OFFER_ACCEPTED</c>, <c>JOB_STARTED</c>, <c>JOB_COMPLETED</c>, <c>CONVERSATION_CLOSED</c>).</item>
    /// </list>
    /// SR ints: senderType 4 = System; messageType 2 = SystemNotification, 3 = StatusChange, 4 = Offer.
    /// </summary>
    public static string? LifecycleCode(int srSenderType, int srMessageType, string? content)
    {
        // Offer card — Provider/Offer with an "offer:{offerId}|..." payload.
        if (srMessageType == 4 && !string.IsNullOrWhiteSpace(content) &&
            content!.StartsWith("offer:", StringComparison.Ordinal))
        {
            var rest = content.Substring("offer:".Length);
            var bar = rest.IndexOf('|');
            var offerId = (bar >= 0 ? rest.Substring(0, bar) : rest).Trim();
            return offerId.Length > 0 ? $"OFFER:{offerId}" : null;
        }

        // System lifecycle — the content IS the status code.
        if (srSenderType == 4 || srMessageType == 2 || srMessageType == 3)
            return string.IsNullOrWhiteSpace(content) ? null : content!.Trim();

        return null;
    }

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
        string? senderName,
        decimal? locationLat = null,
        decimal? locationLng = null,
        string? locationLabel = null,
        string? sourceKey = null)
    {
        var role = (MessagingParticipantRole)srSenderType;
        var type = MapMessageType(srMessageType);
        var name = string.IsNullOrWhiteSpace(senderName) ? RoleName(role) : senderName!.Trim();

        // Location messages carry their payload in Content as JSON ({lat,lng,label}) — that's what the read side
        // (ToDto → ParseLocation → LocationContentDto) expects. The SR side stores only a "lat,lng"/label string in
        // Content, so rebuild the JSON here from the event's discrete lat/lng; otherwise a synced SR location would
        // render as plain text in the admin audit instead of a map bubble.
        if (type == MessageType.Location && locationLat is { } lat && locationLng is { } lng)
        {
            var label = string.IsNullOrWhiteSpace(locationLabel) ? content : locationLabel!.Trim();
            content = BuildLocationJson(lat, lng, label);
        }

        var msg = ConversationMessageEntity.Create(
            conversationId, senderUserId, name, role, content, type, isInternalNote: false, sentAt: sentAt,
            sourceKey: sourceKey);

        if (attachmentFileId is { } fileId)
        {
            var fileRef = fileId.ToString();
            msg.AddAttachment(MessageAttachmentEntity.Create(
                0, fileRef, type == MessageType.MediaAttachment ? "image" : "document", fileRef));
        }

        return msg;
    }

    // Matches the JSON shape ParseLocation reads (lat/lng/label). Invariant culture so the decimal separator is a
    // dot regardless of server locale, and JSON-escape the label so a quote/backslash in it can't break the payload.
    private static string BuildLocationJson(decimal lat, decimal lng, string label)
        => $"{{\"lat\":{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},"
         + $"\"lng\":{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)},"
         + $"\"label\":{System.Text.Json.JsonSerializer.Serialize(label ?? string.Empty)}}}";
}
