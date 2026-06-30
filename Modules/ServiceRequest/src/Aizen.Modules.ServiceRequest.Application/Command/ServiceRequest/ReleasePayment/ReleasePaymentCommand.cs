using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Release payment command", "Releases payment for a completed service request.")]
public sealed class ReleasePaymentCommand : AizenCommand<ReleasePaymentResponse>
{
    public long ServiceRequestId { get; }
    public ReleasePaymentCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
