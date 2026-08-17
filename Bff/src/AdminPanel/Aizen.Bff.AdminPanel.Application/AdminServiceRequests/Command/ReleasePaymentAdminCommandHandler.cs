using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Release payment admin command handler", "Releases payment for a completed service request via the ServiceRequest module.")]
public sealed class ReleasePaymentAdminCommandHandler
    : AizenCommandHandler<ReleasePaymentAdminCommand, ReleasePaymentResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public ReleasePaymentAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ReleasePaymentResponse?> Handle(
        ReleasePaymentAdminCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.ReleaseServiceRequestPayment(
            request.ServiceRequestId);
        return result.Body;
    }
}
