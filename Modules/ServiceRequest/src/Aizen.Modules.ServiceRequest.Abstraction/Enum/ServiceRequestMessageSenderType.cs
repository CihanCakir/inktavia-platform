
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest message sender type enum", "Actor type that sent a message within a service request.")]
public enum ServiceRequestMessageSenderType
{
    Owner = 1,
    Provider = 2,
    Admin = 3,
    System = 4
}
