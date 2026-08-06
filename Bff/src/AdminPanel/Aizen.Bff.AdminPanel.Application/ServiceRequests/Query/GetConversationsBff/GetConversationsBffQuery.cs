using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetConversationsBffQuery : AizenQuery<AdminConversationsResponse>
{
    public string? Filter { get; }

    public GetConversationsBffQuery(string? filter)
    {
        Filter = filter;
    }
}
