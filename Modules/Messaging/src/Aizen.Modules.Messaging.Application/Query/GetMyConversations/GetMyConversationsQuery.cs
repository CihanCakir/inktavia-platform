using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetMyConversations;

[DocumentationInfo("Get my conversations query",
    "Participant-scoped conversation list — conversations the AUTHENTICATED caller participates in. The user id is " +
    "resolved server-side from the request principal (never a client-supplied id). Phase 3 provider read cutover.")]
public sealed class GetMyConversationsQuery : AizenQuery<GetConversationListResponse>
{
    public MessagingContextType? ContextType { get; }
    public int Skip                          { get; }
    public int Take                          { get; }

    public GetMyConversationsQuery(MessagingContextType? contextType, int skip, int take)
    {
        ContextType = contextType;
        Skip        = skip;
        Take        = take;
    }
}
