using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetServiceRequestTimelineBffQuery : AizenQuery<AdminServiceRequestTimelineResponse>
{
    public long ServiceRequestId { get; }
    public GetServiceRequestTimelineBffQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
