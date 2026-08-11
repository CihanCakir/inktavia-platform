using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationTranscriptByContext;

/// <summary>
/// BE_WC3b — read the ordered chat transcript for a conversation resolved by domain context. NOT participant-scoped:
/// the caller is another module (ServiceRequest, composing the dispute case), authorized at its own layer. Never throws
/// on a missing conversation — an SR that has no chat yet yields an empty transcript.
/// </summary>
public sealed class GetConversationTranscriptByContextQuery : AizenQuery<ConversationTranscriptResponse>
{
    public MessagingContextType ContextType { get; }
    public long ContextId { get; }

    public GetConversationTranscriptByContextQuery(MessagingContextType contextType, long contextId)
    {
        ContextType = contextType;
        ContextId = contextId;
    }
}
