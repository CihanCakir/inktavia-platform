namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Get conversation list response", "Paginated list of conversation summaries.")]
public sealed class GetConversationListResponse
{
    public List<ConversationSummaryDto> Items { get; }
    public int Total { get; }

    public GetConversationListResponse(List<ConversationSummaryDto> items, int total)
    {
        Items = items;
        Total = total;
    }
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
