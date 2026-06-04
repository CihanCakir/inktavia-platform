using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestOperationDetailQuery : AizenQuery<AdminServiceRequestOperationDetailResponse>
{
    public long ServiceRequestId { get; }
    public string Authorization { get; }
    public GetAdminServiceRequestOperationDetailQuery(long serviceRequestId, string authorization)
    {
        ServiceRequestId = serviceRequestId;
        Authorization = authorization;
    }
}

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
