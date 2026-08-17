using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationByContext;

[DocumentationInfo("Get conversation by context query",
    "Retrieves a conversation by its owning domain context (e.g. a ServiceRequest ID).")]
public sealed class GetConversationByContextQuery : AizenQuery<GetConversationDetailResponse>
{
    public MessagingContextType ContextType { get; }
    public long ContextId                  { get; }

    public GetConversationByContextQuery(MessagingContextType contextType, long contextId)
    {
        ContextType = contextType;
        ContextId   = contextId;
    }
}
