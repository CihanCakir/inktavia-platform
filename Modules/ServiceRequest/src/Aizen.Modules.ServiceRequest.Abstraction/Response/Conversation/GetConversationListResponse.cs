using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;

[DocumentationInfo("Get conversation list response", "List of service request conversations.")]
public sealed class GetConversationListResponse(List<ConversationSummaryDto> items)
{
    public List<ConversationSummaryDto> Items { get; } = items;
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
