using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Cancel service request request", "Input model for cancelling a service request.")]
public sealed class CancelServiceRequestRequest
{
    /// <summary>N-E structured cancel reason (owner). Auto-maps to a Payment RefundReason for deterministic refund allocation.</summary>
    public ServiceRequestCancelReason? ReasonCode { get; set; }

    /// <summary>Optional free-text note (kept alongside the structured reason).</summary>
    public string? Reason { get; set; }
}
