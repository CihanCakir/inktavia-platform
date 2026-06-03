using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

[DocumentationInfo("Open dispute request", "Opens a dispute on a service request.")]
public sealed class OpenServiceRequestDisputeRequest
{
    public ServiceRequestDisputeReason Reason { get; set; }
    public string Description { get; set; } = default!;
}
