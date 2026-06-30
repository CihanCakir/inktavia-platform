
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest message type enum", "Type of message exchanged within a service request thread.")]
public enum ServiceRequestMessageType
{
    Text = 1,
    SystemNotification = 2,
    StatusChange = 3
}
