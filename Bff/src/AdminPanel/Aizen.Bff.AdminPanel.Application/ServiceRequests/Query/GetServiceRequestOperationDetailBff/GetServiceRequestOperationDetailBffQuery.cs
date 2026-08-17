using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetServiceRequestOperationDetailBffQuery : AizenQuery<AdminServiceRequestOperationDetailResponse>
{
    public long ServiceRequestId { get; }
    public GetServiceRequestOperationDetailBffQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
