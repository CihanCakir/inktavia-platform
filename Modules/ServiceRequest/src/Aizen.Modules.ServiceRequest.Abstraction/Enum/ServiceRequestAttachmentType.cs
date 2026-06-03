using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest attachment type enum", "Type of file attached to a service request.")]
public enum ServiceRequestAttachmentType
{
    Photo = 1,
    Video = 2,
    Document = 3,
    Invoice = 4,
    Certificate = 5,
    Other = 99
}
