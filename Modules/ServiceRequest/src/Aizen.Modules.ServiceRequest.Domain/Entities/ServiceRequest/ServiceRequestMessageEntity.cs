using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

[DocumentationInfo("ServiceRequest message entity", "A message exchanged between owner, provider, or admin scoped to a service request.")]
public sealed class ServiceRequestMessageEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long SenderUserId { get; private set; }
    public ServiceRequestMessageSenderType SenderType { get; private set; }
    public ServiceRequestMessageType MessageType { get; private set; }
    public string Content { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? AttachmentFileId { get; private set; }
    public decimal? LocationLat { get; private set; }
    public decimal? LocationLng { get; private set; }
    public string? LocationLabel { get; private set; }

    public ServiceRequestEntity ServiceRequest { get; private set; } = default!;

    public ServiceRequestMessageEntity() { }

    public static ServiceRequestMessageEntity Create(
        long serviceRequestId,
        long senderUserId,
        ServiceRequestMessageSenderType senderType,
        ServiceRequestMessageType messageType,
        string content,
        Guid? attachmentFileId)
    {
        return new ServiceRequestMessageEntity
        {
            ServiceRequestId = serviceRequestId,
            SenderUserId = senderUserId,
            SenderType = senderType,
            MessageType = messageType,
            Content = content,
            IsRead = false,
            AttachmentFileId = attachmentFileId,
            IsActive = true
        };
    }

    public static ServiceRequestMessageEntity CreateLocation(
        long serviceRequestId, long senderUserId, ServiceRequestMessageSenderType senderType,
        decimal lat, decimal lng, string? label)
    {
        return new ServiceRequestMessageEntity
        {
            ServiceRequestId = serviceRequestId, SenderUserId = senderUserId, SenderType = senderType,
            MessageType = ServiceRequestMessageType.Location,
            Content = label ?? $"{lat},{lng}",
            LocationLat = lat, LocationLng = lng, LocationLabel = label,
            IsRead = false, IsActive = true
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
