using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestTimelineQuery : AizenQuery<AdminServiceRequestTimelineResponse>
{
    public long ServiceRequestId { get; }
    public GetAdminServiceRequestTimelineQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
