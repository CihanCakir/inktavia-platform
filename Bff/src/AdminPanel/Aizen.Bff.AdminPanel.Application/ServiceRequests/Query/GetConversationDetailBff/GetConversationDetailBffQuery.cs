using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetConversationDetailBffQuery : AizenQuery<AdminConversationDetailResponse>
{
    public long ConversationId { get; }

    public GetConversationDetailBffQuery(long conversationId)
    {
        ConversationId = conversationId;
    }
}
