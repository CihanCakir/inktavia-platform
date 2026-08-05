using System.Text.Json;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;

namespace Aizen.Modules.Messaging.Repository.Mapping;

[DocumentationInfo("Messaging mapping extensions", "Extension methods for mapping messaging entities to DTOs.")]
public static class MessagingMappingExtensions
{
    public static ConversationSummaryDto ToSummaryDto(this ConversationEntity entity) => new()
    {
        Id          = entity.Id.ToString(),
        Title       = entity.Title,
        ContextType = entity.ContextType.ToString(),
        ContextId   = entity.ContextId.ToString(),
        Preview     = entity.LastMessagePreview,
        Timestamp   = entity.LastMessageAt,
        UnreadCount = entity.UnreadCountByAdmin,
        Status      = entity.Status.ToString(),
        Topic       = entity.Topic?.ToString(),
    };

    public static ChatMessageDto ToDto(this ConversationMessageEntity entity) => new()
    {
        Id               = entity.Id.ToString(),
        SenderUserId     = entity.SenderUserId.ToString(),
        SenderName       = entity.SenderName,
        SenderRole       = entity.SenderRole.ToString(),
        Content          = entity.Content,
        Type             = entity.Type.ToString(),
        IsInternalNote   = entity.IsInternalNote,
        ModerationStatus = entity.ModerationStatus.ToString(),
        ModerationReason = entity.ModerationReason,
        Timestamp        = entity.SentAt,
        Attachments      = entity.Attachments.Select(a => new AttachmentDto(
            a.FileStorageId ?? string.Empty,
            a.FileName,
            a.FileType
        )).ToList(),
        Location         = entity.Type == MessageType.Location
            ? ParseLocation(entity.Content)
            : null,
    };

    private static LocationContentDto? ParseLocation(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json).RootElement;
            return new LocationContentDto(
                doc.GetProperty("lat").GetDouble(),
                doc.GetProperty("lng").GetDouble(),
                doc.TryGetProperty("label", out var label) ? label.GetString() ?? string.Empty : string.Empty,
                doc.TryGetProperty("accuracy", out var acc) ? acc.GetDouble() : null
            );
        }
        catch { return null; }
    }

    public static ModerationQueueItemDto ToModerationDto(this ConversationMessageEntity entity) => new()
    {
        MessageId        = entity.Id.ToString(),
        ConversationId   = entity.ConversationId.ToString(),
        SenderName       = entity.SenderName,
        SenderRole       = entity.SenderRole.ToString(),
        Content          = entity.Content,
        ModerationStatus = entity.ModerationStatus.ToString(),
        ModerationReason = entity.ModerationReason,
        SentAt           = entity.SentAt,
    };
}
