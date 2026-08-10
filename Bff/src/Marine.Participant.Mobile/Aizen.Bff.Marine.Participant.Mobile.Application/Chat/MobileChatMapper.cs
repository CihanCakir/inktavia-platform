using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>
/// Projects the Messaging + SR chat DTOs → the cost-free mobile owner chat contracts (BE_MO10a). Reads come from the
/// Messaging store (<c>ChatMessageDto</c>, sender role as a string); the send result comes from the SR module
/// (<c>ServiceRequestMessageDto</c>, sender type as an enum) — both map to the same <see cref="MobileChatMessageDto"/>.
/// <c>IsOwn</c> = the owner sent it. Enums cross as string names; System messages ride through for lifecycle pills.
/// </summary>
public static class MobileChatMapper
{
    public static MobileConversationListDto MapConversationList(GetConversationListResponse? r) => new()
    {
        Total = r?.Total ?? 0,
        Items = r?.Items?.Select(MapConversation).ToList() ?? new List<MobileConversationDto>(),
    };

    public static MobileConversationDto MapConversation(ConversationSummaryDto s) => new()
    {
        ServiceRequestId   = long.TryParse(s.ContextId, out var id) ? id : 0,
        Title              = s.Title,
        LastMessagePreview = string.IsNullOrEmpty(s.Preview) ? null : s.Preview,
        LastMessageAt      = s.Timestamp,
        UnreadCount        = s.UnreadCount,
        Status             = s.Status,
        ChannelOpen        = !string.Equals(s.Status, "Closed", StringComparison.OrdinalIgnoreCase),
        Topic              = s.Topic,
    };

    public static MobileChatThreadDto MapThread(GetConversationDetailResponse? r)
    {
        var c = r?.Conversation;
        if (c is null) return new MobileChatThreadDto();

        return new MobileChatThreadDto
        {
            ServiceRequestId = long.TryParse(c.ContextId, out var id) ? id : 0,
            Title            = c.Title,
            Status           = c.Status,
            ChannelOpen      = !string.Equals(c.Status, "Closed", StringComparison.OrdinalIgnoreCase),
            // The counterparty (provider) display name from the conversation participants.
            CounterpartyName = c.Participants
                .FirstOrDefault(p => string.Equals(p.Role, "Provider", StringComparison.OrdinalIgnoreCase))?.DisplayName,
            Messages         = c.Messages.Select(MapMessagingMessage).ToList(),
        };
    }

    /// <summary>Messaging thread message → mobile message. Sender role is a string (Owner/Provider/Admin/System).</summary>
    public static MobileChatMessageDto MapMessagingMessage(ChatMessageDto m) => new()
    {
        Id            = long.TryParse(m.Id, out var mid) ? mid : 0,
        SenderType    = string.IsNullOrWhiteSpace(m.SenderRole) ? "System" : m.SenderRole,
        IsOwn         = string.Equals(m.SenderRole, "Owner", StringComparison.OrdinalIgnoreCase),
        SenderName    = string.IsNullOrEmpty(m.SenderName) ? null : m.SenderName,
        MessageType   = string.IsNullOrWhiteSpace(m.Type) ? "Text" : m.Type,
        Content       = m.Content,
        AttachmentFileId = ParseAttachment(m.Attachments),
        LocationLat   = m.Location?.Lat,
        LocationLng   = m.Location?.Lng,
        LocationLabel = m.Location?.Label,
        IsRead        = true,   // per-participant read state is not surfaced this phase
        CreatedAt     = m.Timestamp,
    };

    /// <summary>SR send-result message → mobile message. Sender type is an enum; the owner is the caller so IsOwn is
    /// true for a just-sent message.</summary>
    public static MobileChatMessageDto MapSentMessage(ServiceRequestMessageDto m) => new()
    {
        Id            = m.Id,
        SenderType    = m.SenderType.ToString(),
        IsOwn         = m.SenderType == ServiceRequestMessageSenderType.Owner,
        MessageType   = m.MessageType.ToString(),
        Content       = m.Content,
        AttachmentFileId = m.AttachmentFileId,
        LocationLat   = m.LocationLat is { } lat ? (double)lat : null,
        LocationLng   = m.LocationLng is { } lng ? (double)lng : null,
        LocationLabel = m.LocationLabel,
        IsRead        = m.IsRead,
        CreatedAt     = m.CreatedAt,
    };

    private static Guid? ParseAttachment(List<AttachmentDto> attachments)
        => attachments.Count > 0 && Guid.TryParse(attachments[0].Url, out var g) ? g : null;
}
