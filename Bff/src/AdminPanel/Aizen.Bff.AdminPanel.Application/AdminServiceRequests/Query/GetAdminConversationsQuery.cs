using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminConversationsQuery : AizenQuery<AdminConversationsResponse>
{
    public string? Filter { get; }
    public string UserToken { get; }

    public GetAdminConversationsQuery(string? filter, string userToken)
    {
        Filter = filter;
        UserToken = userToken;
    }
}
