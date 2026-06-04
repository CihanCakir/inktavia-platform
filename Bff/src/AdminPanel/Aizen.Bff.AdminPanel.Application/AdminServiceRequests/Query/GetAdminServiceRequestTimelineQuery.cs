using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestTimelineQuery : AizenQuery<AdminServiceRequestTimelineResponse>
{
    public long ServiceRequestId { get; }
    public string Authorization { get; }
    public GetAdminServiceRequestTimelineQuery(long serviceRequestId, string authorization)
    {
        ServiceRequestId = serviceRequestId;
        Authorization = authorization;
    }
}

[DocumentationInfo("Get admin service request timeline query handler", "Fetches service request detail for the admin timeline view.")]
public sealed class GetAdminServiceRequestTimelineQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestTimelineQuery, AdminServiceRequestTimelineResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public GetAdminServiceRequestTimelineQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminServiceRequestTimelineResponse?> Handle(
        GetAdminServiceRequestTimelineQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestTimelineResponse();

        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestDetail(
                request.ServiceRequestId,
                request.Authorization);

            response.ServiceRequest = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
