using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetMyConversationByContext;

[DocumentationInfo("Get my conversation-by-context query",
    "Participant-scoped thread read keyed by domain context (e.g. ServiceRequest id). The caller MUST be a " +
    "participant of the resolved conversation; the user id comes from the authenticated principal, never the client.")]
public sealed class GetMyConversationByContextQuery : AizenQuery<GetConversationDetailResponse>
{
    public MessagingContextType ContextType { get; }
    public long ContextId                   { get; }

    public GetMyConversationByContextQuery(MessagingContextType contextType, long contextId)
    {
        ContextType = contextType;
        ContextId   = contextId;
    }
}
