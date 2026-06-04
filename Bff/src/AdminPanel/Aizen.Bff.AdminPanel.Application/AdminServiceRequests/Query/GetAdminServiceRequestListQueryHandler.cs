using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin service request list query handler", "Fetches the paged admin service request list with optional status and vessel filters.")]
public sealed class GetAdminServiceRequestListQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestListQuery, AdminServiceRequestListResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public GetAdminServiceRequestListQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminServiceRequestListResponse?> Handle(
        GetAdminServiceRequestListQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestListResponse();

        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestList(
                request.Authorization, request.UserToken, request.Status, request.VesselId, request.PageIndex, request.PageSize);
            response.ServiceRequests = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
