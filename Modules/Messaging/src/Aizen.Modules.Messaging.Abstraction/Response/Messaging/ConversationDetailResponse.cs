namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Get conversation detail response", "Full conversation detail with participants and messages.")]
public sealed class GetConversationDetailResponse
{
    public ConversationDetailDto Conversation { get; }
    public GetConversationDetailResponse(ConversationDetailDto conversation) => Conversation = conversation;
}

public sealed record ConversationDetailDto
{
    public string Id                             { get; init; } = string.Empty;
    public string Title                          { get; init; } = string.Empty;
    public string ContextType                    { get; init; } = string.Empty;
    public string ContextId                      { get; init; } = string.Empty;
    public string Status                         { get; init; } = string.Empty;
    public List<ParticipantDto> Participants     { get; init; } = [];
    public List<ChatMessageDto> Messages         { get; init; } = [];
}

public sealed record ParticipantDto(string UserId, string DisplayName, string Role);

public sealed record ChatMessageDto
{
    public string Id                        { get; init; } = string.Empty;
    public string SenderUserId              { get; init; } = string.Empty;
    public string SenderName                { get; init; } = string.Empty;
    public string SenderRole                { get; init; } = string.Empty;
    public string Content                   { get; init; } = string.Empty;
    public string Type                      { get; init; } = string.Empty;
    public bool IsInternalNote              { get; init; }
    public string ModerationStatus          { get; init; } = string.Empty;
    public DateTimeOffset Timestamp         { get; init; }
    public List<AttachmentDto> Attachments  { get; init; } = [];
    public LocationContentDto? Location     { get; init; }
}

public sealed record AttachmentDto(string Url, string FileName, string FileType, string? ReadUrl = null);
