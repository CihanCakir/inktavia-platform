using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin service request timeline query handler", "Fetches service request detail for the admin timeline view.")]
public sealed class GetServiceRequestTimelineBffQueryHandler
    : AizenQueryHandler<GetServiceRequestTimelineBffQuery, AdminServiceRequestTimelineResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetServiceRequestTimelineBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminServiceRequestTimelineResponse?> Handle(
        GetServiceRequestTimelineBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestTimelineResponse();

        try
        {

        var result = await _serviceRequest.GetAdminServiceRequestDetail(
                request.ServiceRequestId);
            response.ServiceRequest = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
