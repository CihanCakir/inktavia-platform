using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestFilterOptionsQuery : AizenQuery<AdminServiceRequestFilterOptionsResponse>
{
    public string UserToken { get; }
    public GetAdminServiceRequestFilterOptionsQuery(string userToken)
    {
        UserToken = userToken;
    }
}
