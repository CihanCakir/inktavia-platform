using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetWorkLogsBffQuery : AizenQuery<AdminWorkLogsResponse>
{
    public long ServiceRequestId { get; }

    public GetWorkLogsBffQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
