using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin dispute list query handler", "Fetches the paged admin dispute list with optional status filter.")]
public sealed class GetDisputeListBffQueryHandler
    : AizenQueryHandler<GetDisputeListBffQuery, GetAdminDisputeListResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetDisputeListBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<GetAdminDisputeListResponse?> Handle(
        GetDisputeListBffQuery request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.GetAdminDisputeList(
request.Status, request.PageIndex, request.PageSize);
        return result.Body;
    }
}
