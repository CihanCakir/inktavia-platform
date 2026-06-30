using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

[DocumentationInfo("Change dispute status request", "Admin changes the status of an open dispute.")]
public sealed class ChangeServiceRequestDisputeStatusRequest
{
    public ServiceRequestDisputeStatus Status { get; set; }
    public string? Notes { get; set; }
}
