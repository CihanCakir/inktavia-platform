using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin dispute list query handler", "Fetches the paged admin dispute list with optional status filter.")]
public sealed class GetAdminDisputeListQueryHandler
    : AizenQueryHandler<GetAdminDisputeListQuery, GetAdminDisputeListResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public GetAdminDisputeListQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<GetAdminDisputeListResponse?> Handle(
        GetAdminDisputeListQuery request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.GetAdminDisputeList(
request.Status, request.PageIndex, request.PageSize);
        return result.Body;
    }
}
