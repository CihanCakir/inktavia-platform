using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Cancel service request request", "Input model for cancelling a service request.")]
public sealed class CancelServiceRequestRequest
{
    public string? Reason { get; set; }
}
