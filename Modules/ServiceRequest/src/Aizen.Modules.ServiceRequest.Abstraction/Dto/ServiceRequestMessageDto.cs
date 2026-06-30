using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest message DTO", "Represents a single message exchanged within a service request thread.")]
public sealed class ServiceRequestMessageDto
{
    public long Id { get; set; }
    public long SenderUserId { get; set; }
    public ServiceRequestMessageSenderType SenderType { get; set; }
    public ServiceRequestMessageType MessageType { get; set; }
    public string Content { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? AttachmentFileId { get; set; }
    public DateTime CreatedAt { get; set; }
}
