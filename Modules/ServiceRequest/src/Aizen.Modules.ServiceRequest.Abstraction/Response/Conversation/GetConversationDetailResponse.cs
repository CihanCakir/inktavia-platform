
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;

[DocumentationInfo("Get conversation detail response", "Full conversation with messages.")]
public sealed class GetConversationDetailResponse(ConversationDetailDto conversation)
{
    public ConversationDetailDto Conversation { get; } = conversation;
}

public sealed record ConversationDetailDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ServiceRequestId { get; init; } = string.Empty;
    public List<ChatMessageDto> Messages { get; init; } = [];
}

public sealed record ChatMessageDto
{
    public string Id { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public string SenderRole { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public List<MessageAttachmentItemDto> Attachments { get; init; } = [];
}

public sealed record MessageAttachmentItemDto
{
    public string Url { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "document";
}
