using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin service request operation detail query handler", "Fetches the full service request detail for the admin operation panel.")]
public sealed class GetAdminServiceRequestOperationDetailQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestOperationDetailQuery, AdminServiceRequestOperationDetailResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public GetAdminServiceRequestOperationDetailQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminServiceRequestOperationDetailResponse?> Handle(
        GetAdminServiceRequestOperationDetailQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestOperationDetailResponse();

        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestDetail(
                request.ServiceRequestId, request.Authorization, request.UserToken);
            response.ServiceRequest = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
