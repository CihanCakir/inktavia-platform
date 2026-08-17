using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetServiceRequestOffersBffQuery : AizenQuery<AdminServiceRequestOffersResponse>
{
    public long ServiceRequestId { get; }

    public GetServiceRequestOffersBffQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
