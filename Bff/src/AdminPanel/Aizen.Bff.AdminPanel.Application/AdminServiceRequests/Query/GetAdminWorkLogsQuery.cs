using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminWorkLogsQuery : AizenQuery<AdminWorkLogsResponse>
{
    public long ServiceRequestId { get; }

    public GetAdminWorkLogsQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
