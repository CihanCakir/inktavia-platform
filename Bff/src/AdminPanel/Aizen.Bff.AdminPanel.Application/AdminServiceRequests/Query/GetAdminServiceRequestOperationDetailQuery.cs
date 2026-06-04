using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestOperationDetailQuery : AizenQuery<AdminServiceRequestOperationDetailResponse>
{
    public long ServiceRequestId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public GetAdminServiceRequestOperationDetailQuery(long serviceRequestId, string authorization, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
