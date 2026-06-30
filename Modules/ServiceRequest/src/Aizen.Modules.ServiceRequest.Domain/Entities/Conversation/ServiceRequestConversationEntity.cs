using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;

[DocumentationInfo("Service request conversation entity", "A grouped conversation thread scoped to a service request.")]
public sealed class ServiceRequestConversationEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Status { get; private set; } = "active";
    public int UnreadCount { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }

    private readonly List<ConversationMessageEntity> _messages = new();
    public IReadOnlyCollection<ConversationMessageEntity> Messages => _messages.AsReadOnly();

    public ServiceRequestConversationEntity() { }

    public static ServiceRequestConversationEntity Create(long serviceRequestId, string title)
    {
        return new ServiceRequestConversationEntity
        {
            ServiceRequestId = serviceRequestId,
            Title = title.Trim(),
            Status = "active",
            UnreadCount = 0,
            LastMessageAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    public void AddMessage(ConversationMessageEntity message)
    {
        _messages.Add(message);
        LastMessageAt = message.SentAt;
        UnreadCount++;
    }

    public void MarkRead() => UnreadCount = 0;
}
