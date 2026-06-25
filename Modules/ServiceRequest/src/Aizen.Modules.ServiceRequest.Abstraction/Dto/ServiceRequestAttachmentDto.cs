using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest attachment DTO", "Represents a file attachment linked to a service request.")]
public sealed class ServiceRequestAttachmentDto
{
    public long Id { get; set; }
    public Guid FileId { get; set; }
    public ServiceRequestAttachmentType AttachmentType { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public long UploaderUserId { get; set; }
    public ServiceRequestActorType UploaderActorType { get; set; }
    public DateTime CreatedAt { get; set; }
}
