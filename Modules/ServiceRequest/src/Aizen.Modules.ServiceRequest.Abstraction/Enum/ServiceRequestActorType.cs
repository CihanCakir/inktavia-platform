
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest actor type enum", "Actor role that performs an action within a service request workflow.")]
public enum ServiceRequestActorType
{
    Owner = 1,
    Provider = 2,
    Admin = 3,
    System = 4
}
