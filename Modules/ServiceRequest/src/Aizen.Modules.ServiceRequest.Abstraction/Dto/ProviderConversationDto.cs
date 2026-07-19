namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

public sealed class ProviderConversationDto
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? LastMessagePreview { get; set; }
    public int LastMessageType { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool ChannelOpen { get; set; }
    public string? LifecycleStatus { get; set; }
}
