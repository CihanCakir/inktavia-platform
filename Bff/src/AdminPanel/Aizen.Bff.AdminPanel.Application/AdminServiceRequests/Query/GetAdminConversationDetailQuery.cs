using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminConversationDetailQuery : AizenQuery<AdminConversationDetailResponse>
{
    public long ConversationId { get; }
    public string UserToken { get; }

    public GetAdminConversationDetailQuery(long conversationId, string userToken)
    {
        ConversationId = conversationId;
        UserToken = userToken;
    }
}
