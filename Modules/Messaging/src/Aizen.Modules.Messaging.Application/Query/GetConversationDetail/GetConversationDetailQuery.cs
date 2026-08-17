using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationDetail;

[DocumentationInfo("Get conversation detail query", "Full conversation detail with messages and participants.")]
public sealed class GetConversationDetailQuery : AizenQuery<GetConversationDetailResponse>
{
    public long ConversationId { get; }
    public bool IsAdmin        { get; }

    public GetConversationDetailQuery(long conversationId, bool isAdmin = false)
    {
        ConversationId = conversationId;
        IsAdmin        = isAdmin;
    }
}
