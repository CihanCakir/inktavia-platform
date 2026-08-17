using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestOperationDetailQuery : AizenQuery<AdminServiceRequestOperationDetailResponse>
{
    public long ServiceRequestId { get; }
    public GetAdminServiceRequestOperationDetailQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
