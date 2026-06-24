using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestOffersQuery : AizenQuery<AdminServiceRequestOffersResponse>
{
    public long ServiceRequestId { get; }
    public string UserToken { get; }

    public GetAdminServiceRequestOffersQuery(long serviceRequestId, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        UserToken = userToken;
    }
}
