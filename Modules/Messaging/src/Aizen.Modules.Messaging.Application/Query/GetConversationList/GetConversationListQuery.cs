using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationList;

[DocumentationInfo("Get conversation list query", "Paginated, filtered list of conversations.")]
public sealed class GetConversationListQuery : AizenQuery<GetConversationListResponse>
{
    public ConversationStatus? Status        { get; }
    public MessagingContextType? ContextType { get; }
    public int Skip                          { get; }
    public int Take                          { get; }

    public GetConversationListQuery(ConversationStatus? status, MessagingContextType? contextType, int skip, int take)
    {
        Status      = status;
        ContextType = contextType;
        Skip        = skip;
        Take        = take;
    }
}
