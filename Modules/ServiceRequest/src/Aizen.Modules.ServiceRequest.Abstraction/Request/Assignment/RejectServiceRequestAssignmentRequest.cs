
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;

[DocumentationInfo("Reject assignment request", "Provider rejects an assignment.")]
public sealed class RejectServiceRequestAssignmentRequest
{
    public string? Reason { get; set; }
}
