namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Get moderation queue response", "Returns flagged and pending-review messages for admin review.")]
public sealed class GetModerationQueueResponse
{
    public List<ModerationQueueItemDto> Items { get; }
    public int Total { get; }

    public GetModerationQueueResponse(List<ModerationQueueItemDto> items, int total)
    {
        Items = items;
        Total = total;
    }
}

public sealed record ModerationQueueItemDto
{
    public string MessageId         { get; init; } = string.Empty;
    public string ConversationId    { get; init; } = string.Empty;
    public string SenderName        { get; init; } = string.Empty;
    public string SenderRole        { get; init; } = string.Empty;
    public string Content           { get; init; } = string.Empty;
    public string ModerationStatus  { get; init; } = string.Empty;
    public string? ModerationReason { get; init; }
    public DateTimeOffset SentAt    { get; init; }
}
