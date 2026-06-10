using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestTimelineQuery : AizenQuery<AdminServiceRequestTimelineResponse>
{
    public long ServiceRequestId { get; }
    public string UserToken { get; }
    public GetAdminServiceRequestTimelineQuery(long serviceRequestId, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        UserToken = userToken;
    }
}
