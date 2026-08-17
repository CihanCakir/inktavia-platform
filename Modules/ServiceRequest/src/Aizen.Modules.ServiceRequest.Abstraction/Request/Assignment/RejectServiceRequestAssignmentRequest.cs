using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;

[DocumentationInfo("Reject assignment request", "Provider rejects an assignment.")]
public sealed class RejectServiceRequestAssignmentRequest
{
    /// <summary>N-E structured reject reason (provider). Surfaced in rejection views and mappable to a provider-fault RefundReason.</summary>
    public AssignmentRejectReason? ReasonCode { get; set; }

    /// <summary>Optional free-text note (kept alongside the structured reason).</summary>
    public string? Reason { get; set; }
}
