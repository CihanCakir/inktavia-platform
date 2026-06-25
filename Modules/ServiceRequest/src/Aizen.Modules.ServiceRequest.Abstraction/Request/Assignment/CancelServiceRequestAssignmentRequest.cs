using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;

[DocumentationInfo("Cancel assignment request", "Cancels an active service request assignment.")]
public sealed class CancelServiceRequestAssignmentRequest
{
    public string? Reason { get; set; }
}
