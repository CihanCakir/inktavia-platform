using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;

[DocumentationInfo("Complete assignment request", "Provider marks an assignment as completed.")]
public sealed class CompleteServiceRequestAssignmentRequest
{
    public string? Notes { get; set; }
}
