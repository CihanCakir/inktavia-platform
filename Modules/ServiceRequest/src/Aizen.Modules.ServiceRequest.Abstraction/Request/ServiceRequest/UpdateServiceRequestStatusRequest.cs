using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

public sealed class UpdateServiceRequestStatusRequest
{
    public ServiceRequestStatus Status { get; set; }
    public string? Reason { get; set; }
}
