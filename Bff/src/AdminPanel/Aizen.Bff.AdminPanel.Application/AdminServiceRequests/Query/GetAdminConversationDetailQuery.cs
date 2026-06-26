using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminConversationDetailQuery : AizenQuery<AdminConversationDetailResponse>
{
    public long ConversationId { get; }

    public GetAdminConversationDetailQuery(long conversationId)
    {
        ConversationId = conversationId;
    }
}
