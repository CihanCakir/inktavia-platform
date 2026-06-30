using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;

namespace Aizen.Modules.ServiceRequest.Application.Query.Conversation;

[DocumentationInfo("Get conversation detail query", "Returns full conversation with messages.")]
public sealed class GetConversationDetailQuery : AizenQuery<GetConversationDetailResponse>
{
    public long ConversationId { get; }
    public GetConversationDetailQuery(long conversationId) => ConversationId = conversationId;
}
