using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminWorkLogsQuery : AizenQuery<AdminWorkLogsResponse>
{
    public long ServiceRequestId { get; }
    public string UserToken { get; }

    public GetAdminWorkLogsQuery(long serviceRequestId, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        UserToken = userToken;
    }
}
