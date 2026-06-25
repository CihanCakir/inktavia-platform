using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

[DocumentationInfo("ServiceRequest attachment entity", "File attachment linked to a service request.")]
public sealed class ServiceRequestAttachmentEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public Guid FileId { get; private set; }
    public ServiceRequestAttachmentType AttachmentType { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public long UploaderUserId { get; private set; }
    public ServiceRequestActorType UploaderActorType { get; private set; }

    public ServiceRequestEntity ServiceRequest { get; private set; } = default!;

    public ServiceRequestAttachmentEntity() { }

    public static ServiceRequestAttachmentEntity Create(
        long serviceRequestId,
        Guid fileId,
        ServiceRequestAttachmentType attachmentType,
        string? title,
        string? description,
        long uploaderUserId,
        ServiceRequestActorType uploaderActorType)
    {
        return new ServiceRequestAttachmentEntity
        {
            ServiceRequestId = serviceRequestId,
            FileId = fileId,
            AttachmentType = attachmentType,
            Title = title,
            Description = description,
            UploaderUserId = uploaderUserId,
            UploaderActorType = uploaderActorType,
            IsActive = true
        };
    }
}
