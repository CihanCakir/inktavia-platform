using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetServiceRequestFilterOptionsBffQuery : AizenQuery<AdminServiceRequestFilterOptionsResponse>
{
    public GetServiceRequestFilterOptionsBffQuery()
    {
    }
}
