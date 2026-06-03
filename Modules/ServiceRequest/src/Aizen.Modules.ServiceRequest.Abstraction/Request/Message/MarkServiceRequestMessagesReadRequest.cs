using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Message;

[DocumentationInfo("Mark messages read request", "Marks one or more service request messages as read.")]
public sealed class MarkServiceRequestMessagesReadRequest
{
    public List<long> MessageIds { get; set; } = new();
}
