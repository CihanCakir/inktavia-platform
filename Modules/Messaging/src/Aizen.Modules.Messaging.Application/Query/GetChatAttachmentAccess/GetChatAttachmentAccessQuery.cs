using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetChatAttachmentAccess;

/// <summary>
/// BE_WC3a — participant-scoped chat-attachment access-check. Given a domain context (e.g. ServiceRequest id) and a
/// file id, returns whether the authenticated caller may read that chat image from Messaging: the caller MUST be a
/// participant of the conversation AND the fileId must match an attachment on one of its messages. The user id comes
/// from the authenticated principal, never the client.
/// </summary>
public sealed class GetChatAttachmentAccessQuery : AizenQuery<ChatAttachmentAccessResponse>
{
    public MessagingContextType ContextType { get; }
    public long ContextId                   { get; }
    public Guid FileId                      { get; }

    public GetChatAttachmentAccessQuery(MessagingContextType contextType, long contextId, Guid fileId)
    {
        ContextType = contextType;
        ContextId   = contextId;
        FileId      = fileId;
    }
}
